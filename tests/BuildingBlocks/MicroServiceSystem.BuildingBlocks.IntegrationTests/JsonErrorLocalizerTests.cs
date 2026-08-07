using System.Globalization;
using MicroServiceSystem.BuildingBlocks.Localization;
using MicroServiceSystem.SharedKernel.Results;
using Shouldly;

namespace MicroServiceSystem.BuildingBlocks.IntegrationTests;

public sealed class JsonErrorLocalizerTests
{
    [Fact]
    public void Localize_uses_current_ui_culture_then_falls_back_to_english()
    {
        var localizer = new JsonErrorLocalizer();
        Error source = Error.Unauthorized("identity.invalid_credentials", "Invalid email or password.");

        CultureInfo previousUi = CultureInfo.CurrentUICulture;
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            Error turkish = localizer.Localize(source);
            turkish.Code.ShouldBe(source.Code);
            turkish.Description.ShouldBe("Geçersiz e-posta veya şifre.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zz-ZZ");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("zz-ZZ");

            // Unknown culture falls back to embedded en-US catalog (not the source description).
            Error english = localizer.Localize(
                Error.Unauthorized("identity.invalid_credentials", "placeholder that must be replaced"));
            english.Description.ShouldBe("Invalid email or password.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Localize_preserves_unknown_codes()
    {
        var localizer = new JsonErrorLocalizer();
        Error source = Error.Failure("custom.unknown_code", "Keep me");

        Error localized = localizer.Localize(source);

        localized.ShouldBe(source);
    }
}
