// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;

namespace Segmint.Core.Performance;

/// <summary>
/// Thread-safe cache for parsed field components to avoid repeated string splitting.
/// </summary>
internal static class ComponentCache
{
    private static readonly ConcurrentDictionary<string, string[]> Cache = new();
    private const int MaxCacheSize = 10000; // Prevent memory leaks
    private static volatile int _cacheSize = 0;

    /// <summary>
    /// Gets or caches the split components for a given HL7 field value.
    /// </summary>
    /// <param name="value">The raw field value.</param>
    /// <param name="separator">The component separator.</param>
    /// <returns>Array of components.</returns>
    public static string[] GetComponents(string value, char separator = '^')
    {
        if (string.IsNullOrEmpty(value))
            return Array.Empty<string>();

        // Check cache first
        var cacheKey = $"{value}|{separator}";
        if (Cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        // Split and cache if under limit
        var components = value.Split(separator);
        
        if (_cacheSize < MaxCacheSize)
        {
            if (Cache.TryAdd(cacheKey, components))
            {
                System.Threading.Interlocked.Increment(ref _cacheSize);
            }
        }

        return components;
    }

    /// <summary>
    /// Clears the component cache (useful for testing or memory management).
    /// </summary>
    public static void Clear()
    {
        Cache.Clear();
        _cacheSize = 0;
    }

    /// <summary>
    /// Gets the current cache size for monitoring.
    /// </summary>
    public static int CacheSize => _cacheSize;
}