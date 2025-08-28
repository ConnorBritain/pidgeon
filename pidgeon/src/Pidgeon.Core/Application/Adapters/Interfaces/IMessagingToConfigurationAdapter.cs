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

    /// <summary>
    /// Analyzes patterns for a specific segment or resource type across messages.
    /// Focuses analysis on a single segment/resource structure and usage patterns.
    /// </summary>
    /// <param name="messages">Collection of healthcare messages containing the segment/resource</param>
    /// <param name="segmentType">Type of segment or resource to analyze (MSH, PID, Patient, etc.)</param>
    /// <returns>Segment-specific pattern analysis</returns>
    Task<SegmentPattern> AnalyzeSegmentPatternsAsync(
        IEnumerable<HealthcareMessage> messages, 
        string segmentType);

    /// <summary>
    /// Analyzes component structure patterns within composite fields across messages.
    /// Extracts patterns for complex data types and composite field usage.
    /// For HL7: XPN, XAD, CE component patterns and population frequency
    /// For FHIR: Complex data type patterns (Address, HumanName, CodeableConcept)
    /// For NCPDP: Composite field structures and component usage
    /// </summary>
    /// <param name="messages">Collection of healthcare messages containing composite fields</param>
    /// <param name="fieldType">Type of composite field to analyze (XPN, Address, etc.)</param>
    /// <returns>Component pattern analysis</returns>
    Task<ComponentPattern> AnalyzeComponentPatternsAsync(
        IEnumerable<HealthcareMessage> messages,
        string fieldType);
}