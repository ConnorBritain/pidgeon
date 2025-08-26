// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Standards.Common;

/// <summary>
/// Registry for managing standard plugins.
/// </summary>
public interface IStandardPluginRegistry
{
    /// <summary>
    /// Gets all registered plugins.
    /// </summary>
    /// <returns>A list of all registered plugins</returns>
    IReadOnlyList<IStandardPlugin> GetAllPlugins();

    /// <summary>
    /// Gets a plugin by standard name and version.
    /// </summary>
    /// <param name="standardName">The standard name</param>
    /// <param name="standardVersion">The standard version (optional)</param>
    /// <returns>The plugin if found, null otherwise</returns>
    IStandardPlugin? GetPlugin(string standardName, Version? standardVersion = null);

    /// <summary>
    /// Gets plugins that can handle the specified message content.
    /// </summary>
    /// <param name="messageContent">The message content</param>
    /// <returns>A list of plugins that can handle the message</returns>
    IReadOnlyList<IStandardPlugin> GetCapablePlugins(string messageContent);

    /// <summary>
    /// Gets supported message types across all plugins.
    /// </summary>
    /// <returns>A dictionary mapping standards to their supported message types</returns>
    IReadOnlyDictionary<string, IReadOnlyList<string>> GetSupportedMessageTypes();
}