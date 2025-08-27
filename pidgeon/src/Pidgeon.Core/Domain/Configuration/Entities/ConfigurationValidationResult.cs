// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Configuration.Entities;

/// <summary>
/// Result of validating a message against a vendor configuration.
/// </summary>
public record ConfigurationValidationResult
{
    /// <summary>
    /// Whether the validation passed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation errors found.
    /// </summary>
    public List<string> Errors { get; init; } = new();

    /// <summary>
    /// Validation warnings.
    /// </summary>
    public List<string> Warnings { get; init; } = new();

    /// <summary>
    /// Confidence score for the validation (0.0 to 1.0).
    /// </summary>
    public double ConfidenceScore { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <param name="confidenceScore">Confidence score</param>
    /// <returns>Successful validation result</returns>
    public static ConfigurationValidationResult Success(double confidenceScore = 1.0) => new()
    {
        IsValid = true,
        ConfidenceScore = confidenceScore
    };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <param name="warnings">Validation warnings</param>
    /// <returns>Failed validation result</returns>
    public static ConfigurationValidationResult Failure(IEnumerable<string> errors, IEnumerable<string>? warnings = null) => new()
    {
        IsValid = false,
        Errors = errors.ToList(),
        Warnings = warnings?.ToList() ?? new List<string>(),
        ConfidenceScore = 0.0
    };
}