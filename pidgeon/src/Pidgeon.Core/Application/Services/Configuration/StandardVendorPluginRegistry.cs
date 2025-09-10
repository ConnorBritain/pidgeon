// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using System.Collections.Concurrent;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Registry for managing standard-specific vendor plugins.
/// Provides unified access to vendor intelligence across HL7, FHIR, NCPDP, and other standards.
/// Thread-safe implementation supporting dynamic plugin registration.
/// </summary>
internal class StandardVendorPluginRegistry : IStandardVendorPluginRegistry
{
    private readonly ConcurrentDictionary<string, IStandardVendorPlugin> _pluginsByStandard = new();
    private readonly ConcurrentBag<IStandardVendorPlugin> _allPlugins = new();
    private readonly ILogger<StandardVendorPluginRegistry> _logger;

    public StandardVendorPluginRegistry(ILogger<StandardVendorPluginRegistry> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void RegisterPlugin(IStandardVendorPlugin plugin)
    {
        if (plugin == null)
            throw new ArgumentNullException(nameof(plugin));

        if (string.IsNullOrWhiteSpace(plugin.Standard))
            throw new ArgumentException("Plugin must specify a standard", nameof(plugin));

        var standard = plugin.Standard.ToUpperInvariant();
        
        if (_pluginsByStandard.TryAdd(standard, plugin))
        {
            _allPlugins.Add(plugin);
            _logger.LogInformation("Registered vendor plugin for standard {Standard}: {DisplayName} (Priority: {Priority})", 
                plugin.Standard, plugin.DisplayName, plugin.Priority);
        }
        else
        {
            _logger.LogWarning("Plugin for standard {Standard} already registered, skipping duplicate", plugin.Standard);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IStandardVendorPlugin> GetAllPlugins()
    {
        return _allPlugins.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<IStandardVendorPlugin> GetPluginsForMessage(string messageContent)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
            return Array.Empty<IStandardVendorPlugin>();

        var compatiblePlugins = new List<IStandardVendorPlugin>();

        foreach (var plugin in _allPlugins)
        {
            try
            {
                if (plugin.CanAnalyze(messageContent))
                {
                    compatiblePlugins.Add(plugin);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if plugin {Standard} can analyze message", plugin.Standard);
            }
        }

        // Order by priority (highest first), then by standard name for deterministic ordering
        var orderedPlugins = compatiblePlugins
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.Standard)
            .ToList();

        _logger.LogDebug("Found {PluginCount} compatible plugins for message: {Standards}", 
            orderedPlugins.Count, string.Join(", ", orderedPlugins.Select(p => p.Standard)));

        return orderedPlugins;
    }

    /// <inheritdoc />
    public IStandardVendorPlugin? GetPluginForStandard(string standard)
    {
        if (string.IsNullOrWhiteSpace(standard))
            return null;

        var standardKey = standard.ToUpperInvariant();
        
        // Direct match first
        if (_pluginsByStandard.TryGetValue(standardKey, out var plugin))
        {
            return plugin;
        }

        // Try partial matching for version-specific standards
        // e.g., "HL7" should match "HL7V23", "HL7V25", etc.
        foreach (var (key, candidate) in _pluginsByStandard)
        {
            if (key.StartsWith(standardKey, StringComparison.OrdinalIgnoreCase) ||
                standardKey.StartsWith(key, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Found partial match for standard '{Standard}': {ActualStandard}", 
                    standard, candidate.Standard);
                return candidate;
            }
        }

        _logger.LogDebug("No plugin found for standard '{Standard}'", standard);
        return null;
    }

    /// <inheritdoc />
    public IReadOnlyList<IStandardVendorPlugin> GetPluginsForStandardFamily(string standardFamily)
    {
        if (string.IsNullOrWhiteSpace(standardFamily))
            return Array.Empty<IStandardVendorPlugin>();

        var familyKey = standardFamily.ToUpperInvariant();
        var familyPlugins = new List<IStandardVendorPlugin>();

        foreach (var plugin in _allPlugins)
        {
            var pluginStandard = plugin.Standard.ToUpperInvariant();
            
            // Check if plugin standard starts with family name
            // e.g., "HL7" family matches "HL7V23", "HL7V25", etc.
            if (pluginStandard.StartsWith(familyKey, StringComparison.OrdinalIgnoreCase))
            {
                familyPlugins.Add(plugin);
            }
        }

        // Order by priority within family
        var orderedPlugins = familyPlugins
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.Standard)
            .ToList();

        _logger.LogDebug("Found {PluginCount} plugins for standard family '{Family}': {Standards}", 
            orderedPlugins.Count, standardFamily, string.Join(", ", orderedPlugins.Select(p => p.Standard)));

        return orderedPlugins;
    }

    /// <summary>
    /// Gets comprehensive statistics about registered plugins.
    /// Useful for diagnostics and system health monitoring.
    /// </summary>
    public PluginRegistryStats GetStatistics()
    {
        var pluginsByFamily = _allPlugins
            .GroupBy(p => GetStandardFamily(p.Standard))
            .ToDictionary(g => g.Key, g => g.Count());

        return new PluginRegistryStats
        {
            TotalPlugins = _allPlugins.Count,
            RegisteredStandards = _pluginsByStandard.Keys.ToList(),
            PluginsByFamily = pluginsByFamily,
            AveragePriority = _allPlugins.Any() ? _allPlugins.Average(p => p.Priority) : 0.0
        };
    }

    private static string GetStandardFamily(string standard)
    {
        if (standard.StartsWith("HL7", StringComparison.OrdinalIgnoreCase))
            return "HL7";
        if (standard.StartsWith("FHIR", StringComparison.OrdinalIgnoreCase))
            return "FHIR";
        if (standard.StartsWith("NCPDP", StringComparison.OrdinalIgnoreCase))
            return "NCPDP";
        if (standard.StartsWith("DICOM", StringComparison.OrdinalIgnoreCase))
            return "DICOM";
        if (standard.StartsWith("CDA", StringComparison.OrdinalIgnoreCase))
            return "CDA";
        
        return "Other";
    }
}

/// <summary>
/// Statistics about the plugin registry for monitoring and diagnostics.
/// </summary>
public record PluginRegistryStats
{
    /// <summary>
    /// Total number of registered plugins.
    /// </summary>
    public required int TotalPlugins { get; init; }

    /// <summary>
    /// List of all registered standard names.
    /// </summary>
    public required IReadOnlyList<string> RegisteredStandards { get; init; }

    /// <summary>
    /// Count of plugins by standard family (HL7, FHIR, NCPDP, etc.).
    /// </summary>
    public required IReadOnlyDictionary<string, int> PluginsByFamily { get; init; }

    /// <summary>
    /// Average priority across all plugins.
    /// </summary>
    public required double AveragePriority { get; init; }

    /// <summary>
    /// Gets a summary string for logging and diagnostics.
    /// </summary>
    public string GetSummary()
    {
        var families = string.Join(", ", PluginsByFamily.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        return $"{TotalPlugins} plugins registered - Families: {families} - Avg Priority: {AveragePriority:F1}";
    }
}