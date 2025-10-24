// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Service for analyzing null value tolerance patterns in healthcare messages.
/// Calculates the frequency of missing or null values across message fields.
/// </summary>
public interface INullValueToleranceAnalysisService
{
    /// <summary>
    /// Calculates null value tolerance from field frequency data.
    /// </summary>
    /// <param name="fieldFrequencies">Field frequency analysis results</param>
    /// <returns>Dictionary mapping field paths to null tolerance rates (0.0-1.0)</returns>
    Result<Dictionary<string, double>> CalculateNullTolerance(Dictionary<string, FieldFrequency> fieldFrequencies);
}

/// <summary>
/// Implementation of null value tolerance analysis service.
/// Focuses solely on statistical calculation of missing value patterns.
/// </summary>
internal class NullValueToleranceAnalysisService : INullValueToleranceAnalysisService
{
    private readonly ILogger<NullValueToleranceAnalysisService> _logger;

    public NullValueToleranceAnalysisService(ILogger<NullValueToleranceAnalysisService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Result<Dictionary<string, double>> CalculateNullTolerance(Dictionary<string, FieldFrequency> fieldFrequencies)
    {
        try
        {
            _logger.LogDebug("Calculating null value tolerance for {FieldCount} fields", fieldFrequencies.Count);

            var nullTolerance = new Dictionary<string, double>();
            
            foreach (var fieldFreq in fieldFrequencies)
            {
                var totalOccurrences = fieldFreq.Value.TotalCount;
                var populatedOccurrences = fieldFreq.Value.PopulatedCount;
                var nullOccurrences = totalOccurrences - populatedOccurrences;
                
                var tolerance = totalOccurrences > 0 ? (double)nullOccurrences / totalOccurrences : 0.0;
                nullTolerance[fieldFreq.Key] = tolerance;
            }

            _logger.LogInformation("Null value tolerance calculated for {FieldCount} fields", nullTolerance.Count);
            return Result<Dictionary<string, double>>.Success(nullTolerance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating null value tolerance");
            return Result<Dictionary<string, double>>.Failure($"Null tolerance calculation failed: {ex.Message}");
        }
    }
}