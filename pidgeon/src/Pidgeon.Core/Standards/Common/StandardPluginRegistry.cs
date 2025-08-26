// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;

namespace Pidgeon.Core.Standards.Common;

/// <summary>
/// Implementation of the standard plugin registry.
/// </summary>
internal class StandardPluginRegistry : IStandardPluginRegistry
{
    private readonly IEnumerable<IStandardPlugin> _plugins;
    private readonly ILogger<StandardPluginRegistry> _logger;

    public StandardPluginRegistry(IEnumerable<IStandardPlugin> plugins, ILogger<StandardPluginRegistry> logger)
    {
        _plugins = plugins;
        _logger = logger;
        
        LogRegisteredPlugins();
    }

    public IReadOnlyList<IStandardPlugin> GetAllPlugins()
    {
        return _plugins.ToList();
    }

    public IStandardPlugin? GetPlugin(string standardName, Version? standardVersion = null)
    {
        var candidates = _plugins.Where(p => 
            string.Equals(p.StandardName, standardName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!candidates.Any())
            return null;

        if (standardVersion == null)
        {
            // Return the plugin with the highest version
            return candidates.OrderByDescending(p => p.StandardVersion).First();
        }

        // Return exact version match
        return candidates.FirstOrDefault(p => p.StandardVersion == standardVersion);
    }

    public IReadOnlyList<IStandardPlugin> GetCapablePlugins(string messageContent)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
            return Array.Empty<IStandardPlugin>();

        var capablePlugins = new List<IStandardPlugin>();

        foreach (var plugin in _plugins)
        {
            try
            {
                if (plugin.CanHandle(messageContent))
                {
                    capablePlugins.Add(plugin);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Plugin {PluginName} threw exception when checking if it can handle message", 
                    plugin.StandardName);
            }
        }

        return capablePlugins;
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetSupportedMessageTypes()
    {
        return _plugins.ToDictionary(
            p => $"{p.StandardName} v{p.StandardVersion}",
            p => p.SupportedMessageTypes);
    }

    private void LogRegisteredPlugins()
    {
        if (!_plugins.Any())
        {
            _logger.LogWarning("No standard plugins are registered");
            return;
        }

        _logger.LogInformation("Registered {PluginCount} standard plugins:", _plugins.Count());
        
        foreach (var plugin in _plugins)
        {
            _logger.LogInformation("  - {StandardName} v{StandardVersion}: {MessageTypeCount} message types", 
                plugin.StandardName, 
                plugin.StandardVersion, 
                plugin.SupportedMessageTypes.Count);
        }
    }
}