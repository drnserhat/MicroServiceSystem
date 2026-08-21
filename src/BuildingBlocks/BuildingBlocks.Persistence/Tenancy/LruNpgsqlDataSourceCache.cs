using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Tenancy;

public sealed class NpgsqlDataSourceCacheOptions
{
    public const string SectionName = "Persistence:Postgres:DataSourceCache";

    public int MaxEntries { get; set; } = 256;

    public int IdleEvictionMinutes { get; set; } = 15;
}

public sealed class LruNpgsqlDataSourceCache : INpgsqlDataSourceCache, IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private readonly NpgsqlDataSourceCacheOptions _options;
    private bool _disposed;

    public LruNpgsqlDataSourceCache(IOptions<NpgsqlDataSourceCacheOptions>? options = null)
    {
        _options = options?.Value ?? new NpgsqlDataSourceCacheOptions();
    }

    public NpgsqlDataSource GetOrAdd(Guid tenantId, string serviceKey, string connectionString)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string key = BuildKey(tenantId, serviceKey);

        CacheEntry entry = _entries.AddOrUpdate(
            key,
            _ => new CacheEntry(CreateDataSource(connectionString)),
            (_, existing) =>
            {
                existing.Touch();
                return existing;
            });

        EvictIfNeeded();
        return entry.DataSource;
    }

    public void Remove(Guid tenantId, string serviceKey)
    {
        if (_entries.TryRemove(BuildKey(tenantId, serviceKey), out CacheEntry? entry))
        {
            entry.DataSource.Dispose();
        }
    }

    public void RemoveAllForTenant(Guid tenantId)
    {
        string prefix = tenantId.ToString("N") + "|";
        foreach (string key in _entries.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal)
                && _entries.TryRemove(key, out CacheEntry? entry))
            {
                entry.DataSource.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (string key in _entries.Keys)
        {
            if (_entries.TryRemove(key, out CacheEntry? entry))
            {
                entry.DataSource.Dispose();
            }
        }
    }

    private static NpgsqlDataSource CreateDataSource(string connectionString) =>
        new NpgsqlDataSourceBuilder(connectionString).Build();

    private static string BuildKey(Guid tenantId, string serviceKey) =>
        $"{tenantId:N}|{serviceKey.Trim().ToLowerInvariant()}";

    private void EvictIfNeeded()
    {
        if (_entries.Count <= _options.MaxEntries)
        {
            EvictIdle();
            return;
        }

        lock (_gate)
        {
            if (_entries.Count <= _options.MaxEntries)
            {
                EvictIdle();
                return;
            }

            List<KeyValuePair<string, CacheEntry>> ordered = _entries
                .OrderBy(pair => pair.Value.LastAccessUtc)
                .ToList();

            int removeCount = _entries.Count - _options.MaxEntries;
            for (int i = 0; i < removeCount && i < ordered.Count; i++)
            {
                if (_entries.TryRemove(ordered[i].Key, out CacheEntry? entry))
                {
                    entry.DataSource.Dispose();
                }
            }

            EvictIdle();
        }
    }

    private void EvictIdle()
    {
        if (_options.IdleEvictionMinutes <= 0)
        {
            return;
        }

        DateTimeOffset threshold = DateTimeOffset.UtcNow.AddMinutes(-_options.IdleEvictionMinutes);
        foreach (KeyValuePair<string, CacheEntry> pair in _entries)
        {
            if (pair.Value.LastAccessUtc < threshold
                && _entries.TryRemove(pair.Key, out CacheEntry? entry))
            {
                entry.DataSource.Dispose();
            }
        }
    }

    private sealed class CacheEntry(NpgsqlDataSource dataSource)
    {
        public NpgsqlDataSource DataSource { get; } = dataSource;

        public DateTimeOffset LastAccessUtc { get; private set; } = DateTimeOffset.UtcNow;

        public void Touch() => LastAccessUtc = DateTimeOffset.UtcNow;
    }
}
