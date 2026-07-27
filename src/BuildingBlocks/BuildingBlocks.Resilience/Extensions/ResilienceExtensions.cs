using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using MicroServiceSystem.BuildingBlocks.Resilience.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Resilience.Extensions;

public static class ResilienceExtensions
{
    /// <summary>
    /// Applies the standard timeout/retry/circuit breaker pipeline to a typed HttpClient.
    /// </summary>
    public static IHttpClientBuilder AddFrameworkHttpClient<TClient>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? baseAddress = null)
        where TClient : class =>
        ConfigureResilience(
            services,
            configuration,
            services.AddHttpClient<TClient>(client => ApplyBaseAddress(client, baseAddress)));

    /// <summary>
    /// Typed client registration with interface + implementation and the standard resilience pipeline.
    /// </summary>
    public static IHttpClientBuilder AddFrameworkHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? baseAddress = null)
        where TClient : class
        where TImplementation : class, TClient =>
        ConfigureResilience(
            services,
            configuration,
            services.AddHttpClient<TClient, TImplementation>(client => ApplyBaseAddress(client, baseAddress)));

    private static void ApplyBaseAddress(HttpClient client, string? baseAddress)
    {
        if (!string.IsNullOrWhiteSpace(baseAddress))
        {
            client.BaseAddress = new Uri(baseAddress);
        }
    }

    private static IHttpClientBuilder ConfigureResilience(
        IServiceCollection services,
        IConfiguration configuration,
        IHttpClientBuilder clientBuilder)
    {
        services.AddOptions<ResilienceOptions>()
            .Bind(configuration.GetSection(ResilienceOptions.SectionName))
            .ValidateOnStart();

        ResilienceOptions resilienceOptions = configuration.GetSection(ResilienceOptions.SectionName)
            .Get<ResilienceOptions>() ?? new ResilienceOptions();

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
