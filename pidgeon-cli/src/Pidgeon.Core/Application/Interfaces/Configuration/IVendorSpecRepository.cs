// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Application.Interfaces.Configuration;

/// <summary>
/// Repository for loading and managing detailed vendor specifications.
/// Handles complete interface documentation including field mappings, constraints, and business rules.
/// </summary>
public interface IVendorSpecRepository
{
    /// <summary>
    /// Configuration path where vendor specifications are stored.
    /// </summary>
    string ConfigurationPath { get; }

    /// <summary>
    /// Loads all available vendor specifications.
    /// </summary>
    /// <returns>Collection of all vendor specifications</returns>
    Task<Result<IReadOnlyList<VendorSpecification>>> LoadAllSpecificationsAsync();

    /// <summary>
    /// Loads specifications that support a specific HL7 standard.
    /// </summary>
    /// <param name="standard">HL7 standard (e.g., "HL7v23", "FHIR")</param>
    /// <returns>Vendor specifications supporting the standard</returns>
    Task<Result<IReadOnlyList<VendorSpecification>>> LoadSpecificationsForStandardAsync(string standard);

    /// <summary>
    /// Gets a specific vendor specification by ID.
    /// </summary>
    /// <param name="specificationId">Unique specification identifier</param>
    /// <returns>Vendor specification if found, null otherwise</returns>
    Task<Result<VendorSpecification?>> GetSpecificationAsync(string specificationId);

    /// <summary>
    /// Gets specifications for a specific vendor name.
    /// </summary>
    /// <param name="vendorName">Vendor name (e.g., "Epic", "Cerner")</param>
    /// <returns>All specifications for the vendor</returns>
    Task<Result<IReadOnlyList<VendorSpecification>>> GetSpecificationsForVendorAsync(string vendorName);

    /// <summary>
    /// Saves a vendor specification to storage.
    /// </summary>
    /// <param name="specification">Vendor specification to save</param>
    /// <returns>Success or failure result</returns>
    Task<Result> SaveSpecificationAsync(VendorSpecification specification);

    /// <summary>
    /// Refreshes the specification cache from storage.
    /// </summary>
    /// <returns>Success or failure result</returns>
    Task<Result> RefreshSpecificationsAsync();

    /// <summary>
    /// Creates default vendor specifications if none exist.
    /// Used for initial setup and testing scenarios.
    /// </summary>
    /// <returns>Success or failure result</returns>
    Task<Result> CreateDefaultSpecificationsAsync();
}