// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Standards.Common;

/// <summary>
/// Plugin interface for standard-specific field pattern analysis.
/// Each healthcare standard (HL7, FHIR, NCPDP) implements this interface
/// to provide specialized logic for analyzing field population patterns and structures.
/// Follows plugin architecture sacred principle: standard logic isolated in plugins.
/// </summary>
public interface IStandardFieldAnalysisPlugin
{
    /// <summary>
    /// The healthcare standard name this plugin handles (HL7v23, FHIRv4, etc.).
    /// </summary>
    string StandardName { get; }

    /// <summary>
    /// Determines if this plugin can handle field analysis for the given standard.
    /// </summary>
    /// <param name="standard">Healthcare standard name</param>
    /// <returns>True if this plugin can handle the standard</returns>
    bool CanHandle(string standard);

    /// <summary>
    /// Analyzes field population patterns across a collection of messages.
    /// For HL7: Segment field frequency, component patterns, data type analysis.
    /// For FHIR: Element population frequency, resource structure patterns.
    /// For NCPDP: Field usage patterns, data element frequency.
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="messageType">Specific message type for pattern analysis</param>
    /// <returns>Result containing field patterns and frequency analysis</returns>
    Task<Result<FieldPatterns>> AnalyzeFieldPatternsAsync(
        IEnumerable<string> messages,
        string messageType);

    /// <summary>
    /// Analyzes patterns for a specific segment or resource type.
    /// Focuses analysis on a single segment/resource across multiple messages.
    /// </summary>
    /// <param name="segments">Collection of segment/resource strings to analyze</param>
    /// <param name="segmentType">Type of segment or resource (MSH, PID, Patient, etc.)</param>
    /// <returns>Result containing segment-specific pattern analysis</returns>
    Task<Result<SegmentPattern>> AnalyzeSegmentPatternsAsync(
        IEnumerable<string> segments,
        string segmentType);

    /// <summary>
    /// Analyzes component structure patterns within composite fields.
    /// For HL7: XPN, XAD, CE component patterns and usage.
    /// For FHIR: Complex data type patterns (Address, HumanName, etc.).
    /// For NCPDP: Composite field structures and patterns.
    /// </summary>
    /// <param name="fieldValues">Collection of field values to analyze for component patterns</param>
    /// <param name="fieldType">Type of field being analyzed (XPN, Address, etc.)</param>
    /// <returns>Result containing component pattern analysis</returns>
    Task<Result<ComponentPattern>> AnalyzeComponentPatternsAsync(
        IEnumerable<string> fieldValues,
        string fieldType);

    /// <summary>
    /// Calculates statistical confidence and quality metrics for field patterns.
    /// Provides confidence scoring for the reliability of pattern analysis results.
    /// </summary>
    /// <param name="patterns">Field patterns to calculate statistics for</param>
    /// <returns>Result containing statistical analysis of field patterns</returns>
    Task<Result<FieldStatistics>> CalculateFieldStatisticsAsync(FieldPatterns patterns);

    /// <summary>
    /// Calculates field coverage score based on standard-specific expectations.
    /// For HL7: Expected segments and fields for message types (ADT expects PID, PV1, etc.)
    /// For FHIR: Required elements and resources for profiles
    /// For NCPDP: Required fields for transaction types
    /// </summary>
    /// <param name="patterns">Field patterns to evaluate coverage for</param>
    /// <returns>Coverage score between 0.0 and 1.0</returns>
    Task<Result<double>> CalculateFieldCoverageAsync(FieldPatterns patterns);
}