// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Orchestrator for comprehensive message pattern analysis.
/// Coordinates between different analysis services without implementing analysis logic itself.
/// Follows Single Responsibility Principle by focusing solely on orchestration.
/// </summary>
internal class MessagePatternAnalysisOrchestrator : IMessagePatternAnalysisOrchestrator
{
    private readonly IFieldPatternAnalysisService _fieldAnalyzer;
    private readonly IConfidenceCalculationService _confidenceCalculator;
    private readonly INullValueToleranceAnalysisService _nullToleranceAnalyzer;
    private readonly ILogger<MessagePatternAnalysisOrchestrator> _logger;

    public MessagePatternAnalysisOrchestrator(
        IFieldPatternAnalysisService fieldAnalyzer,
        IConfidenceCalculationService confidenceCalculator,
        INullValueToleranceAnalysisService nullToleranceAnalyzer,
        ILogger<MessagePatternAnalysisOrchestrator> logger)
    {
        _fieldAnalyzer = fieldAnalyzer ?? throw new ArgumentNullException(nameof(fieldAnalyzer));
        _confidenceCalculator = confidenceCalculator ?? throw new ArgumentNullException(nameof(confidenceCalculator));
        _nullToleranceAnalyzer = nullToleranceAnalyzer ?? throw new ArgumentNullException(nameof(nullToleranceAnalyzer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Orchestrates comprehensive analysis by coordinating between analysis services.
    /// </summary>
    /// <param name="messages">Messages to analyze</param>
    /// <param name="standard">Healthcare standard (e.g., HL7v23)</param>
    /// <param name="messageType">Message type (e.g., ADT^A01)</param>
    /// <returns>Complete message pattern with all analysis results</returns>
    public async Task<Result<MessagePattern>> PerformComprehensiveAnalysisAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType)
    {
        try
        {
            var messageList = messages.ToList();
            _logger.LogInformation("Orchestrating comprehensive pattern analysis for {MessageCount} {Standard} {MessageType} messages",
                messageList.Count, standard, messageType);

            // Step 1: Field frequency analysis (delegates to service)
            _logger.LogDebug("Step 1: Field frequency analysis");
            var patternsResult = await _fieldAnalyzer.AnalyzeAsync(messageList, standard, messageType);
            if (patternsResult.IsFailure)
                return Result<MessagePattern>.Failure($"Field pattern analysis failed: {patternsResult.Error}");

            // Step 2: Extract field frequencies from patterns
            _logger.LogDebug("Step 2: Extract field frequencies");
            var fieldFrequencies = ExtractFieldFrequencies(patternsResult.Value);

            // Step 3: Null value tolerance analysis (delegates to service)
            _logger.LogDebug("Step 3: Null tolerance analysis");
            var nullToleranceResult = _nullToleranceAnalyzer.CalculateNullTolerance(fieldFrequencies);
            if (nullToleranceResult.IsFailure)
                _logger.LogWarning("Null tolerance analysis failed: {Error}", nullToleranceResult.Error);

            // Step 4: Calculate confidence score (delegates to service)
            _logger.LogDebug("Step 4: Confidence calculation");
            var confidenceResult = await _confidenceCalculator.CalculateFieldPatternConfidenceAsync(patternsResult.Value, messageList.Count);
            if (confidenceResult.IsFailure)
                _logger.LogWarning("Confidence calculation failed: {Error}", confidenceResult.Error);

            // Step 5: Assemble comprehensive pattern (pure orchestration logic)
            var messagePattern = AssembleMessagePattern(
                standard, 
                messageType, 
                messageList.Count,
                fieldFrequencies,
                patternsResult.Value,
                nullToleranceResult.IsSuccess ? nullToleranceResult.Value : new Dictionary<string, double>(),
                confidenceResult.IsSuccess ? confidenceResult.Value : 0.5);

            _logger.LogInformation("Comprehensive pattern analysis completed with {Confidence:P1} confidence",
                messagePattern.Confidence);

            return Result<MessagePattern>.Success(messagePattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error orchestrating comprehensive pattern analysis for {Standard} {MessageType}", standard, messageType);
            return Result<MessagePattern>.Failure($"Comprehensive analysis failed: {ex.Message}");
        }
    }

    #region Private Orchestration Helpers

    /// <summary>
    /// Extracts field frequencies from field patterns analysis results.
    /// Pure data transformation logic.
    /// </summary>
    private static Dictionary<string, FieldFrequency> ExtractFieldFrequencies(FieldPatterns patterns)
    {
        var fieldFrequencies = new Dictionary<string, FieldFrequency>();

        // Transform segment patterns into field frequencies
        foreach (var segmentPattern in patterns.SegmentPatterns)
        {
            foreach (var field in segmentPattern.Value.Fields)
            {
                var fieldPath = $"{segmentPattern.Key}.{field.Key}";
                fieldFrequencies[fieldPath] = field.Value;
            }
        }

        return fieldFrequencies;
    }

    /// <summary>
    /// Assembles complete message pattern from analysis results.
    /// Pure data assembly logic.
    /// </summary>
    private static MessagePattern AssembleMessagePattern(
        string standard,
        string messageType,
        int sampleSize,
        Dictionary<string, FieldFrequency> fieldFrequencies,
        FieldPatterns fieldPatterns,
        Dictionary<string, double> nullTolerance,
        double confidence)
    {
        return new MessagePattern
        {
            Standard = standard,
            MessageType = messageType,
            SampleSize = sampleSize,
            FieldFrequencies = fieldFrequencies,
            ComponentPatterns = new Dictionary<string, ComponentPattern>(), // Component analysis not implemented yet
            NullTolerance = nullTolerance.Values.Any() ? nullTolerance.Values.Average() : 0.0,
            Confidence = confidence,
            AnalysisDate = DateTime.UtcNow,
            SegmentPatterns = fieldPatterns.SegmentPatterns
        };
    }

    #endregion
}