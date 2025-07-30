// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Segmint.Core.Configuration.Inference;

/// <summary>
/// Interface for objects that contain pattern data.
/// </summary>
public interface IPatternContainer
{
    HashSet<string> Patterns { get; }
}

/// <summary>
/// Represents the analysis summary of processed HL7 messages.
/// </summary>
public class AnalysisSummary
{
    /// <summary>
    /// Total number of messages analyzed.
    /// </summary>
    public int TotalMessages { get; set; }

    /// <summary>
    /// Number of unique segment patterns identified.
    /// </summary>
    public int SegmentPatterns { get; set; }

    /// <summary>
    /// Number of unique field patterns identified.
    /// </summary>
    public int FieldPatterns { get; set; }

    /// <summary>
    /// Date when analysis was performed.
    /// </summary>
    public DateTime AnalysisDate { get; set; }

    /// <summary>
    /// Vendor signatures detected and their frequencies.
    /// </summary>
    public Dictionary<string, int> VendorSignatures { get; set; } = new();
}

/// <summary>
/// Represents a vendor-specific HL7 configuration inferred from message analysis.
/// </summary>
public class VendorConfiguration
{
    /// <summary>
    /// Name of the vendor or system.
    /// </summary>
    [JsonPropertyName("vendor")]
    public string Vendor { get; set; } = "";

    /// <summary>
    /// System version information.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// HL7 message type this configuration applies to.
    /// </summary>
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = "";

    /// <summary>
    /// Metadata about how this configuration was inferred.
    /// </summary>
    [JsonPropertyName("inferred_from")]
    public InferenceMetadata InferredFrom { get; set; } = new();

    /// <summary>
    /// Segment-level configuration data.
    /// </summary>
    [JsonPropertyName("segments")]
    public Dictionary<string, Dictionary<string, object>> Segments { get; set; } = new();

    /// <summary>
    /// Message-level patterns and conventions.
    /// </summary>
    [JsonPropertyName("patterns")]
    public Dictionary<string, object> Patterns { get; set; } = new();

    /// <summary>
    /// Validation rules specific to this vendor's implementation.
    /// </summary>
    [JsonPropertyName("validation_rules")]
    public List<ValidationRule> ValidationRules { get; set; } = new();

    /// <summary>
    /// Configuration creation timestamp.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Unique identifier for this configuration.
    /// </summary>
    [JsonPropertyName("configuration_id")]
    public string ConfigurationId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Metadata about the inference process used to create a configuration.
/// </summary>
public class InferenceMetadata
{
    /// <summary>
    /// Number of sample messages used for inference.
    /// </summary>
    [JsonPropertyName("sample_count")]
    public int SampleCount { get; set; }

    /// <summary>
    /// Date range of samples used for inference.
    /// </summary>
    [JsonPropertyName("date_range")]
    public string DateRange { get; set; } = "";

    /// <summary>
    /// Statistical confidence level (0.0 - 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    /// <summary>
    /// Analysis algorithm version used.
    /// </summary>
    [JsonPropertyName("algorithm_version")]
    public string AlgorithmVersion { get; set; } = "1.0";

    /// <summary>
    /// Additional metadata about the inference process.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents patterns identified in an HL7 segment.
/// </summary>
public class SegmentPattern
{
    /// <summary>
    /// Three-letter segment identifier.
    /// </summary>
    public string SegmentId { get; set; } = "";

    /// <summary>
    /// Number of times this segment appeared in analyzed messages.
    /// </summary>
    public int Occurrences { get; set; }

    /// <summary>
    /// Statistical confidence in this pattern (0.0 - 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Field patterns within this segment.
    /// </summary>
    public Dictionary<string, FieldPattern> FieldPatterns { get; set; } = new();

    /// <summary>
    /// Segment-level characteristics and metadata.
    /// </summary>
    public Dictionary<string, object> Characteristics { get; set; } = new();
}

/// <summary>
/// Represents patterns identified in an HL7 field.
/// </summary>
public class FieldPattern : IPatternContainer
{
    /// <summary>
    /// Field identifier (e.g., "PID.3", "ORC.12").
    /// </summary>
    public string FieldKey { get; set; } = "";

    /// <summary>
    /// Number of times this field was populated.
    /// </summary>
    public int Occurrences { get; set; }

    /// <summary>
    /// Statistical confidence in this pattern (0.0 - 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Sample values encountered in this field.
    /// </summary>
    public List<string> Values { get; set; } = new();

    /// <summary>
    /// Identified patterns in field values.
    /// </summary>
    public HashSet<string> Patterns { get; set; } = new();

    /// <summary>
    /// Component structure for composite fields.
    /// </summary>
    public Dictionary<int, ComponentPattern> ComponentStructure { get; set; } = new();

    /// <summary>
    /// Additional field characteristics.
    /// </summary>
    public Dictionary<string, object> Characteristics { get; set; } = new();
}

/// <summary>
/// Represents patterns in individual components of composite fields.
/// </summary>
public class ComponentPattern : IPatternContainer
{
    /// <summary>
    /// Position within the composite field (1-based).
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Number of times this component was populated.
    /// </summary>
    public int Occurrences { get; set; }

    /// <summary>
    /// Statistical confidence in this pattern (0.0 - 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Sample values encountered in this component.
    /// </summary>
    public List<string> Values { get; set; } = new();

    /// <summary>
    /// Identified patterns in component values.
    /// </summary>
    public HashSet<string> Patterns { get; set; } = new();

