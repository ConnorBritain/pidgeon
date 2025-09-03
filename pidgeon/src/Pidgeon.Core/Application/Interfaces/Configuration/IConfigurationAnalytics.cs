// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Service for configuration analytics and statistics.
/// Single responsibility: Configuration analytics and reporting.
/// </summary>
public interface IConfigurationAnalytics
{
    /// <summary>
    /// Gets statistics about the configuration catalog.
    /// </summary>
    /// <returns>Catalog statistics</returns>
    Task<Result<ConfigurationCatalogStats>> GetStatisticsAsync();

    /// <summary>
    /// Validates a message against an existing configuration.
    /// </summary>
    /// <param name="message">Message to validate</param>
    /// <param name="address">Configuration to validate against</param>
    /// <returns>Validation result</returns>
    Task<Result<ConfigurationValidationResult>> ValidateMessageAsync(
        string message,
        ConfigurationAddress address);
}