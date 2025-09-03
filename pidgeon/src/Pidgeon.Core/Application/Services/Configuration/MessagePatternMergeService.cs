// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Service for merging message patterns with complex aggregation logic.
/// Single responsibility: Pattern merging and statistical combination.
/// </summary>
public interface IMessagePatternMergeService
{
    /// <summary>
    /// Merges two message patterns combining their statistical data.
    /// </summary>
    /// <param name="primary">Primary message pattern</param>
    /// <param name="secondary">Secondary pattern to merge into primary</param>
    /// <returns>Merged message pattern with combined statistics</returns>
    Result<MessagePattern> MergePatterns(MessagePattern primary, MessagePattern secondary);
}

/// <summary>
/// Implementation of message pattern merging service.
/// Handles complex statistical aggregation and pattern combination logic.
/// </summary>
internal class MessagePatternMergeService : IMessagePatternMergeService
{
    private readonly ILogger<MessagePatternMergeService> _logger;

    public MessagePatternMergeService(ILogger<MessagePatternMergeService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Result<MessagePattern> MergePatterns(MessagePattern primary, MessagePattern secondary)
    {
        try
        {
            _logger.LogDebug("Merging patterns for message type {MessageType}", primary.MessageType);

            // Validate compatibility
            var validationResult = ValidateMergeCompatibility(primary, secondary);
            if (validationResult.IsFailure)
                return Result<MessagePattern>.Failure(validationResult.Error);

            // Merge segment patterns with statistical aggregation
            var mergedSegmentPatterns = MergeSegmentPatterns(primary.SegmentPatterns, secondary.SegmentPatterns);

            // Create merged pattern
            var mergedPattern = primary with
            {
                Frequency = primary.Frequency + secondary.Frequency,
                SegmentPatterns = mergedSegmentPatterns,
                SampleSize = primary.SampleSize + secondary.SampleSize,
                Confidence = CalculateMergedConfidence(primary, secondary),
                AnalysisDate = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully merged patterns for {MessageType}", primary.MessageType);
            return Result<MessagePattern>.Success(mergedPattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging message patterns for {MessageType}", primary.MessageType);
            return Result<MessagePattern>.Failure($"Pattern merge failed: {ex.Message}");
        }
    }

    #region Private Merge Logic

    /// <summary>
    /// Validates that two patterns can be merged together.
    /// </summary>
    private static Result ValidateMergeCompatibility(MessagePattern primary, MessagePattern secondary)
    {
        if (primary.MessageType != secondary.MessageType)
            return Result.Failure($"Cannot merge patterns of different message types: {primary.MessageType} vs {secondary.MessageType}");

        if (primary.Standard != secondary.Standard)
            return Result.Failure($"Cannot merge patterns of different standards: {primary.Standard} vs {secondary.Standard}");

        return Result.Success();
    }

    /// <summary>
    /// Merges segment patterns combining field frequency statistics.
    /// </summary>
    private Dictionary<string, SegmentPattern> MergeSegmentPatterns(
        Dictionary<string, SegmentPattern> primary,
        Dictionary<string, SegmentPattern> secondary)
    {
        var merged = new Dictionary<string, SegmentPattern>(primary);

        foreach (var kvp in secondary)
        {
            if (merged.ContainsKey(kvp.Key))
            {
                // Merge existing segment pattern
                merged[kvp.Key] = MergeSegmentPattern(merged[kvp.Key], kvp.Value);
            }
            else
            {
                // Add new segment pattern
                merged[kvp.Key] = kvp.Value;
            }
        }

        return merged;
    }

    /// <summary>
    /// Merges individual segment patterns combining field frequencies.
    /// </summary>
    private static SegmentPattern MergeSegmentPattern(SegmentPattern primary, SegmentPattern secondary)
    {
        var mergedFields = new Dictionary<int, FieldFrequency>(primary.Fields);

        foreach (var fieldKvp in secondary.Fields)
        {
            if (mergedFields.ContainsKey(fieldKvp.Key))
            {
                // Merge field frequency statistics
                mergedFields[fieldKvp.Key] = MergeFieldFrequency(mergedFields[fieldKvp.Key], fieldKvp.Value);
            }
            else
            {
                // Add new field frequency
                mergedFields[fieldKvp.Key] = fieldKvp.Value;
            }
        }

        return primary with
        {
            Fields = mergedFields,
            PopulatedCount = primary.PopulatedCount + secondary.PopulatedCount,
            TotalCount = primary.TotalCount + secondary.TotalCount,
            SampleSize = primary.SampleSize + secondary.SampleSize
        };
    }

    /// <summary>
    /// Merges field frequency data combining statistical counts and common values.
    /// </summary>
    private static FieldFrequency MergeFieldFrequency(FieldFrequency primary, FieldFrequency secondary)
    {
        var totalPopulated = primary.PopulatedCount + secondary.PopulatedCount;
        var totalCount = primary.TotalCount + secondary.TotalCount;

        // Combine common values dictionaries
        var mergedCommonValues = new Dictionary<string, int>(primary.CommonValues);
        foreach (var kvp in secondary.CommonValues)
        {
            if (mergedCommonValues.ContainsKey(kvp.Key))
                mergedCommonValues[kvp.Key] += kvp.Value;
            else
                mergedCommonValues[kvp.Key] = kvp.Value;
        }

        return new FieldFrequency
        {
            FieldIndex = primary.FieldIndex,
            FieldName = primary.FieldName,
            PopulatedCount = totalPopulated,
            TotalCount = totalCount,
            Frequency = totalCount > 0 ? (double)totalPopulated / totalCount : 0.0,
            CommonValues = mergedCommonValues,
            SampleSize = primary.SampleSize + secondary.SampleSize
        };
    }

    /// <summary>
    /// Calculates confidence score for merged pattern based on sample sizes.
    /// </summary>
    private static double CalculateMergedConfidence(MessagePattern primary, MessagePattern secondary)
    {
        var totalSamples = primary.SampleSize + secondary.SampleSize;
        var weightedConfidence = (primary.Confidence * primary.SampleSize + secondary.Confidence * secondary.SampleSize) / totalSamples;
        
        // Apply sample size bonus (larger samples generally increase confidence)
        var sampleSizeBonus = Math.Min(0.1, totalSamples / 1000.0);
        return Math.Min(1.0, weightedConfidence + sampleSizeBonus);
    }

    #endregion
}