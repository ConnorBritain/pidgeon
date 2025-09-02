// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Universal analysis result types for Configuration Intelligence plugins.
/// These are STANDARD-AGNOSTIC but comprehensive enough for any healthcare standard.
/// Plugins populate these based on their standard-specific logic, but the result format is universal.
/// </summary>

/// <summary>
/// Complete configuration analysis result from any healthcare standard plugin.
/// Universal format populated by HL7, FHIR, NCPDP, or other standard-specific plugins.
/// </summary>
public record AnalysisResult
{
    /// <summary>
    /// Healthcare standard analyzed (e.g., "HL7v23", "FHIR", "NCPDP").
    /// Populated by the plugin based on the standard it handles.
    /// </summary>
    [JsonPropertyName("standard")]
    public string Standard { get; init; } = string.Empty;

    /// <summary>
    /// Message type analyzed (e.g., "ADT^A01", "Patient", "NewRx").
    /// Standard-agnostic but context-appropriate based on the healthcare standard.
    /// </summary>
    [JsonPropertyName("messageType")]
    public string MessageType { get; init; } = string.Empty;

    /// <summary>
    /// Segment/resource/section analysis by type.
    /// Universal format: HL7 uses segments, FHIR uses resources, NCPDP uses sections.
    /// </summary>
    [JsonPropertyName("segmentAnalysis")]
    public Dictionary<string, SegmentAnalysisResult> SegmentAnalysis { get; init; } = new();

    /// <summary>
    /// Overall message pattern analysis.
    /// </summary>
    [JsonPropertyName("messageAnalysis")]
    public MessageAnalysisResult MessageAnalysis { get; init; } = new();

    /// <summary>
    /// Vendor/system detection results.
    /// </summary>
    [JsonPropertyName("vendorDetection")]
    public VendorAnalysisResult VendorDetection { get; init; } = new();

    /// <summary>
    /// Configuration inference confidence (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    /// <summary>
    /// Number of messages/resources/transactions analyzed.
    /// </summary>
    [JsonPropertyName("sampleSize")]
    public int SampleSize { get; init; }

    /// <summary>
    /// When this analysis was performed.
    /// </summary>
    [JsonPropertyName("analysisDate")]
    public DateTime AnalysisDate { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Analysis results for a specific segment/resource/section type.
/// Universal format: works for HL7 segments, FHIR resources, NCPDP sections.
/// </summary>
public record SegmentAnalysisResult
{
    /// <summary>
    /// Segment/resource/section type (e.g., "MSH", "Patient", "NewRx").
    /// </summary>
    [JsonPropertyName("segmentType")]
    public string SegmentType { get; init; } = string.Empty;

    /// <summary>
    /// Field/element analysis by position/path.
    /// Universal format: HL7 uses positions, FHIR uses paths, NCPDP uses element names.
    /// </summary>
    [JsonPropertyName("fieldAnalysis")]
    public Dictionary<string, FieldAnalysisResult> FieldAnalysis { get; init; } = new();

    /// <summary>
    /// Number of segments/resources/sections analyzed.
    /// </summary>
    [JsonPropertyName("sampleSize")]
    public int SampleSize { get; init; }

    /// <summary>
    /// Analysis confidence for this segment/resource/section.
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }
}

/// <summary>
/// Analysis results for a specific field/element within a segment/resource/section.
/// Universal format: works across all healthcare standards.
/// </summary>
public record FieldAnalysisResult
{
    /// <summary>
    /// Field identifier (position for HL7, path for FHIR, element name for NCPDP).
    /// </summary>
    [JsonPropertyName("fieldIdentifier")]
    public string FieldIdentifier { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable field name (e.g., "Patient Last Name", "Sending Application").
    /// </summary>
    [JsonPropertyName("fieldName")]
    public string FieldName { get; init; } = string.Empty;

    /// <summary>
    /// Population rate (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("populationRate")]
    public double PopulationRate { get; init; }

    /// <summary>
    /// Number of times field was populated.
    /// </summary>
    [JsonPropertyName("populatedCount")]
    public int PopulatedCount { get; init; }

    /// <summary>
    /// Total number of times field was observed.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    /// <summary>
    /// Most common values with their frequencies.
    /// </summary>
    [JsonPropertyName("commonValues")]
    public Dictionary<string, int> CommonValues { get; init; } = new();

    /// <summary>
    /// Count of unique values observed.
    /// </summary>
    [JsonPropertyName("uniqueValues")]
    public int UniqueValues { get; init; }

    /// <summary>
    /// Average length of field values when populated.
    /// </summary>
    [JsonPropertyName("averageLength")]
    public double AverageLength { get; init; }

    /// <summary>
    /// Component analysis for composite fields.
    /// Universal: HL7 has components, FHIR has sub-elements, NCPDP has sub-fields.
    /// </summary>
    [JsonPropertyName("componentAnalysis")]
    public Dictionary<string, ComponentAnalysisResult> ComponentAnalysis { get; init; } = new();
}

/// <summary>
/// Analysis results for components/sub-elements within composite fields.
/// Universal format across healthcare standards.
/// </summary>
public record ComponentAnalysisResult
{
    /// <summary>
    /// Component identifier (position, path, or name depending on standard).
    /// </summary>
    [JsonPropertyName("componentIdentifier")]
    public string ComponentIdentifier { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable component name (e.g., "Family Name", "Street Address").
    /// </summary>
    [JsonPropertyName("componentName")]
    public string ComponentName { get; init; } = string.Empty;

    /// <summary>
    /// Population rate for this component.
    /// </summary>
    [JsonPropertyName("populationRate")]
    public double PopulationRate { get; init; }

    /// <summary>
    /// Number of unique values for this component.
    /// </summary>
    [JsonPropertyName("uniqueValues")]
    public int UniqueValues { get; init; }

    /// <summary>
    /// Average length of component values.
    /// </summary>
    [JsonPropertyName("averageLength")]
    public double AverageLength { get; init; }
}

/// <summary>
/// Analysis results for overall message/bundle/transaction patterns.
/// Universal format for any healthcare standard.
/// </summary>
public record MessageAnalysisResult
{
    /// <summary>
    /// Message/bundle/transaction type.
    /// </summary>
    [JsonPropertyName("messageType")]
    public string MessageType { get; init; } = string.Empty;

