using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Messaging.Configuration;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;

namespace MicroServiceSystem.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqConnectionProvider : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConnectionProvider> _logger;
    private readonly ResiliencePipeline _connectPipeline;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnectionProvider(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqConnectionProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        _connectPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _options.ConnectionRetryCount,
                Delay = TimeSpan.FromSeconds(_options.ConnectionRetryDelaySeconds),
                BackoffType = DelayBackoffType.Constant,
                OnRetry = arguments =>
                {
                    _logger.LogWarning(
                        arguments.Outcome.Exception,
                        "RabbitMQ connection attempt {AttemptNumber} failed",
                        arguments.AttemptNumber + 1);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _connection = await _connectPipeline.ExecuteAsync(
                async token => await CreateConnectionAsync(token),
                cancellationToken);

            _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", _options.Host, _options.Port);

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task<IChannel> CreateChannelAsync(
        bool publisherConfirms,
        ushort? consumerDispatchConcurrency = null,
        CancellationToken cancellationToken = default)
    {
        IConnection connection = await GetConnectionAsync(cancellationToken);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: publisherConfirms,
            publisherConfirmationTrackingEnabled: publisherConfirms,
            consumerDispatchConcurrency: consumerDispatchConcurrency);

        return await connection.CreateChannelAsync(channelOptions, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }

        _connectionLock.Dispose();
    }

    private async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        if (_options.UseSsl)
        {
            factory.Ssl = new SslOption { Enabled = true, ServerName = _options.Host };
        }

        return await factory.CreateConnectionAsync(cancellationToken);
    }
}
