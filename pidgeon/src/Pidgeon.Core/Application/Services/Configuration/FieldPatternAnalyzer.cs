// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Infrastructure.Standards.Abstractions;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Adapters.Interfaces;
using Pidgeon.Core.Domain.Configuration.Services;
using Pidgeon.Core.Domain.Messaging;

namespace Pidgeon.Core.Domain.Configuration.Services;

/// <summary>
/// Standard-agnostic orchestrator for analyzing field population patterns across healthcare messages.
/// Delegates standard-specific logic to appropriate plugins while providing
/// unified interface for field pattern analysis across all healthcare standards.
/// Single responsibility: "Orchestrate field pattern analysis using standard plugins."
/// </summary>
internal class FieldPatternAnalyzer : IFieldPatternAnalyzer
{
    private readonly IStandardPluginRegistry _pluginRegistry;
    private readonly IMessagingToConfigurationAdapter _adapter;
    private readonly IFieldStatisticsService _statisticsService;
    private readonly ILogger<FieldPatternAnalyzer> _logger;

    public FieldPatternAnalyzer(
        IStandardPluginRegistry pluginRegistry,
        IMessagingToConfigurationAdapter adapter,
        IFieldStatisticsService statisticsService,
        ILogger<FieldPatternAnalyzer> logger)
    {
        _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<FieldPatterns>> AnalyzeAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType)
    {
        try
        {
            var messageList = messages.ToList();
            if (messageList.Count == 0)
                return Result<FieldPatterns>.Failure("No messages provided for analysis");

            _logger.LogInformation("Analyzing field patterns for {MessageCount} {Standard} {MessageType} messages",
                messageList.Count, standard, messageType);

            var plugin = _pluginRegistry.GetFieldAnalysisPlugin(standard);
            if (plugin == null)
            {
                _logger.LogWarning("No field analysis plugin found for standard: {Standard}", standard);
                return Result<FieldPatterns>.Failure($"No field analysis plugin available for standard: {standard}");
            }

            var patterns = await plugin.AnalyzeFieldPatternsAsync(messageList, messageType);

            if (patterns.IsSuccess)
            {
                _logger.LogInformation("Field pattern analysis completed for {MessageType} with {SegmentCount} segment patterns",
                    messageType, patterns.Value.SegmentPatterns.Count);
            }

            return patterns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing field patterns");
            return Result<FieldPatterns>.Failure($"Field pattern analysis failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<SegmentPattern>> AnalyzeSegmentAsync(
        IEnumerable<string> segments,
        string segmentType,
        string standard)
    {
        try
        {
            var segmentList = segments.ToList();
            if (segmentList.Count == 0)
                return Result<SegmentPattern>.Failure("No segments provided for analysis");

            _logger.LogDebug("Analyzing {SegmentCount} {SegmentType} segments for {Standard}", 
                segmentList.Count, segmentType, standard);

            var plugin = _pluginRegistry.GetFieldAnalysisPlugin(standard);
            if (plugin == null)
            {
                _logger.LogWarning("No field analysis plugin found for standard: {Standard}", standard);
                return Result<SegmentPattern>.Failure($"No field analysis plugin available for standard: {standard}");
            }

            // Create minimal valid messages containing each segment for analysis
            var messageStrings = segmentList.Select(segment => 
                $"MSH|^~\\&|SYSTEM||RECEIVER||{DateTime.UtcNow:yyyyMMddHHmmss}||{segmentType}|{Guid.NewGuid()}|P|2.3\r{segment}"
            ).ToList();

            // Use existing plugin method for full message analysis
            var patternsResult = await plugin.AnalyzeFieldPatternsAsync(messageStrings, segmentType);
            if (patternsResult.IsFailure)
                return Result<SegmentPattern>.Failure(patternsResult.Error.Message);

            // Extract specific segment pattern from full analysis
            var segmentPattern = patternsResult.Value.SegmentPatterns.GetValueOrDefault(segmentType);
            var result = segmentPattern != null 
                ? Result<SegmentPattern>.Success(segmentPattern)
                : Result<SegmentPattern>.Success(new SegmentPattern
                {
                    SegmentId = segmentType,
                    FieldFrequencies = new Dictionary<int, FieldFrequency>(),
                    SampleSize = segmentList.Count
                });
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("Segment analysis completed for {SegmentType} with {FieldCount} fields",
                    segmentType, result.Value.Fields.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing segment {SegmentType}", segmentType);
            return Result<SegmentPattern>.Failure($"Segment analysis failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<ComponentPattern>> AnalyzeComponentPatternsAsync(
        IEnumerable<string> fieldValues,
        string fieldType,
        string standard)
    {
        try
        {
            var valueList = fieldValues.ToList();
            if (valueList.Count == 0)
                return Result<ComponentPattern>.Failure("No field values provided for analysis");

            if (string.IsNullOrWhiteSpace(standard))
                return Result<ComponentPattern>.Failure("Standard must be specified for component analysis");

            _logger.LogDebug("Analyzing component patterns for {ValueCount} {FieldType} values in {Standard}", 
                valueList.Count, fieldType, standard);

            var plugin = _pluginRegistry.GetFieldAnalysisPlugin(standard);
            if (plugin == null)
            {
                _logger.LogWarning("No field analysis plugin found for standard: {Standard}", standard);
                return Result<ComponentPattern>.Failure($"No field analysis plugin available for standard: {standard}");
            }

            // Create minimal valid messages containing each field value for analysis
            var messageStrings = valueList.Select(fieldValue => 
                $"MSH|^~\\&|SYSTEM||RECEIVER||{DateTime.UtcNow:yyyyMMddHHmmss}||ADT^A01|{Guid.NewGuid()}|P|2.3\rPID|1||12345||{fieldValue}||||||||||||||||||||||||||||"
            ).ToList();

            // Use existing plugin method for full message analysis
            var patternsResult = await plugin.AnalyzeFieldPatternsAsync(messageStrings, "ADT^A01");
            if (patternsResult.IsFailure)
                return Result<ComponentPattern>.Failure(patternsResult.Error.Message);

            // Extract component pattern from PID segment analysis
            var pidPattern = patternsResult.Value.SegmentPatterns.GetValueOrDefault("PID");
            var fieldFrequency = pidPattern?.FieldFrequencies.Values.FirstOrDefault(f => f.FieldName == fieldType);
            
            var result = fieldFrequency?.ComponentPatterns.Any() == true
                ? Result<ComponentPattern>.Success(new ComponentPattern
                {
                    FieldType = fieldType,
                    ComponentFrequencies = fieldFrequency.ComponentPatterns.ToDictionary(
                        kvp => int.Parse(kvp.Key), 
                        kvp => new ComponentFrequency 
                        { 
                            ComponentIndex = int.Parse(kvp.Key), 
                            PopulatedCount = kvp.Value.ComponentFrequencies.Values.Sum(cf => cf.PopulatedCount),
                            TotalCount = valueList.Count,
                            ComponentName = kvp.Key
                        }),
                    SampleSize = valueList.Count
                })
                : Result<ComponentPattern>.Success(new ComponentPattern
                {
                    FieldType = fieldType,
                    ComponentFrequencies = new Dictionary<int, ComponentFrequency>(),
                    SampleSize = valueList.Count
                });
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("Component pattern analysis completed for {FieldType} with {ComponentCount} components",
                    fieldType, result.Value.ComponentFrequencies.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing component patterns for {FieldType} in {Standard}", fieldType, standard);
            return Result<ComponentPattern>.Failure($"Component analysis failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<FieldStatistics>> CalculateStatisticsAsync(FieldPatterns patterns)
    {
        try
        {
            if (patterns == null)
                return Result<FieldStatistics>.Failure("Patterns cannot be null");

            // Get standard from the patterns object
            var standard = patterns.Standard;
            if (string.IsNullOrWhiteSpace(standard))
            {
                _logger.LogWarning("No standard specified in field patterns, cannot calculate statistics");
                return Result<FieldStatistics>.Failure("Standard must be specified in field patterns for statistics calculation");
            }

            var plugin = _pluginRegistry.GetFieldAnalysisPlugin(standard);
            if (plugin == null)
            {
                _logger.LogWarning("No field analysis plugin found for standard: {Standard}", standard);
                return Result<FieldStatistics>.Failure($"No field analysis plugin available for standard: {standard}");
            }

            // Use dedicated field statistics service for proper domain separation
            var result = await _statisticsService.CalculateFieldStatisticsAsync(patterns);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Calculated statistics for {Standard} patterns: {TotalFields} fields with {ConsistentRate:P1} consistency",
                    standard, result.Value.TotalFields, result.Value.DataQualityScore);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating field statistics");
            return Result<FieldStatistics>.Failure($"Statistics calculation failed: {ex.Message}");
        }
    }
}