    /// <summary>
    /// Expected structure for this message type (segments for HL7, resources for FHIR, sections for NCPDP).
    /// </summary>
    [JsonPropertyName("expectedStructure")]
    public List<string> ExpectedStructure { get; init; } = new();

    /// <summary>
    /// Observed structure patterns.
    /// </summary>
    [JsonPropertyName("observedPatterns")]
    public List<List<string>> ObservedPatterns { get; init; } = new();

    /// <summary>
    /// Coverage analysis (how well observed matches expected).
    /// </summary>
    [JsonPropertyName("coverage")]
    public double Coverage { get; init; }

    /// <summary>
    /// Number of messages/bundles/transactions analyzed.
    /// </summary>
    [JsonPropertyName("sampleSize")]
    public int SampleSize { get; init; }
}

/// <summary>
/// Vendor/system detection and fingerprinting results.
/// Universal format for any healthcare standard.
/// </summary>
public record VendorAnalysisResult
{
    /// <summary>
    /// Detected vendor/system (e.g., "Epic", "Cerner", "Allscripts" for HL7, "Epic FHIR" for FHIR).
    /// </summary>
    [JsonPropertyName("detectedVendor")]
    public string DetectedVendor { get; init; } = string.Empty;

    /// <summary>
    /// Confidence in vendor detection (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    /// <summary>
    /// Vendor signature patterns that led to detection.
    /// </summary>
    [JsonPropertyName("signaturePatterns")]
    public List<VendorSignaturePattern> SignaturePatterns { get; init; } = new();

    /// <summary>
    /// Format deviations from standard specification.
    /// </summary>
    [JsonPropertyName("formatDeviations")]
    public List<FormatDeviationResult> FormatDeviations { get; init; } = new();
}

/// <summary>
/// Specific vendor signature pattern detected.
/// Universal format across healthcare standards.
/// </summary>
public record VendorSignaturePattern
{
    /// <summary>
    /// Pattern type (e.g., "MSH.3" for HL7, "Identifier.system" for FHIR).
    /// </summary>
    [JsonPropertyName("patternType")]
    public string PatternType { get; init; } = string.Empty;

    /// <summary>
    /// Observed value that matches vendor pattern.
    /// </summary>
    [JsonPropertyName("observedValue")]
    public string ObservedValue { get; init; } = string.Empty;

    /// <summary>
    /// Expected pattern for this vendor.
    /// </summary>
    [JsonPropertyName("expectedPattern")]
    public string ExpectedPattern { get; init; } = string.Empty;

    /// <summary>
    /// Match confidence (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("matchConfidence")]
    public double MatchConfidence { get; init; }
}

/// <summary>
/// Format deviation from standard specification.
/// Universal format for any healthcare standard.
/// </summary>
public record FormatDeviationResult
{
    /// <summary>
    /// Type of deviation (e.g., "ExtraField", "MissingField", "InvalidFormat").
    /// </summary>
    [JsonPropertyName("deviationType")]
    public string DeviationType { get; init; } = string.Empty;

    /// <summary>
    /// Location of deviation (standard-specific format).
    /// </summary>
    [JsonPropertyName("location")]
    public string Location { get; init; } = string.Empty;

    /// <summary>
    /// Description of the deviation.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Severity level (Info, Warning, Error).
    /// </summary>
    [JsonPropertyName("severity")]
    public string Severity { get; init; } = "Info";
}

/// <summary>
/// Field statistics analysis results.
/// Universal format for any healthcare standard.
/// </summary>
public record FieldStatistics
{
    /// <summary>
    /// Total number of fields/elements analyzed.
    /// </summary>
    [JsonPropertyName("totalFields")]
    public int TotalFields { get; init; }

    /// <summary>
    /// Number of fields/elements that were populated.
    /// </summary>
    [JsonPropertyName("populatedFields")]
    public int PopulatedFields { get; init; }

    /// <summary>
    /// Data quality score (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("dataQualityScore")]
    public double DataQualityScore { get; init; }

    /// <summary>
    /// Average field length across all fields.
    /// </summary>
    [JsonPropertyName("averageFieldLength")]
    public double AverageFieldLength { get; init; }

    /// <summary>
    /// Most commonly used segments/resources/sections.
    /// </summary>
    [JsonPropertyName("mostCommonStructures")]
    public List<string> MostCommonStructures { get; init; } = new();

    /// <summary>
    /// When this analysis was performed.
    /// </summary>
    [JsonPropertyName("analysisDate")]
    public DateTime AnalysisDate { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Quality score for field patterns (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("qualityScore")]
    public double QualityScore { get; init; }

    /// <summary>
    /// Number of samples analyzed for these statistics.
    /// </summary>
    [JsonPropertyName("sampleSize")]
    public int SampleSize { get; init; }
}