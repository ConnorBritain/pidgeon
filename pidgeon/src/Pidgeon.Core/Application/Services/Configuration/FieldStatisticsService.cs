// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Service implementation for domain-agnostic field statistics and coverage calculations.
/// Works with any FieldPatterns regardless of the source healthcare standard.
/// </summary>
internal class FieldStatisticsService : IFieldStatisticsService
{
    private readonly ILogger<FieldStatisticsService> _logger;

    public FieldStatisticsService(ILogger<FieldStatisticsService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<FieldStatistics>> CalculateFieldStatisticsAsync(FieldPatterns patterns)
    {
        try
        {
            if (patterns == null)
            {
                return Result<FieldStatistics>.Failure("Field patterns cannot be null");
            }

            _logger.LogDebug("Calculating field statistics for {Standard} {MessageType}",
                patterns.Standard, patterns.MessageType);

            var totalFields = 0;
            var populatedFields = 0;
            var totalSampleSize = 0;

            foreach (var segmentPattern in patterns.SegmentPatterns.Values)
            {
                totalFields += segmentPattern.FieldFrequencies.Count;
                populatedFields += segmentPattern.FieldFrequencies.Values
                    .Count(f => f.Frequency > 0);
                
                if (segmentPattern.SampleSize > totalSampleSize)
                    totalSampleSize = segmentPattern.SampleSize;
            }

            var qualityScore = totalFields > 0 ? (double)populatedFields / totalFields : 0.0;

            var statistics = new FieldStatistics
            {
                TotalFields = totalFields,
                PopulatedFields = populatedFields,
                QualityScore = qualityScore,
                SampleSize = totalSampleSize
            };

            await Task.CompletedTask;
            return Result<FieldStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating field statistics");
            return Result<FieldStatistics>.Failure($"Failed to calculate statistics: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<double>> CalculateFieldCoverageAsync(
        FieldPatterns patterns,
        IEnumerable<string>? expectedFields = null)
    {
        try
        {
            if (patterns == null)
            {
                return Result<double>.Failure("Field patterns cannot be null");
            }

            _logger.LogDebug("Calculating field coverage for {Standard} {MessageType}",
                patterns.Standard, patterns.MessageType);

            if (expectedFields == null || !expectedFields.Any())
            {
                // If no expected fields specified, calculate coverage based on populated vs total
                var totalFields = patterns.SegmentPatterns.Values
                    .Sum(s => s.FieldFrequencies.Count);
                var populatedFields = patterns.SegmentPatterns.Values
                    .SelectMany(s => s.FieldFrequencies.Values)
                    .Count(f => f.Frequency > 0.5); // Consider >50% population as "covered"

                var coverage = totalFields > 0 ? (double)populatedFields / totalFields : 0.0;
                
                await Task.CompletedTask;
                return Result<double>.Success(coverage);
            }
            else
            {
                // Calculate coverage based on expected fields
                var expectedSet = new HashSet<string>(expectedFields);
                var actualFields = new HashSet<string>();
                
                foreach (var segmentPattern in patterns.SegmentPatterns.Values)
                {
                    foreach (var fieldFreq in segmentPattern.FieldFrequencies)
                    {
                        if (fieldFreq.Value.Frequency > 0.5)
                        {
                            actualFields.Add($"{segmentPattern.SegmentId}.{fieldFreq.Key}");
                        }
                    }
                }

                var matchedFields = expectedSet.Intersect(actualFields).Count();
                var coverage = expectedSet.Count > 0 ? (double)matchedFields / expectedSet.Count : 0.0;

                await Task.CompletedTask;
                return Result<double>.Success(coverage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating field coverage");
            return Result<double>.Failure($"Failed to calculate coverage: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<double>> CalculateDataQualityScoreAsync(FieldPatterns patterns)
    {
        try
        {
            if (patterns == null)
            {
                return Result<double>.Failure("Field patterns cannot be null");
            }

            _logger.LogDebug("Calculating data quality score for {Standard} {MessageType}",
                patterns.Standard, patterns.MessageType);

            var scores = new List<double>();

            // Factor 1: Field population consistency
            var populationRates = patterns.SegmentPatterns.Values
                .SelectMany(s => s.FieldFrequencies.Values)
                .Select(f => f.Frequency)
                .Where(rate => rate > 0)
                .ToList();

            if (populationRates.Any())
            {
                var avgPopulation = populationRates.Average();
                scores.Add(avgPopulation);
            }

            // Factor 2: Required field coverage (assume fields with >80% population are required)
            var requiredFieldsCovered = patterns.SegmentPatterns.Values
                .SelectMany(s => s.FieldFrequencies.Values)
                .Count(f => f.Frequency > 0.8);
            var totalRequiredFields = patterns.SegmentPatterns.Values
                .SelectMany(s => s.FieldFrequencies.Values)
                .Count(f => f.Frequency > 0);
            
            if (totalRequiredFields > 0)
            {
                scores.Add((double)requiredFieldsCovered / totalRequiredFields);
            }

            // Factor 3: Pattern confidence
            scores.Add(patterns.Confidence);

            // Calculate weighted average
            var qualityScore = scores.Any() ? scores.Average() : 0.0;

            await Task.CompletedTask;
            return Result<double>.Success(qualityScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating data quality score");
            return Result<double>.Failure($"Failed to calculate quality score: {ex.Message}");
        }
    }
}