    /// <summary>
    /// Semantic meaning inferred for this component.
    /// </summary>
    public string? SemanticMeaning { get; set; }
}

/// <summary>
/// Represents a vendor-specific validation rule.
/// </summary>
public class ValidationRule
{
    /// <summary>
    /// Field this rule applies to.
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; set; } = "";

    /// <summary>
    /// Validation rule identifier or expression.
    /// </summary>
    [JsonPropertyName("rule")]
    public string Rule { get; set; } = "";

    /// <summary>
    /// Human-readable description of the rule.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Error message to display when validation fails.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    /// <summary>
    /// Statistical confidence in this rule (0.0 - 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    /// <summary>
    /// Severity level of validation failures.
    /// </summary>
    [JsonPropertyName("severity")]
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Warning;

    /// <summary>
    /// Rule category for organization.
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = "inferred";
}

/// <summary>
/// Represents the result of validating a message against a vendor configuration.
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// Overall conformance score (0.0 - 1.0).
    /// </summary>
    public double OverallConformance { get; set; }

    /// <summary>
    /// Whether the message passes validation.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of detected deviations from expected patterns.
    /// </summary>
    public List<PatternDeviation> Deviations { get; set; } = new();

    /// <summary>
    /// Configuration used for validation.
    /// </summary>
    public string ConfigurationId { get; set; } = "";

    /// <summary>
    /// Validation timestamp.
    /// </summary>
    public DateTime ValidationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional validation metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a deviation from expected vendor patterns.
/// </summary>
public class PatternDeviation
{
    /// <summary>
    /// Field where deviation was detected.
    /// </summary>
    public string Field { get; set; } = "";

    /// <summary>
    /// Expected pattern or value.
    /// </summary>
    public string Expected { get; set; } = "";

    /// <summary>
    /// Actual value encountered.
    /// </summary>
    public string Actual { get; set; } = "";

    /// <summary>
    /// Type of deviation detected.
    /// </summary>
    public DeviationType DeviationType { get; set; }

    /// <summary>
    /// Severity of the deviation.
    /// </summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>
    /// Statistical confidence in this deviation (0.0 - 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Human-readable description of the deviation.
    /// </summary>
    public string Description { get; set; } = "";
}

/// <summary>
/// Types of pattern deviations that can be detected.
/// </summary>
public enum DeviationType
{
    /// <summary>
    /// Field is missing when expected to be present.
    /// </summary>
    MissingField,

    /// <summary>
    /// Field format doesn't match expected pattern.
    /// </summary>
    FormatMismatch,

    /// <summary>
    /// Field length is outside expected range.
    /// </summary>
    LengthMismatch,

    /// <summary>
    /// Value is not in expected value set.
    /// </summary>
    ValueMismatch,

    /// <summary>
    /// Composite field structure differs from expected.
    /// </summary>
    StructureMismatch,

    /// <summary>
    /// Unexpected field present.
    /// </summary>
    UnexpectedField,

    /// <summary>
    /// Data type doesn't match expected type.
    /// </summary>
    TypeMismatch
}

/// <summary>
/// Validation severity levels.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning - doesn't prevent processing.
    /// </summary>
    Warning,

    /// <summary>
    /// Error - may prevent processing.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error - prevents processing.
    /// </summary>
    Critical
}

/// <summary>
/// Configuration comparison result.
/// </summary>
public class ConfigurationDiff
{
    /// <summary>
    /// Configuration identifier for baseline.
    /// </summary>
    public string BaselineConfigId { get; set; } = "";

    /// <summary>
    /// Configuration identifier for comparison target.
    /// </summary>
    public string TargetConfigId { get; set; } = "";

    /// <summary>
    /// Overall similarity score (0.0 - 1.0).
    /// </summary>
    public double Similarity { get; set; }

    /// <summary>
    /// Detected differences between configurations.
    /// </summary>
    public List<ConfigurationDifference> Differences { get; set; } = new();

    /// <summary>
    /// Timestamp when comparison was performed.
    /// </summary>
    public DateTime ComparisonTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a difference between two configurations.
/// </summary>
public class ConfigurationDifference
{
    /// <summary>
    /// Type of difference detected.
    /// </summary>
    public DifferenceType Type { get; set; }

    /// <summary>
    /// Field or element path where difference exists.
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// Value in baseline configuration.
    /// </summary>
    public object? BaselineValue { get; set; }

    /// <summary>
    /// Value in target configuration.
    /// </summary>
    public object? TargetValue { get; set; }

    /// <summary>
    /// Impact level of this difference.
    /// </summary>
    public ImpactLevel Impact { get; set; }

    /// <summary>
    /// Human-readable description of the difference.
    /// </summary>
    public string Description { get; set; } = "";
}

/// <summary>
/// Types of configuration differences.
/// </summary>
public enum DifferenceType
{
    /// <summary>
    /// Field added in target configuration.
    /// </summary>
    Added,

    /// <summary>
    /// Field removed in target configuration.
    /// </summary>
    Removed,

    /// <summary>
    /// Field value changed between configurations.
    /// </summary>
    Modified,

    /// <summary>
    /// Field moved to different position or structure.
    /// </summary>
    Moved
}

/// <summary>
/// Impact levels for configuration differences.
/// </summary>
public enum ImpactLevel
{
    /// <summary>
    /// Low impact - cosmetic or minor changes.
    /// </summary>
    Low,

    /// <summary>
    /// Medium impact - may affect processing.
    /// </summary>
    Medium,

    /// <summary>
    /// High impact - likely to affect processing.
    /// </summary>
    High,

    /// <summary>
    /// Critical impact - breaks compatibility.
    /// </summary>
    Critical
}