// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Domain.Messaging;

namespace Pidgeon.Core.Adapters.Interfaces;

/// <summary>
/// Adapter interface for translating between Messaging Domain and Configuration Domain.
/// Handles the architectural boundary between message structures and vendor patterns.
/// 
/// DOMAIN BOUNDARY: Messaging → Configuration
/// - INPUT: Messaging Domain types (HL7Message, field positions as int, segment structures)
/// - OUTPUT: Configuration Domain types (FieldPatterns, VendorConfiguration, string-based paths)
/// 
/// RESPONSIBILITY: Clean translation between domain representations without leaking
/// messaging concepts into configuration domain or vice versa.
/// </summary>
public interface IMessagingToConfigurationAdapter
{
    /// <summary>
    /// Analyzes messaging structures to extract field population patterns.
    /// Converts from messaging domain (int field positions) to configuration domain (string field paths).
    /// </summary>
    /// <param name="messages">Collection of healthcare messages to analyze</param>
    /// <returns>Field patterns using configuration domain semantics</returns>
    Task<FieldPatterns> AnalyzePatternsAsync(IEnumerable<HealthcareMessage> messages);

    /// <summary>
    /// Infers vendor configuration from message samples.
    /// Extracts vendor signatures, field patterns, and format deviations.
    /// </summary>
    /// <param name="messages">Collection of messages from the same vendor/system</param>
    /// <returns>Complete vendor configuration profile</returns>
    Task<VendorConfiguration> InferConfigurationAsync(IEnumerable<HealthcareMessage> messages);

    /// <summary>
    /// Detects format deviations by comparing messages against a known configuration.
    /// Used for configuration validation and drift detection.
    /// </summary>
    /// <param name="message">Message to validate</param>
    /// <param name="configuration">Expected vendor configuration</param>
    /// <returns>List of detected format deviations</returns>
    Task<List<FormatDeviation>> DetectDeviationsAsync(HealthcareMessage message, VendorConfiguration configuration);

    /// <summary>
    /// Calculates field statistics from message analysis.
    /// Handles the conversion between messaging domain field representations 
    /// and configuration domain statistical patterns.
    /// </summary>
    /// <param name="messages">Messages to analyze for field statistics</param>
    /// <returns>Statistical analysis using configuration domain types</returns>
    Task<Dictionary<string, FieldFrequency>> CalculateFieldStatisticsAsync(IEnumerable<HealthcareMessage> messages);
}