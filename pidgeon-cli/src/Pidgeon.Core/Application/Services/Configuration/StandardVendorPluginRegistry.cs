// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Registry for managing standard-specific vendor plugins.
/// Provides unified access to vendor intelligence across all healthcare standards.
/// </summary>
internal class StandardVendorPluginRegistry : IStandardVendorPluginRegistry
{
    private readonly List<IStandardVendorPlugin> _plugins;
    private readonly ILogger<StandardVendorPluginRegistry> _logger;

    public StandardVendorPluginRegistry(
        IEnumerable<IStandardVendorPlugin> plugins,
        ILogger<StandardVendorPluginRegistry> logger)
    {
        _plugins = plugins.ToList();
        _logger = logger;
        
        _logger.LogInformation("Initialized StandardVendorPluginRegistry with {PluginCount} plugins", _plugins.Count);
    }

    public void RegisterPlugin(IStandardVendorPlugin plugin)
    {
        if (plugin == null)
            throw new ArgumentNullException(nameof(plugin));

        if (!_plugins.Contains(plugin))
        {
            _plugins.Add(plugin);
            _logger.LogDebug("Registered vendor plugin for standard: {Standard}", plugin.Standard);
        }
    }

    public IReadOnlyList<IStandardVendorPlugin> GetAllPlugins()
    {
        return _plugins.AsReadOnly();
    }

    public IReadOnlyList<IStandardVendorPlugin> GetPluginsForMessage(string messageContent)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
            return new List<IStandardVendorPlugin>();

        var compatiblePlugins = new List<(IStandardVendorPlugin Plugin, int Priority)>();

        foreach (var plugin in _plugins)
        {
            if (plugin.CanAnalyze(messageContent))
            {
                // Assign priority based on standard (HL7 gets higher priority for pipe-delimited messages)
                var priority = GetStandardPriority(plugin.Standard, messageContent);
                compatiblePlugins.Add((plugin, priority));
            }
        }

        return compatiblePlugins
            .OrderByDescending(p => p.Priority)
            .Select(p => p.Plugin)
            .ToList();
    }

    public IStandardVendorPlugin? GetPluginForStandard(string standard)
    {
        if (string.IsNullOrWhiteSpace(standard))
            return null;

        var standardLower = standard.ToLowerInvariant();
        return _plugins.FirstOrDefault(p => p.Standard.ToLowerInvariant() == standardLower);
    }

    public IReadOnlyList<IStandardVendorPlugin> GetPluginsForStandardFamily(string standardFamily)
    {
        if (string.IsNullOrWhiteSpace(standardFamily))
            return new List<IStandardVendorPlugin>();

        var familyLower = standardFamily.ToLowerInvariant();
        return _plugins
            .Where(p => p.Standard.ToLowerInvariant().StartsWith(familyLower))
            .ToList();
    }

    private int GetStandardPriority(string standard, string messageContent)
    {
        var standardLower = standard.ToLowerInvariant();

        // HL7 messages typically start with MSH and use pipe delimiters
        if (messageContent.StartsWith("MSH") && messageContent.Contains('|'))
        {
            if (standardLower.StartsWith("hl7"))
                return 100;
            
            return 10; // Lower priority for non-HL7 plugins analyzing HL7-like content
        }

        // FHIR messages are typically JSON
        if (messageContent.TrimStart().StartsWith("{") || messageContent.TrimStart().StartsWith("["))
        {
            if (standardLower.StartsWith("fhir"))
                return 100;
                
            return 10; // Lower priority for non-FHIR plugins analyzing JSON
        }

        // NCPDP messages have specific format patterns
        if (standardLower.StartsWith("ncpdp"))
        {
            return 50; // Medium priority for NCPDP
        }

        // Default priority
        return 1;
    }
}