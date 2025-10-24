// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Comparison.Entities;

/// <summary>
/// Context information about a message diff operation.
/// Contains metadata about the sources being compared.
/// </summary>
public record DiffContext
{
    /// <summary>
    /// Identifier or path of the left/first message source.
    /// </summary>
    [JsonPropertyName("leftSource")]
    public MessageSource LeftSource { get; init; } = new();
    
    /// <summary>
    /// Identifier or path of the right/second message source.
    /// </summary>
    [JsonPropertyName("rightSource")]
    public MessageSource RightSource { get; init; } = new();
    
    /// <summary>
    /// Type of comparison being performed.
    /// </summary>
    [JsonPropertyName("comparisonType")]
    public ComparisonType ComparisonType { get; init; }
    
    /// <summary>
    /// Healthcare standards being compared (HL7v2, FHIR, NCPDP).
    /// </summary>
    [JsonPropertyName("standards")]
    public List<string> Standards { get; init; } = new();
    
    /// <summary>
    /// Fields to ignore during comparison.
    /// </summary>
    [JsonPropertyName("ignoredFields")]
    public List<string> IgnoredFields { get; init; } = new();
    
    /// <summary>
    /// Vendor configuration used for context-aware analysis.
    /// </summary>
    [JsonPropertyName("vendorConfiguration")]
    public string? VendorConfiguration { get; init; }
    
    /// <summary>
    /// User who initiated the comparison.
    /// </summary>
    [JsonPropertyName("initiatedBy")]
    public string InitiatedBy { get; init; } = string.Empty;
    
    /// <summary>
    /// Environment context (dev, staging, production).
    /// </summary>
    [JsonPropertyName("environment")]
    public string Environment { get; init; } = string.Empty;
    
    /// <summary>
    /// Purpose or description of this comparison.
    /// </summary>
    [JsonPropertyName("purpose")]
    public string Purpose { get; init; } = string.Empty;
    
    /// <summary>
    /// Analysis options that were applied.
    /// </summary>
    [JsonPropertyName("analysisOptions")]
    public ComparisonOptions AnalysisOptions { get; init; } = new();
    
    /// <summary>
    /// Whether AI-enhanced analysis is enabled for this comparison.
    /// </summary>
    [JsonPropertyName("enableAIAnalysis")]
    public bool EnableAIAnalysis { get; init; } = false;
    
    /// <summary>
    /// Healthcare standards being compared (derived from first standard for compatibility).
    /// </summary>
    [JsonIgnore]
    public string? Standard => Standards.FirstOrDefault();
    
    /// <summary>
    /// Additional context metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Information about a message source being compared.
/// </summary>
public record MessageSource
{
    /// <summary>
    /// Source identifier (file path, URL, database key, etc.).
    /// </summary>
    [JsonPropertyName("identifier")]
    public string Identifier { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of source (file, directory, database, API, etc.).
    /// </summary>
    [JsonPropertyName("sourceType")]
    public string SourceType { get; init; } = string.Empty;
    
    /// <summary>
    /// Healthcare standard format of the source.
    /// </summary>
    [JsonPropertyName("standard")]
    public string Standard { get; init; } = string.Empty;
    
    /// <summary>
    /// Version of the standard.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;
    
    /// <summary>
    /// Size of the source (file size, message count, etc.).
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; init; }
    
    /// <summary>
    /// Timestamp when source was last modified.
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTime? LastModified { get; init; }
    
    /// <summary>
    /// Content hash for integrity verification.
    /// </summary>
    [JsonPropertyName("contentHash")]
    public string ContentHash { get; init; } = string.Empty;
    
    /// <summary>
    /// Message type (ADT^A01, Patient, NewRx, etc.).
    /// </summary>
    [JsonPropertyName("messageType")]
    public string MessageType { get; init; } = string.Empty;
    
    /// <summary>
    /// Vendor configuration used for this message source.
    /// </summary>
    [JsonPropertyName("vendorConfiguration")]
    public object? VendorConfiguration { get; init; }
    
    /// <summary>
    /// Additional source metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Types of message comparisons supported.
/// </summary>
public enum ComparisonType
{
    /// <summary>
    /// Compare two individual messages.
    /// </summary>
    MessageToMessage,
    
    /// <summary>
    /// Compare message to a template or standard.
    /// </summary>
    MessageToTemplate,
    
    /// <summary>
    /// Compare two directories of messages.
    /// </summary>
    DirectoryToDirectory,
    
    /// <summary>
    /// Compare workflow outputs.
    /// </summary>
    WorkflowComparison,
    
    /// <summary>
    /// Compare environment configurations.
    /// </summary>
    EnvironmentComparison,
    
    /// <summary>
    /// Compare vendor-specific implementations.
    /// </summary>
    VendorComparison
}

/// <summary>
/// Options for controlling comparison analysis.
/// </summary>
public record ComparisonOptions
{
    /// <summary>
    /// Whether to include structural analysis.
    /// </summary>
    [JsonPropertyName("includeStructuralAnalysis")]
    public bool IncludeStructuralAnalysis { get; init; } = true;
    
    /// <summary>
    /// Whether to include semantic analysis.
    /// </summary>
    [JsonPropertyName("includeSemanticAnalysis")]
    public bool IncludeSemanticAnalysis { get; init; } = true;
    
    /// <summary>
    /// Whether to use AI/ML analysis (local or cloud).
    /// </summary>
    [JsonPropertyName("useIntelligentAnalysis")]
    public bool UseIntelligentAnalysis { get; init; } = true;
    
    /// <summary>
    /// Whether to normalize whitespace and formatting.
    /// </summary>
    [JsonPropertyName("normalizeFormatting")]
    public bool NormalizeFormatting { get; init; } = true;
    
    /// <summary>
    /// Whether to ignore case differences in text fields.
    /// </summary>
    [JsonPropertyName("ignoreCase")]
    public bool IgnoreCase { get; init; } = false;
    
    /// <summary>
    /// Whether to ignore timestamp differences.
    /// </summary>
    [JsonPropertyName("ignoreTimestamps")]
    public bool IgnoreTimestamps { get; init; } = false;
    
    /// <summary>
    /// Minimum confidence threshold for analysis insights.
    /// </summary>
    [JsonPropertyName("confidenceThreshold")]
    public double ConfidenceThreshold { get; init; } = 0.7;
}