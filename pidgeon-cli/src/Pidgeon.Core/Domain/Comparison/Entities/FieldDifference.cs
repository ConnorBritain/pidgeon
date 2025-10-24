// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Comparison.Entities;

/// <summary>
/// Represents a difference found between two fields in healthcare messages.
/// </summary>
public record FieldDifference
{
    /// <summary>
    /// Field path in standard notation (e.g., "PID.5", "Patient.name.given").
    /// </summary>
    [JsonPropertyName("fieldPath")]
    public string FieldPath { get; init; } = string.Empty;
    
    /// <summary>
    /// Human-readable description of the field (e.g., "Patient Last Name").
    /// </summary>
    [JsonPropertyName("fieldDescription")]
    public string FieldDescription { get; init; } = string.Empty;
    
    /// <summary>
    /// Value from the left/first message.
    /// </summary>
    [JsonPropertyName("leftValue")]
    public string? LeftValue { get; init; }
    
    /// <summary>
    /// Value from the right/second message.
    /// </summary>
    [JsonPropertyName("rightValue")]
    public string? RightValue { get; init; }
    
    /// <summary>
    /// Type of difference detected.
    /// </summary>
    [JsonPropertyName("differenceType")]
    public DifferenceType DifferenceType { get; init; }
    
    /// <summary>
    /// Severity level of this difference.
    /// </summary>
    [JsonPropertyName("severity")]
    public DifferenceSeverity Severity { get; init; }
    
    /// <summary>
    /// Standard this field belongs to (HL7v2, FHIR, NCPDP).
    /// </summary>
    [JsonPropertyName("standard")]
    public string Standard { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether this field is required by the standard.
    /// </summary>
    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; init; }
    
    /// <summary>
    /// Expected data type for this field.
    /// </summary>
    [JsonPropertyName("expectedDataType")]
    public string ExpectedDataType { get; init; } = string.Empty;
    
    /// <summary>
    /// Analysis context explaining the difference.
    /// </summary>
    [JsonPropertyName("analysisContext")]
    public string AnalysisContext { get; init; } = string.Empty;
    
    /// <summary>
    /// Suggested fix for this difference.
    /// </summary>
    [JsonPropertyName("suggestedFix")]
    public string SuggestedFix { get; init; } = string.Empty;
    
    /// <summary>
    /// Confidence level of the analysis (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; } = 1.0;
    
    /// <summary>
    /// Additional metadata about this field difference.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Types of differences that can be detected between fields.
/// </summary>
public enum DifferenceType
{
    /// <summary>
    /// Field values are different.
    /// </summary>
    ValueDifference,
    
    /// <summary>
    /// Field is missing in left message.
    /// </summary>
    MissingInLeft,
    
    /// <summary>
    /// Field is missing in right message.
    /// </summary>
    MissingInRight,
    
    /// <summary>
    /// Field data type is different.
    /// </summary>
    TypeMismatch,
    
    /// <summary>
    /// Field format is different (e.g., date formats).
    /// </summary>
    FormatDifference,
    
    /// <summary>
    /// Field encoding is different (e.g., escape sequences).
    /// </summary>
    EncodingDifference,
    
    /// <summary>
    /// Field structure is different (e.g., component count).
    /// </summary>
    StructuralDifference,
    
    /// <summary>
    /// Field values are semantically different but syntactically valid.
    /// </summary>
    SemanticDifference
}

/// <summary>
/// Severity levels for field differences.
/// </summary>
public enum DifferenceSeverity
{
    /// <summary>
    /// Informational difference, unlikely to cause issues.
    /// </summary>
    Info,
    
    /// <summary>
    /// Warning difference, might cause issues.
    /// </summary>
    Warning,
    
    /// <summary>
    /// Critical difference, likely to cause failures.
    /// </summary>
    Critical,
    
    /// <summary>
    /// Error difference, will cause failures.
    /// </summary>
    Error
}