using System.ComponentModel.DataAnnotations;

namespace MicroServiceSystem.BuildingBlocks.Authentication.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(MinimumSigningKeyLength)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenLifetimeDays { get; set; } = 14;

    [Range(0, 300)]
    public int ClockSkewSeconds { get; set; } = 30;

    public bool RequireHttpsMetadata { get; set; } = true;

    public const int MinimumSigningKeyLength = 32;
}
