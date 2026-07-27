using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using MicroServiceSystem.BuildingBlocks.Resilience.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Resilience.Extensions;

public static class ResilienceExtensions
{
    /// <summary>
    /// Applies the standard timeout/retry/circuit breaker pipeline to every typed HttpClient that opts
    /// in through <see cref="AddFrameworkHttpClient{TClient}"/>.
    /// </summary>
    public static IHttpClientBuilder AddFrameworkHttpClient<TClient>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? baseAddress = null)
        where TClient : class
    {
        services.AddOptions<ResilienceOptions>()
            .Bind(configuration.GetSection(ResilienceOptions.SectionName))
            .ValidateOnStart();

        ResilienceOptions resilienceOptions = configuration.GetSection(ResilienceOptions.SectionName)
            .Get<ResilienceOptions>() ?? new ResilienceOptions();

        IHttpClientBuilder clientBuilder = services.AddHttpClient<TClient>(client =>
        {
            if (!string.IsNullOrWhiteSpace(baseAddress))
            {
                client.BaseAddress = new Uri(baseAddress);
            }
        });

        clientBuilder.AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(resilienceOptions.TotalRequestTimeoutSeconds);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(resilienceOptions.AttemptTimeoutSeconds);
            options.Retry.MaxRetryAttempts = resilienceOptions.MaxRetryAttempts;
            options.Retry.Delay = TimeSpan.FromMilliseconds(resilienceOptions.RetryBaseDelayMilliseconds);
            options.CircuitBreaker.FailureRatio = resilienceOptions.CircuitBreakerFailureRatio;
            options.CircuitBreaker.SamplingDuration =
                TimeSpan.FromSeconds(resilienceOptions.CircuitBreakerSamplingDurationSeconds);
            options.CircuitBreaker.MinimumThroughput = resilienceOptions.CircuitBreakerMinimumThroughput;
            options.CircuitBreaker.BreakDuration =
                TimeSpan.FromSeconds(resilienceOptions.CircuitBreakerBreakDurationSeconds);
        });

        return clientBuilder;
    }
}
