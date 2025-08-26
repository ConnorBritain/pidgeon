// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Standards.Common;

/// <summary>
/// Defines the contract for standard-specific configuration.
/// </summary>
public interface IStandardConfig
{
    /// <summary>
    /// Gets the standard this configuration applies to.
    /// </summary>
    string StandardName { get; }

    /// <summary>
    /// Gets the version of the standard.
    /// </summary>
    Version StandardVersion { get; }

    /// <summary>
    /// Gets configuration values by key.
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <returns>The configuration value, or null if not found</returns>
    string? GetValue(string key);

    /// <summary>
    /// Gets all configuration keys.
    /// </summary>
    /// <returns>A list of all configuration keys</returns>
    IReadOnlyList<string> GetKeys();

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>A result indicating whether the configuration is valid</returns>
    Result<IStandardConfig> Validate();
}