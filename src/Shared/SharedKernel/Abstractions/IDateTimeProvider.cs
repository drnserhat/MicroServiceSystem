namespace MicroServiceSystem.SharedKernel.Abstractions;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }

    DateOnly TodayUtc { get; }
}
