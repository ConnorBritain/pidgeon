// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Intelligence;

/// <summary>
/// Result from AI analysis with performance metrics and structured output.
/// </summary>
public record AnalysisResult
{
    /// <summary>
    /// The generated response text from the AI model
    /// </summary>
    public string Response { get; init; } = string.Empty;
    
    /// <summary>
    /// Confidence score of the analysis (0.0-1.0)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Time taken for the analysis
    /// </summary>
    public TimeSpan ProcessingTime { get; init; }
    
    /// <summary>
    /// Number of tokens used in the analysis
    /// </summary>
    public int TokensUsed { get; init; }
    
    /// <summary>
    /// Provider that performed the analysis
    /// </summary>
    public string ProviderId { get; init; } = string.Empty;
    
    /// <summary>
    /// Model that was used for the analysis
    /// </summary>
    public string? ModelId { get; init; }
    
    /// <summary>
    /// Structured insights extracted from the response
    /// </summary>
    public List<string> Insights { get; init; } = new();
    
    /// <summary>
    /// Additional metadata about the analysis
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}