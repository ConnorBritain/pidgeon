// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Types;

namespace Pidgeon.Core.Services.Configuration;

/// <summary>
/// Service responsible for analyzing field population patterns across healthcare messages.
/// Identifies which fields are commonly populated, frequency patterns, and data structures.
/// Single responsibility: "How are fields typically populated in this vendor's messages?"
/// </summary>
public interface IFieldPatternAnalyzer
{
    /// <summary>
    /// Analyzes field population patterns across multiple messages.
    /// Identifies frequency of field usage, common values, and population statistics.
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="standard">Healthcare standard (HL7v23, FHIRv4, etc.)</param>
    /// <param name="messageType">Specific message type to analyze</param>
    /// <returns>Result containing comprehensive field patterns analysis</returns>
    Task<Result<FieldPatterns>> AnalyzeAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType);

    /// <summary>
    /// Analyzes patterns for a specific segment or resource type.
    /// Useful for focused analysis of particular message components.
    /// </summary>
    /// <param name="segments">Collection of segment/resource data to analyze</param>
    /// <param name="segmentType">Type of segment or resource (PID, Patient, etc.)</param>
    /// <param name="standard">Healthcare standard</param>
    /// <returns>Result containing segment-specific field patterns</returns>
    Task<Result<SegmentPattern>> AnalyzeSegmentAsync(
        IEnumerable<string> segments,
        string segmentType,
        string standard);

    /// <summary>
    /// Detects component patterns within composite fields.
    /// For HL7: Analyzes XPN, XAD, CE component structures.
    /// For FHIR: Analyzes complex data types like HumanName, Address.
    /// </summary>
    /// <param name="fieldValues">Collection of field values to analyze for component patterns</param>
    /// <param name="fieldType">Type of field being analyzed</param>
    /// <param name="standard">Healthcare standard for proper plugin selection</param>
    /// <returns>Result containing component pattern analysis</returns>
    Task<Result<ComponentPattern>> AnalyzeComponentPatternsAsync(
        IEnumerable<string> fieldValues,
        string fieldType,
        string standard);

    /// <summary>
    /// Calculates field population statistics for a given set of patterns.
    /// Provides metrics on consistency, completeness, and data quality.
    /// </summary>
    /// <param name="patterns">Field patterns to calculate statistics for</param>
    /// <returns>Result containing statistical analysis of field patterns</returns>
    Task<Result<FieldStatistics>> CalculateStatisticsAsync(FieldPatterns patterns);
}

/// <summary>
/// Service responsible for detecting message format deviations and encoding variations.
/// Identifies non-standard implementations and vendor-specific formatting choices.
/// Single responsibility: "How does this vendor deviate from the standard format?"
/// </summary>
public interface IFormatDeviationDetector
{
    /// <summary>
    /// Detects encoding variations in messages (field separators, escape characters, etc.).
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="standard">Healthcare standard</param>
    /// <returns>Result containing detected encoding deviations</returns>
    Task<Result<IReadOnlyList<FormatDeviation>>> DetectEncodingDeviationsAsync(
        IEnumerable<string> messages,
        string standard);

    /// <summary>
    /// Detects structural deviations (extra fields, non-standard segments, ordering changes).
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="standard">Healthcare standard</param>
    /// <param name="messageType">Specific message type</param>
    /// <returns>Result containing detected structural deviations</returns>
    Task<Result<IReadOnlyList<FormatDeviation>>> DetectStructuralDeviationsAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType);

    /// <summary>
    /// Analyzes severity and impact of detected deviations.
    /// Categorizes deviations by their impact on interoperability.
    /// </summary>
    /// <param name="deviations">Collection of format deviations to analyze</param>
    /// <returns>Result containing deviation impact analysis</returns>
    Task<Result<DeviationImpactAnalysis>> AnalyzeDeviationImpactAsync(
        IEnumerable<FormatDeviation> deviations);
}

/// <summary>
/// Service responsible for calculating confidence scores for configuration analysis.
/// Provides statistical confidence metrics based on sample size and consistency.
/// Single responsibility: "How confident are we in this configuration analysis?"
/// </summary>
public interface IConfidenceCalculator
{
    /// <summary>
    /// Calculates overall confidence score for a vendor configuration.
    /// Considers sample size, pattern consistency, and detection accuracy.
    /// </summary>
    /// <param name="configuration">Vendor configuration to score</param>
    /// <param name="sampleSize">Number of messages analyzed</param>
    /// <returns>Result containing confidence score (0.0 to 1.0)</returns>
    Task<Result<double>> CalculateConfigurationConfidenceAsync(
        VendorConfiguration configuration,
        int sampleSize);

    /// <summary>
    /// Calculates confidence score for field pattern analysis.
    /// Based on consistency of field population across sample messages.
    /// </summary>
    /// <param name="patterns">Field patterns to score</param>
    /// <param name="sampleSize">Number of messages analyzed</param>
    /// <returns>Result containing field pattern confidence score</returns>
    Task<Result<double>> CalculateFieldPatternConfidenceAsync(
        FieldPatterns patterns,
        int sampleSize);

    /// <summary>
    /// Calculates confidence score for vendor signature detection.
    /// Based on strength of vendor indicators and detection consistency.
    /// </summary>
    /// <param name="signature">Vendor signature to score</param>
    /// <param name="detectionCriteria">Criteria used for detection</param>
    /// <returns>Result containing vendor detection confidence score</returns>
    Task<Result<double>> CalculateVendorDetectionConfidenceAsync(
        VendorSignature signature,
        VendorDetectionCriteria detectionCriteria);
}