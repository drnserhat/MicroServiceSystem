namespace MicroServiceSystem.BuildingBlocks.Authentication.Configuration;

public sealed class PasswordPolicyOptions
{
    public const string SectionName = "Authentication:PasswordPolicy";

    public int MinimumLength { get; set; } = 12;

    public int MaximumLength { get; set; } = 128;

    public bool RequireUppercase { get; set; } = true;

    public bool RequireLowercase { get; set; } = true;

    public bool RequireDigit { get; set; } = true;

    public bool RequireNonAlphanumeric { get; set; } = true;

    public int PasswordHistoryCount { get; set; } = 5;

    public int MaximumFailedAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public int HashIterations { get; set; } = 210_000;
}
