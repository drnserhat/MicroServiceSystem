using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;
using RabbitMQ.Client;

namespace MicroServiceSystem.BuildingBlocks.Messaging.RabbitMq;

/// <summary>
/// Keeps publisher channels alive between messages. A channel is rented by one publish at a time because
/// <see cref="IChannel"/> is not meant to be shared across concurrent publishes.
/// </summary>
public sealed class RabbitMqChannelPool(
    RabbitMqConnectionProvider connectionProvider,
    IOptions<RabbitMqOptions> options) : IAsyncDisposable
{
    private readonly ConcurrentBag<IChannel> _idle = [];
    private int _idleCount;
    private bool _disposed;

    public async ValueTask<IChannel> RentAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        while (_idle.TryTake(out IChannel? pooled))
        {
            Interlocked.Decrement(ref _idleCount);

            if (pooled.IsOpen)
            {
                return pooled;
            }

            await pooled.DisposeAsync();
        }

        return await connectionProvider.CreateChannelAsync(
            options.Value.PublisherConfirms,
            consumerDispatchConcurrency: null,
            cancellationToken);
    }

    public async ValueTask ReturnAsync(IChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        if (_disposed || !channel.IsOpen)
        {
            await DiscardAsync(channel);
            return;
        }

        if (Interlocked.Increment(ref _idleCount) > options.Value.PublisherChannelPoolSize)
        {
            Interlocked.Decrement(ref _idleCount);
            await DiscardAsync(channel);
            return;
        }

        _idle.Add(channel);
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        while (_idle.TryTake(out IChannel? pooled))
        {
            Interlocked.Decrement(ref _idleCount);
            await DiscardAsync(pooled);
        }
    }

    private static async ValueTask DiscardAsync(IChannel channel)
    {
        try
        {
            if (channel.IsOpen)
            {
                await channel.CloseAsync();
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A channel that cannot be closed cleanly is already unusable; disposing is all that is left.
        }

        await channel.DisposeAsync();
    }
}
