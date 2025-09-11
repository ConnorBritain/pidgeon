// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Application.Interfaces.Configuration;

/// <summary>
/// Multi-standard vendor detection service interface.
/// Provides unified vendor detection across HL7, FHIR, NCPDP, and other healthcare standards.
/// </summary>
public interface IMultiStandardVendorDetectionService : IVendorDetectionService
{
    /// <summary>
    /// Analyzes a collection of messages to infer vendor patterns across multiple standards.
    /// Automatically determines the healthcare standard and processes with appropriate plugin.
    /// </summary>
    /// <param name="messages">Collection of healthcare messages to analyze</param>
    /// <param name="options">Inference configuration options</param>
    /// <returns>Inferred vendor configuration with confidence metrics</returns>
    Task<Result<VendorConfiguration>> AnalyzeVendorPatternsAsync(
        IEnumerable<string> messages, 
        InferenceOptions options);

    /// <summary>
    /// Gets all vendor configurations across all supported standards.
    /// Provides unified view of vendor intelligence from all plugins.
    /// </summary>
    /// <returns>Collection of all available vendor configurations</returns>
    Task<Result<IReadOnlyList<VendorConfiguration>>> GetAllVendorConfigurationsAsync();

    /// <summary>
    /// Gets vendor configurations for a specific standard.
    /// Supports both exact standard names and standard families.
    /// </summary>
    /// <param name="standard">Healthcare standard name (e.g., "hl7", "fhir", "ncpdp")</param>
    /// <returns>Vendor configurations for the specified standard</returns>
    Task<Result<IReadOnlyList<VendorConfiguration>>> GetVendorConfigurationsForStandardAsync(string standard);

    /// <summary>
    /// Detects vendor patterns in a message using smart standard inference.
    /// Automatically determines the healthcare standard and routes to appropriate plugin.
    /// </summary>
    /// <param name="messageContent">Healthcare message content to analyze</param>
    /// <returns>Best matching vendor with confidence score</returns>
    Task<Result<VendorMatch>> DetectVendorAsync(string messageContent);
}