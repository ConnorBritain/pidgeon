// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Interfaces.Configuration;

/// <summary>
/// Service for configuration comparison and similarity analysis.
/// Single responsibility: Configuration comparison operations.
/// </summary>
public interface IConfigurationComparator
{
    /// <summary>
    /// Compares two configurations and returns differences.
    /// </summary>
    /// <param name="fromAddress">Source configuration address</param>
    /// <param name="toAddress">Target configuration address</param>
    /// <returns>Configuration comparison result</returns>
    Task<Result<ConfigurationComparison>> CompareConfigurationsAsync(
        ConfigurationAddress fromAddress,
        ConfigurationAddress toAddress);

    /// <summary>
    /// Finds configurations similar to the provided reference.
    /// </summary>
    /// <param name="reference">Reference configuration</param>
    /// <param name="threshold">Similarity threshold (0.0 to 1.0)</param>
    /// <returns>List of similar configurations</returns>
    Task<Result<IReadOnlyList<VendorConfiguration>>> FindSimilarAsync(
        VendorConfiguration reference,
        double threshold = 0.7);
}