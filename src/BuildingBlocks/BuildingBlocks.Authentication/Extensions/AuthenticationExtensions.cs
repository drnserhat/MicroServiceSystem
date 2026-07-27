using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        IConfiguration configuration)
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

        services.AddHttpContextAccessor();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        JwtOptions jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
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
            })
            .AddScheme<AuthenticationSchemeOptions, InternalApiKeyAuthenticationHandler>(
                InternalApiKeyDefaults.AuthenticationScheme,
                _ => { });

        return services;
    }
}
