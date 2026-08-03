namespace MicroServiceSystem.BuildingBlocks.Localization.Configuration;

public sealed class FrameworkLocalizationOptions
{
    public const string SectionName = "Localization";

    public string DefaultCulture { get; set; } = "en-US";

    public string[] SupportedCultures { get; set; } = ["en-US", "tr-TR", "zh-CN", "es-ES", "hi-IN"];

    public string ResourcesPath { get; set; } = "Resources";

    public bool ApplyCurrentCultureToResponseHeaders { get; set; } = true;
}
