// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Service responsible for analyzing field population patterns across healthcare messages.
/// Identifies which fields are commonly populated, frequency patterns, and data structures.
/// Single responsibility: "How are fields typically populated in this vendor's messages?"
/// </summary>
public interface IFieldPatternAnalysisService
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

