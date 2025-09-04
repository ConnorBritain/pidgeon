// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Interfaces.Configuration;

/// <summary>
/// Repository for configuration persistence operations.
/// Single responsibility: Configuration storage and modification.
/// </summary>
public interface IConfigurationRepository
{
    /// <summary>
    /// Stores or updates a configuration.
    /// </summary>
    /// <param name="configuration">Configuration to store</param>
    /// <returns>Success or failure result</returns>
    Task<Result> StoreConfigurationAsync(VendorConfiguration configuration);

    /// <summary>
    /// Removes a configuration by address.
    /// </summary>
    /// <param name="address">Configuration address to remove</param>
    /// <returns>Success or failure result</returns>
    Task<Result> RemoveConfigurationAsync(ConfigurationAddress address);

    /// <summary>
    /// Gets configuration change history for evolution tracking.
    /// </summary>
    /// <param name="address">Configuration address</param>
    /// <param name="timeWindow">Time window to look back</param>
    /// <returns>List of configuration changes</returns>
    Task<Result<IReadOnlyList<ConfigurationChange>>> GetChangeHistoryAsync(
        ConfigurationAddress address,
        TimeSpan? timeWindow = null);
}