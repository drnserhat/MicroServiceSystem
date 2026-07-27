namespace MicroServiceSystem.BuildingBlocks.Caching.Abstractions;

/// <summary>
/// Builds fully qualified cache keys. Keys are always scoped by service and tenant so that a cached
/// entry can never leak across tenant boundaries.
/// </summary>
public interface ICacheKeyBuilder
{
    string Build(string category, params ReadOnlySpan<string> segments);
}
