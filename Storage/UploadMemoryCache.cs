using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Supabase.Storage;

public class UploadMemoryCache
{
    // Thread-safe in-memory cache for resumable upload URLs keyed by an identifier (e.g., file path or upload id).
    // Uses simple sliding expiration.
    private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    // Default sliding expiration for cached URLs.
    private static TimeSpan _defaultTtl = TimeSpan.FromMinutes(60);

    private static long _version; // helps with testing/observability if needed

    private sealed class CacheEntry
    {
        public string Url { get; }
        public DateTimeOffset Expiration { get; private set; }
        public TimeSpan Ttl { get; }

        public CacheEntry(string url, TimeSpan ttl)
        {
            Url = url;
            Ttl = ttl <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : ttl;
            Touch();
        }

        public void Touch()
        {
            Expiration = DateTimeOffset.UtcNow.Add(Ttl);
        }

        public bool IsExpired() => DateTimeOffset.UtcNow >= Expiration;
    }

    // Sets the default time-to-live for future cache entries.
    public static void SetDefaultTtl(TimeSpan ttl)
    {
        _defaultTtl = ttl <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : ttl;
    }

    // Store or update the resumable upload URL for the provided key.
    public static void Set(string key, string url, TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key must be provided.", nameof(key));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url must be provided.", nameof(url));

        var entryTtl = ttl.GetValueOrDefault(_defaultTtl);
        _cache.AddOrUpdate(
            key,
            _ => new CacheEntry(url, entryTtl),
            (_, existing) => new CacheEntry(url, entryTtl)
        );

        Interlocked.Increment(ref _version);
        CleanupIfNeeded();
    }

    // Try to get a cached URL. Refreshes sliding expiration on successful hit.
    public static bool TryGet(string key, out string? url)
    {
        url = null;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired())
            {
                // Evict expired entry
                _cache.TryRemove(key, out _);
                return false;
            }

            // Sliding expiration
            entry.Touch();
            url = entry.Url;
            return true;
        }

        return false;
    }

    // Remove a cached URL.
    public static bool Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var removed = _cache.TryRemove(key, out _);
        if (removed)
            Interlocked.Increment(ref _version);
        return removed;
    }

    // Clear all cached URLs.
    public static void Clear()
    {
        _cache.Clear();
        Interlocked.Increment(ref _version);
    }

    // Optionally expose count for diagnostics.
    public static int Count => _cache.Count;

    // Simple opportunistic cleanup to remove expired entries.
    private static void CleanupIfNeeded()
    {
        // Cheap scan for expired entries. No need for strict guarantees.
        foreach (var kvp in _cache)
        {
            if (kvp.Value.IsExpired())
            {
                _cache.TryRemove(kvp.Key, out _);
            }
        }
    }
}