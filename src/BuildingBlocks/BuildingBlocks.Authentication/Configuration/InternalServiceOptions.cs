using System.ComponentModel.DataAnnotations;

namespace MicroServiceSystem.BuildingBlocks.Authentication.Configuration;

/// <summary>
/// Shared secret used by Coordinator (and other internal callers) to invoke privileged service endpoints.
/// </summary>
public sealed class InternalServiceOptions
{
    public const string SectionName = "Authentication:InternalService";

    public bool Enabled { get; set; }

    /// <summary>
    /// Shared API key. Leave empty to disable the scheme (internal endpoints will reject callers).
    /// </summary>
    [MinLength(16)]
    public string ApiKey { get; set; } = string.Empty;

    public string HeaderName { get; set; } = "X-Internal-Api-Key";
}
