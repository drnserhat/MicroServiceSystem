using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Localization.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Localization.Extensions;

public static class LocalizationExtensions
{
    public static IServiceCollection AddFrameworkLocalization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FrameworkLocalizationOptions>()
            .Bind(configuration.GetSection(FrameworkLocalizationOptions.SectionName))
            .ValidateOnStart();

        FrameworkLocalizationOptions localizationOptions = configuration
            .GetSection(FrameworkLocalizationOptions.SectionName)
            .Get<FrameworkLocalizationOptions>() ?? new FrameworkLocalizationOptions();

        services.AddLocalization(options => options.ResourcesPath = localizationOptions.ResourcesPath);

        services.Configure<RequestLocalizationOptions>(options =>
        {
            CultureInfo[] cultures = localizationOptions.SupportedCultures
                .Select(TryCreateCulture)
                .OfType<CultureInfo>()
                .ToArray();

            if (cultures.Length == 0)
            {
                cultures = [CultureInfo.InvariantCulture];
            }

            CultureInfo defaultCulture = TryCreateCulture(localizationOptions.DefaultCulture) ?? cultures[0];

            options.DefaultRequestCulture = new RequestCulture(defaultCulture);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            options.ApplyCurrentCultureToResponseHeaders = localizationOptions.ApplyCurrentCultureToResponseHeaders;

            options.RequestCultureProviders =
            [
                new AcceptLanguageHeaderRequestCultureProvider(),
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider()
            ];
        });

        return services;
    }

    public static IApplicationBuilder UseFrameworkLocalization(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseRequestLocalization();
    }

    /// <summary>
    /// Returns null instead of throwing when a culture is unavailable, so a misconfigured or
    /// ICU-less host degrades to invariant rather than failing to boot.
    /// </summary>
    private static CultureInfo? TryCreateCulture(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return null;
        }

        try
        {
            return CultureInfo.GetCultureInfo(culture);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            // Thrown under globalization-invariant mode for non-invariant culture names.
            return null;
        }
    }
}
