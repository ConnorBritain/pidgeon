// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Interfaces.Configuration;

/// <summary>
/// Service for querying and retrieving configurations.
/// Single responsibility: Configuration retrieval operations.
/// </summary>
public interface IConfigurationQuery
{
    /// <summary>
    /// Gets a configuration by exact address.
    /// </summary>
    /// <param name="address">Configuration address</param>
    /// <returns>Vendor configuration if found</returns>
    Task<Result<VendorConfiguration?>> GetConfigurationAsync(ConfigurationAddress address);

    /// <summary>
    /// Gets all configurations for a specific vendor.
    /// </summary>
    /// <param name="vendor">Vendor name</param>
    /// <returns>List of vendor configurations</returns>
    Task<Result<IReadOnlyList<VendorConfiguration>>> GetByVendorAsync(string vendor);

    /// <summary>
    /// Gets all configurations for a specific standard.
    /// </summary>
    /// <param name="standard">Standard name</param>
    /// <returns>List of vendor configurations</returns>
    Task<Result<IReadOnlyList<VendorConfiguration>>> GetByStandardAsync(string standard);

    /// <summary>
    /// Gets all configurations for a specific message type.
    /// </summary>
    /// <param name="messageType">Message type</param>
    /// <returns>List of vendor configurations</returns>
    Task<Result<IReadOnlyList<VendorConfiguration>>> GetByMessageTypeAsync(string messageType);

    /// <summary>
    /// Lists all available configurations.
    /// </summary>
    /// <returns>List of all vendor configurations</returns>
    Task<Result<IReadOnlyList<VendorConfiguration>>> ListAllAsync();
}