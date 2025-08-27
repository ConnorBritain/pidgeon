// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Types;

namespace Pidgeon.Core.Services.Configuration;

/// <summary>
/// Interface for standard-specific configuration analysis plugins.
/// Each healthcare standard (HL7v23, FHIRv4, NCPDPScript) implements this interface
/// as a thin orchestration layer that coordinates specialized domain services.
/// The plugin delegates actual business logic to injectable domain services.
/// </summary>
public interface IConfigurationPlugin
{
    /// <summary>
    /// Gets the name of the standard this plugin handles.
    /// </summary>
    string StandardName { get; }

    /// <summary>
    /// Determines if this plugin can handle configuration analysis for the given address.
    /// </summary>
    /// <param name="address">The configuration address to check</param>
    /// <returns>True if this plugin can handle the standard specified in the address</returns>
    bool CanHandle(ConfigurationAddress address);

    /// <summary>
    /// Orchestrates complete configuration analysis workflow for this standard.
    /// Coordinates vendor detection, field pattern analysis, and confidence scoring
    /// by delegating to specialized domain services.
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="address">Configuration address specifying vendor/standard/message type</param>
    /// <param name="options">Optional analysis configuration</param>
    /// <returns>Result containing complete vendor configuration with all analysis results</returns>
    Task<Result<VendorConfiguration>> AnalyzeMessagesAsync(
        IEnumerable<string> messages,
        ConfigurationAddress address,
        InferenceOptions? options = null);

    /// <summary>
    /// Validates a message against a vendor configuration using standard-specific rules.
    /// Delegates to the appropriate validation service for this standard.
    /// </summary>
    /// <param name="message">The message to validate</param>
    /// <param name="configuration">The vendor configuration to validate against</param>
    /// <returns>Result containing validation results with standard-specific analysis</returns>
    Task<Result<ConfigurationValidationResult>> ValidateMessageAsync(
        string message,
        VendorConfiguration configuration);
}