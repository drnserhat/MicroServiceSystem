using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MicroServiceSystem.BuildingBlocks.Authentication.Abstractions;
using MicroServiceSystem.BuildingBlocks.Authentication.Configuration;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.BuildingBlocks.Authentication.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddFrameworkAuthentication(
        this IServiceCollection services,
        IConfiguration configuration) =>
        AddFrameworkAuthentication(services, configuration, environment: null);

    public static IServiceCollection AddFrameworkAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PasswordPolicyOptions>()
            .Bind(configuration.GetSection(PasswordPolicyOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<InternalServiceOptions>()
            .Bind(configuration.GetSection(InternalServiceOptions.SectionName))
            .Validate(options =>
                !options.Enabled
                || string.IsNullOrWhiteSpace(options.ApiKey)
                || options.ApiKey.Length >= 16,
                "Authentication:InternalService:ApiKey must be at least 16 characters when Enabled.")
            .ValidateOnStart();

        if (environment is not null)
        {
            services.AddSingleton<IValidateOptions<JwtOptions>>(new JwtOptionsValidator(environment));
            services.AddSingleton<IValidateOptions<InternalServiceOptions>>(
                new InternalServiceOptionsValidator(environment));
        }
        else
        {
            services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
            services.AddSingleton<IValidateOptions<InternalServiceOptions>, InternalServiceOptionsValidator>();
        }

        services.AddHttpContextAccessor();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer()
            .AddScheme<AuthenticationSchemeOptions, InternalApiKeyAuthenticationHandler>(
                InternalApiKeyDefaults.AuthenticationScheme,
                _ => { });

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
            {
                JwtOptions jwtOptions = jwtOptionsAccessor.Value;
                bearerOptions.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
                bearerOptions.MapInboundClaims = false;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds),
                    NameClaimType = FrameworkClaimTypes.UserName,
                    RoleClaimType = FrameworkClaimTypes.Role
                };
            });

        return services;
    }
}
