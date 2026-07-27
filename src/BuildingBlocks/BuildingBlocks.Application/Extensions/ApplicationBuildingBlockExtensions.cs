using System.Reflection;
using FluentValidation;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Application.Behaviors;
using MicroServiceSystem.BuildingBlocks.Application.Configuration;
using MicroServiceSystem.BuildingBlocks.Application.DomainEvents;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Application.Extensions;

public static class ApplicationBuildingBlockExtensions
{
    /// <summary>
    /// Registers CQRS dispatching, the validation pipeline, mapping and domain event dispatching for
    /// the given application assemblies.
    /// </summary>
    public static IServiceCollection AddApplicationBuildingBlock(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] applicationAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (applicationAssemblies.Length == 0)
        {
            throw new ArgumentException("At least one application assembly must be provided.", nameof(applicationAssemblies));
        }

        services.AddOptions<ApplicationPipelineOptions>()
            .Bind(configuration.GetSection(ApplicationPipelineOptions.SectionName))
            .ValidateOnStart();

        ApplicationPipelineOptions pipelineOptions = configuration
            .GetSection(ApplicationPipelineOptions.SectionName)
            .Get<ApplicationPipelineOptions>() ?? new ApplicationPipelineOptions();

        services.AddMediatR(configurationExpression =>
            configurationExpression.RegisterServicesFromAssemblies(applicationAssemblies));

        if (pipelineOptions.EnableRequestLogging)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        }

        if (pipelineOptions.EnablePerformanceLogging)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        }

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        if (pipelineOptions.EnableUnitOfWorkBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        }

        services.AddValidatorsFromAssemblies(applicationAssemblies, includeInternalTypes: true);

        services.AddMappingConfiguration(applicationAssemblies);

        services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

        return services;
    }

    private static IServiceCollection AddMappingConfiguration(
        this IServiceCollection services,
        Assembly[] applicationAssemblies)
    {
        TypeAdapterConfig typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
        typeAdapterConfig.Scan(applicationAssemblies);

        services.AddSingleton(typeAdapterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
