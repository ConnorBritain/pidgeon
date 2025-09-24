// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Comparison.Entities;
using Pidgeon.Core.Common.Types;

namespace Pidgeon.Core.Application.Interfaces.Comparison;

/// <summary>
/// Advanced procedural analysis engine for healthcare message differences.
/// Provides constraint-based validation, demographic analysis, and clinical impact assessment.
/// </summary>
public interface IProceduralAnalysisEngine
{
    /// <summary>
    /// Performs comprehensive procedural analysis on field differences.
    /// Uses constraint resolution and demographic validation for enhanced insights.
    /// </summary>
    /// <param name="fieldDifferences">Raw field differences from basic comparison</param>
    /// <param name="context">Diff context including standards and vendor information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced analysis with procedural validation results</returns>
    Task<Result<ProceduralAnalysisResult>> AnalyzeAsync(
        List<FieldDifference> fieldDifferences,
        DiffContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates field differences against healthcare constraints and standards.
    /// Identifies constraint violations, required field issues, and data type problems.
    /// </summary>
    /// <param name="fieldDifferences">Field differences to validate</param>
    /// <param name="context">Analysis context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Constraint validation results</returns>
    Task<Result<ConstraintValidationResult>> ValidateConstraintsAsync(
        List<FieldDifference> fieldDifferences,
        DiffContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes demographic field differences for data quality and consistency.
    /// Validates names, dates, addresses, and other demographic data.
    /// </summary>
    /// <param name="fieldDifferences">Field differences containing demographic data</param>
    /// <param name="context">Analysis context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Demographic validation results</returns>
    Task<Result<DemographicAnalysisResult>> AnalyzeDemographicsAsync(
        List<FieldDifference> fieldDifferences,
        DiffContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates clinical impact scores for field differences.
    /// Determines if differences affect patient care, billing, or operational workflows.
    /// </summary>
    /// <param name="fieldDifferences">Field differences to assess</param>
    /// <param name="context">Clinical context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Clinical impact assessment results</returns>
    Task<Result<ClinicalImpactAssessment>> AssessClinicalImpactAsync(
        List<FieldDifference> fieldDifferences,
        DiffContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates procedural recommendations based on analysis results.
    /// Provides actionable steps for resolving differences and preventing future issues.
    /// </summary>
    /// <param name="analysisResult">Complete procedural analysis result</param>
    /// <param name="context">Analysis context</param>
    /// <returns>Procedural recommendations</returns>
    Task<Result<List<ProceduralRecommendation>>> GenerateRecommendationsAsync(
        ProceduralAnalysisResult analysisResult,
        DiffContext context);
}