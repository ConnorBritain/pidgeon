// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Interfaces.Configuration;

/// <summary>
/// Service interface for domain-agnostic field statistics and coverage calculations.
/// Handles statistical analysis that applies to any healthcare standard after
/// field patterns have been extracted by plugins and adapters.
/// </summary>
public interface IFieldStatisticsService
{
    /// <summary>
    /// Calculates statistical confidence and quality metrics for field patterns.
    /// Provides confidence scoring for the reliability of pattern analysis results.
    /// Domain-agnostic: works with any FieldPatterns regardless of source standard.
    /// </summary>
    /// <param name="patterns">Field patterns to calculate statistics for</param>
    /// <returns>Result containing statistical analysis of field patterns</returns>
    Task<Result<FieldStatistics>> CalculateFieldStatisticsAsync(FieldPatterns patterns);

    /// <summary>
    /// Calculates field coverage score based on expected field populations.
    /// Uses domain-agnostic algorithms to determine how completely fields are populated
    /// across the analyzed message set.
    /// </summary>
    /// <param name="patterns">Field patterns to evaluate coverage for</param>
    /// <param name="expectedFields">Optional list of fields expected to be present</param>
    /// <returns>Coverage score between 0.0 and 1.0</returns>
    Task<Result<double>> CalculateFieldCoverageAsync(
        FieldPatterns patterns, 
        IEnumerable<string>? expectedFields = null);

    /// <summary>
    /// Calculates data quality score based on field population consistency.
    /// Analyzes patterns for missing data, inconsistent formats, and completeness.
    /// </summary>
    /// <param name="patterns">Field patterns to evaluate quality for</param>
    /// <returns>Quality score between 0.0 and 1.0</returns>
    Task<Result<double>> CalculateDataQualityScoreAsync(FieldPatterns patterns);
}