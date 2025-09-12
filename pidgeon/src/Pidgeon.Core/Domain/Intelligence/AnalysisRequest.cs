// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Intelligence;

/// <summary>
/// Request for AI analysis with healthcare-specific context and parameters.
/// </summary>
public record AnalysisRequest
{
    /// <summary>
    /// The prompt or query for AI analysis
    /// </summary>
    public string Prompt { get; init; } = string.Empty;
    
    /// <summary>
    /// Healthcare context for the analysis (e.g., "hl7_diff_analysis", "message_generation")
    /// </summary>
    public string Context { get; init; } = string.Empty;
    
    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    public int MaxTokens { get; init; } = 500;
    
    /// <summary>
    /// Temperature for randomness (0.0-1.0, lower = more deterministic)
    /// </summary>
    public double Temperature { get; init; } = 0.3;
    
    /// <summary>
    /// Healthcare standard being analyzed (HL7, FHIR, NCPDP)
    /// </summary>
    public string? Standard { get; init; }
    
    /// <summary>
    /// Message type being analyzed (ADT^A01, Patient, NewRx)
    /// </summary>
    public string? MessageType { get; init; }
    
    /// <summary>
    /// Additional metadata for the analysis
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}