// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Adapters.Interfaces;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Domain.Messaging;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Standard-agnostic orchestrator for analyzing field population patterns across healthcare messages.
/// Delegates standard-specific logic to appropriate plugins while providing
/// unified interface for field pattern analysis across all healthcare standards.
/// Single responsibility: "Orchestrate field pattern analysis using standard plugins."
/// </summary>
internal class FieldPatternAnalysisService : PluginAccessorBase<FieldPatternAnalysisService, IStandardFieldAnalysisPlugin>, IFieldPatternAnalysisService
{
    private readonly IMessagingToConfigurationAdapter _adapter;
    private readonly IFieldStatisticsService _statisticsService;

    public FieldPatternAnalysisService(
        IStandardPluginRegistry pluginRegistry,
        IMessagingToConfigurationAdapter adapter,
        IFieldStatisticsService statisticsService,
        ILogger<FieldPatternAnalysisService> logger)
        : base(pluginRegistry, logger)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
    }

    /// <inheritdoc />
    public async Task<Result<FieldPatterns>> AnalyzeAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
            return Result<FieldPatterns>.Failure("No messages provided for analysis");

        _logger.LogInformation("Analyzing field patterns for {MessageCount} {Standard} {MessageType} messages",
            messageList.Count, standard, messageType);

        return await ExecutePluginOperationAsync(
            standard,
            registry => registry.GetFieldAnalysisPlugin(standard),
            async plugin =>
            {
                var patterns = await plugin.AnalyzeFieldPatternsAsync(messageList, messageType);

                if (patterns.IsSuccess)
                {
                    _logger.LogInformation("Field pattern analysis completed for {MessageType} with {SegmentCount} segment patterns",
                        messageType, patterns.Value.SegmentPatterns.Count);
                }

                return patterns;
            },
            "field pattern analysis");
    }

    /// <inheritdoc />
    public async Task<Result<SegmentPattern>> AnalyzeSegmentAsync(
        IEnumerable<string> segments,
        string segmentType,
        string standard)
    {
        var segmentList = segments.ToList();
        if (segmentList.Count == 0)
            return Result<SegmentPattern>.Failure("No segments provided for analysis");

        _logger.LogDebug("Analyzing {SegmentCount} {SegmentType} segments for {Standard}", 
            segmentList.Count, segmentType, standard);

        return await ExecutePluginOperationAsync(
            standard,
            registry => registry.GetFieldAnalysisPlugin(standard),
            async plugin =>
            {
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
            },
            "segment analysis");
    }

    /// <inheritdoc />
    public async Task<Result<ComponentPattern>> AnalyzeComponentPatternsAsync(
        IEnumerable<string> fieldValues,
        string fieldType,
        string standard)
    {
        var valueList = fieldValues.ToList();
        if (valueList.Count == 0)
            return Result<ComponentPattern>.Failure("No field values provided for analysis");

        if (string.IsNullOrWhiteSpace(standard))
            return Result<ComponentPattern>.Failure("Standard must be specified for component analysis");

        _logger.LogDebug("Analyzing component patterns for {ValueCount} {FieldType} values in {Standard}", 
            valueList.Count, fieldType, standard);

        return await ExecutePluginOperationAsync(
            standard,
            registry => registry.GetFieldAnalysisPlugin(standard),
            async plugin =>
            {
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
            },
            "component pattern analysis");
    }

    /// <inheritdoc />
    public async Task<Result<Application.Services.Configuration.FieldStatistics>> CalculateStatisticsAsync(FieldPatterns patterns)
    {
        if (patterns == null)
            return Result<Application.Services.Configuration.FieldStatistics>.Failure("Patterns cannot be null");

        // Get standard from the patterns object
        var standard = patterns.Standard;
        if (string.IsNullOrWhiteSpace(standard))
        {
            _logger.LogWarning("No standard specified in field patterns, cannot calculate statistics");
            return Result<Application.Services.Configuration.FieldStatistics>.Failure("Standard must be specified in field patterns for statistics calculation");
        }

        return await ExecutePluginOperationAsync(
            standard,
            registry => registry.GetFieldAnalysisPlugin(standard),
            async plugin =>
            {
                // Use dedicated field statistics service for proper domain separation
                var result = await _statisticsService.CalculateFieldStatisticsAsync(patterns);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Calculated statistics for {Standard} patterns: {TotalFields} fields with {ConsistentRate:P1} consistency",
                        standard, result.Value.TotalFields, result.Value.DataQualityScore);
                }

                return result;
            },
            "field statistics calculation");
    }
}