// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Configuration.Entities;

/// <summary>
/// Represents frequency analysis for a specific field in healthcare messages.
/// Tracks how often a field is populated and common values found.
/// </summary>
public record FieldFrequency
{
    /// <summary>
    /// Field position index (1-based for HL7).
    /// </summary>
    [JsonPropertyName("fieldIndex")]
    public int FieldIndex { get; set; }

    /// <summary>
    /// Number of times this field was populated (non-empty).
    /// </summary>
    [JsonPropertyName("populatedCount")]
    public int PopulatedCount { get; set; }

    /// <summary>
    /// Total number of times this field was observed.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Population rate (0.0 to 1.0) - PopulatedCount / TotalCount.
    /// </summary>
    [JsonPropertyName("populationRate")]
    public double PopulationRate { get; set; }

    /// <summary>
    /// Most common values found in this field with their frequencies.
    /// Limited to prevent memory issues.
    /// </summary>
    [JsonPropertyName("commonValues")]
    public Dictionary<string, int> CommonValues { get; set; } = new();

    /// <summary>
    /// Average length of field values when populated.
    /// </summary>
    [JsonPropertyName("averageLength")]
    public double AverageLength { get; set; }

    /// <summary>
    /// Total number of occurrences (same as TotalCount for compatibility).
    /// </summary>
    [JsonPropertyName("totalOccurrences")]
    public int TotalOccurrences { get; set; }

    /// <summary>
    /// Field frequency rate (same as PopulationRate for compatibility).
    /// </summary>
    [JsonPropertyName("frequency")]
    public double Frequency { get; set; }

    /// <summary>
    /// Field name for identification.
    /// </summary>
    [JsonPropertyName("fieldName")]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Count of unique values observed.
    /// </summary>
    [JsonPropertyName("uniqueValues")]
    public int UniqueValues { get; set; }

    /// <summary>
    /// Component patterns found in this field (for composite fields).
    /// </summary>
    [JsonPropertyName("componentPatterns")]
    public Dictionary<string, ComponentPattern> ComponentPatterns { get; set; } = new();
}

/// <summary>
/// Represents frequency analysis for a component within a composite field.
/// </summary>
public record ComponentFrequency
{
    /// <summary>
    /// Component position index (0-based).
    /// </summary>
    [JsonPropertyName("componentIndex")]
    public int ComponentIndex { get; set; }

    /// <summary>
    /// Number of times this component was populated.
    /// </summary>
    [JsonPropertyName("populatedCount")]
    public int PopulatedCount { get; set; }

    /// <summary>
    /// Total number of times this component was observed.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Component name for identification.
    /// </summary>
    [JsonPropertyName("componentName")]
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// Component frequency rate.
    /// </summary>
    [JsonPropertyName("frequency")]
    public double Frequency { get; set; }

    /// <summary>
    /// Total occurrences of this component.
    /// </summary>
    [JsonPropertyName("totalOccurrences")]
    public int TotalOccurrences { get; set; }

    /// <summary>
    /// Count of unique values for this component.
    /// </summary>
    [JsonPropertyName("uniqueValues")]
    public int UniqueValues { get; set; }

    /// <summary>
    /// Average length of component values.
    /// </summary>
    [JsonPropertyName("averageLength")]
    public double AverageLength { get; set; }
}

/// <summary>
/// Represents component structure patterns within composite fields.
/// </summary>
public record ComponentPattern
{
    /// <summary>
    /// Type of field being analyzed (e.g., "XPN", "XAD", "CE").
    /// </summary>
    [JsonPropertyName("fieldType")]
    public string FieldType { get; init; } = string.Empty;

    /// <summary>
    /// Component frequency analysis by position.
    /// </summary>
    [JsonPropertyName("componentFrequencies")]
    public Dictionary<int, ComponentFrequency> ComponentFrequencies { get; init; } = new();

    /// <summary>
    /// Number of field values analyzed.
    /// </summary>
    [JsonPropertyName("sampleSize")]
    public int SampleSize { get; init; }

    /// <summary>
    /// Total samples analyzed for this pattern.
    /// </summary>
    [JsonPropertyName("totalSamples")]
    public int TotalSamples { get; init; }

    /// <summary>
    /// Standard name for this pattern.
    /// </summary>
    [JsonPropertyName("standardName")]
    public string StandardName { get; init; } = string.Empty;
}

/// <summary>
/// Represents a segment's field population patterns.
/// </summary>
public record SegmentPattern
{
    /// <summary>
    /// Segment identifier (e.g., "PID", "MSH", "OBR").
    /// </summary>
    [JsonPropertyName("segmentId")]
    public string SegmentId { get; init; } = string.Empty;

    /// <summary>
    /// Field frequency analysis by field position.
    /// </summary>
    [JsonPropertyName("fields")]
    public Dictionary<int, FieldFrequency> Fields { get; init; } = new();

    /// <summary>
    /// Field frequencies by field position.
    /// </summary>
    [JsonPropertyName("fieldFrequencies")]
    public Dictionary<int, FieldFrequency> FieldFrequencies { get; set; } = new();

    /// <summary>
    /// Segment type name.
    /// </summary>
    [JsonPropertyName("segmentType")]
    public string SegmentType { get; init; } = string.Empty;

    /// <summary>
    /// Total occurrences of this segment.
    /// </summary>
    [JsonPropertyName("totalOccurrences")]
    public int TotalOccurrences { get; init; }

    /// <summary>
    /// Confidence score for this pattern (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    /// <summary>
    /// Number of samples used to generate this segment pattern.
    /// </summary>
    [JsonPropertyName("sampleSize")]
    public int SampleSize { get; init; }
}