// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Service responsible for analyzing message patterns and detecting statistical patterns.
/// Orchestrates field population frequency analysis, null value tolerance mapping,
/// and statistical confidence scoring across healthcare message samples.
/// Single responsibility: "What are the statistical patterns in these messages?"
/// </summary>
public interface IMessagePatternAnalysisService
{
    /// <summary>
    /// Analyzes field population frequency across a collection of messages.
    /// Identifies which fields are commonly populated and their frequency patterns.
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="standard">Healthcare standard (HL7v23, FHIRv4, etc.)</param>
    /// <param name="messageType">Specific message type for targeted analysis</param>
    /// <returns>Result containing field frequency analysis</returns>
    Task<Result<Dictionary<string, FieldFrequency>>> AnalyzeFieldFrequencyAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType);

    /// <summary>
    /// Detects component structure patterns within composite fields.
    /// Analyzes how complex data types are typically populated and structured.
    /// For HL7: XPN, XAD, CE patterns; For FHIR: Complex data types; For NCPDP: Composite elements.
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="standard">Healthcare standard</param>
    /// <param name="messageType">Message type for context-specific analysis</param>
    /// <returns>Result containing component pattern analysis</returns>
    Task<Result<Dictionary<string, ComponentPattern>>> DetectComponentPatternsAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType);

    /// <summary>
    /// Calculates statistical confidence score for pattern analysis results.
    /// Provides confidence scoring based on sample size, consistency, and pattern strength.
    /// Target accuracy: 85%+ for valid pattern detection.
    /// </summary>
    /// <param name="patterns">Field patterns to calculate confidence for</param>
    /// <param name="sampleSize">Number of messages analyzed</param>
    /// <param name="standard">Healthcare standard for standard-specific confidence calculation</param>
    /// <returns>Result containing confidence score between 0.0 and 1.0</returns>
    Task<Result<double>> CalculateConfidenceScoreAsync(
        FieldPatterns patterns,
        int sampleSize,
        string standard);

    /// <summary>
    /// Analyzes null value tolerance and identifies optional vs required field patterns.
    /// Maps which fields can be safely omitted vs those that must be populated.
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="standard">Healthcare standard</param>
    /// <param name="messageType">Message type for requirement analysis</param>
    /// <returns>Result containing null tolerance analysis</returns>
    Task<Result<Dictionary<string, double>>> AnalyzeNullValueToleranceAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType);

    /// <summary>
    /// Performs comprehensive statistical analysis combining all pattern detection methods.
    /// Provides complete picture of message structure and population patterns.
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="standard">Healthcare standard</param>
    /// <param name="messageType">Message type</param>
    /// <returns>Result containing comprehensive pattern analysis</returns>
    Task<Result<MessagePattern>> PerformComprehensiveAnalysisAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType);
}