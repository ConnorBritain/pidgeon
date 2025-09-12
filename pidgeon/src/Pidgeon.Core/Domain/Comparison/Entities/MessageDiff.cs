// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Comparison.Entities;

/// <summary>
/// Represents the result of comparing two healthcare messages.
/// Contains field-level differences and analysis insights.
/// </summary>
public record MessageDiff
{
    /// <summary>
    /// Unique identifier for this diff analysis.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Context information about what was compared.
    /// </summary>
    [JsonPropertyName("context")]
    public DiffContext Context { get; init; } = new();
    
    /// <summary>
    /// Individual field-level differences found.
    /// </summary>
    [JsonPropertyName("fieldDifferences")]
    public List<FieldDifference> FieldDifferences { get; init; } = new();
    
    /// <summary>
    /// Overall similarity score (0.0 = completely different, 1.0 = identical).
    /// </summary>
    [JsonPropertyName("similarityScore")]
    public double SimilarityScore { get; init; }
    
    /// <summary>
    /// Summary of difference types and counts.
    /// </summary>
    [JsonPropertyName("summary")]
    public DiffSummary Summary { get; init; } = new();
    
    /// <summary>
    /// Analysis insights from algorithmic or AI analysis.
    /// </summary>
    [JsonPropertyName("insights")]
    public List<AnalysisInsight> Insights { get; init; } = new();
    
    /// <summary>
    /// Timestamp when the diff analysis was performed.
    /// </summary>
    [JsonPropertyName("analysisTime")]
    public DateTime AnalysisTime { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional metadata for the diff analysis.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Summary statistics about the differences found.
/// </summary>
public record DiffSummary
{
    /// <summary>
    /// Total number of field differences.
    /// </summary>
    [JsonPropertyName("totalDifferences")]
    public int TotalDifferences { get; init; }
    
    /// <summary>
    /// Number of critical differences (likely to cause failures).
    /// </summary>
    [JsonPropertyName("criticalDifferences")]
    public int CriticalDifferences { get; init; }
    
    /// <summary>
    /// Number of warning differences (might cause issues).
    /// </summary>
    [JsonPropertyName("warningDifferences")]
    public int WarningDifferences { get; init; }
    
    /// <summary>
    /// Number of informational differences (formatting, etc.).
    /// </summary>
    [JsonPropertyName("informationalDifferences")]
    public int InformationalDifferences { get; init; }
    
    /// <summary>
    /// Fields present in left but missing in right.
    /// </summary>
    [JsonPropertyName("fieldsOnlyInLeft")]
    public int FieldsOnlyInLeft { get; init; }
    
    /// <summary>
    /// Fields present in right but missing in left.
    /// </summary>
    [JsonPropertyName("fieldsOnlyInRight")]
    public int FieldsOnlyInRight { get; init; }
    
    /// <summary>
    /// Fields with different values.
    /// </summary>
    [JsonPropertyName("fieldsWithDifferentValues")]
    public int FieldsWithDifferentValues { get; init; }
}