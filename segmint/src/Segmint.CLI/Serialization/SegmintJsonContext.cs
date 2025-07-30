// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Segmint.CLI.Services;
using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.HL7.Validation;
using Segmint.Core.HL7;
using Segmint.Core.Configuration;
using System.Collections.Generic;

namespace Segmint.CLI.Serialization;

/// <summary>
/// JsonSerializerContext for Segmint CLI to support trimmed builds and AOT compilation.
/// This provides metadata for System.Text.Json to properly serialize types without reflection.
/// </summary>
[JsonSerializable(typeof(ValidationSummary))]
[JsonSerializable(typeof(MessageValidationResult))]
[JsonSerializable(typeof(Segmint.Core.HL7.Validation.ValidationResult), TypeInfoPropertyName = "HL7ValidationResult")]
[JsonSerializable(typeof(ValidationIssue))]
[JsonSerializable(typeof(ConfigurationComparisonResult))]
[JsonSerializable(typeof(ConfigurationDifference))]
[JsonSerializable(typeof(ConfigurationTemplate))]
[JsonSerializable(typeof(SegmintConfiguration))]
[JsonSerializable(typeof(HL7Message))]
[JsonSerializable(typeof(RDEMessage))]
[JsonSerializable(typeof(ADTMessage))]
[JsonSerializable(typeof(ACKMessage))]
[JsonSerializable(typeof(HL7Segment))]
[JsonSerializable(typeof(List<ValidationSummary>))]
[JsonSerializable(typeof(List<MessageValidationResult>))]
[JsonSerializable(typeof(List<ValidationIssue>))]
[JsonSerializable(typeof(List<ConfigurationDifference>))]
[JsonSerializable(typeof(List<ConfigurationTemplate>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(MessageMetadata))]
[JsonSerializable(typeof(GenerationSummary))]
[JsonSerializable(typeof(ScenarioMetadataDto))]
[JsonSerializable(typeof(SuiteSummaryDto))]
[JsonSerializable(typeof(ScenarioBreakdownDto))]
[JsonSerializable(typeof(List<ScenarioBreakdownDto>))]
// Anonymous types for message data serialization
[JsonSerializable(typeof(MessageDataDto))]
[JsonSerializable(typeof(SegmentDataDto))]
[JsonSerializable(typeof(List<MessageDataDto>))]
[JsonSerializable(typeof(List<SegmentDataDto>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
public partial class SegmintJsonContext : JsonSerializerContext
{
}

/// <summary>
/// DTO for message data serialization to avoid anonymous types in JsonSerializerContext.
/// </summary>
public class MessageDataDto
{
    public string MessageType { get; set; } = string.Empty;
    public List<SegmentDataDto> Segments { get; set; } = new();
    public string HL7String { get; set; } = string.Empty;
}

/// <summary>
/// DTO for segment data serialization to avoid anonymous types in JsonSerializerContext.
/// </summary>
public class SegmentDataDto
{
    public string SegmentType { get; set; } = string.Empty;
    public string HL7Content { get; set; } = string.Empty;
}

/// <summary>
/// DTO for message metadata serialization.
/// </summary>
public class MessageMetadata
{
    public string MessageType { get; set; } = string.Empty;
    public int SegmentCount { get; set; }
    public int FieldCount { get; set; }
    public long SizeBytes { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// DTO for generation summary serialization.
/// </summary>
public class GenerationSummary
{
    public int TotalMessages { get; set; }
    public int SuccessfulMessages { get; set; }
    public int FailedMessages { get; set; }
    public double ElapsedTimeMs { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// DTO for scenario metadata serialization.
/// </summary>
public class ScenarioMetadataDto
{
    public string Scenario { get; set; } = string.Empty;
    public string Patient { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// DTO for suite summary serialization.
/// </summary>
public class SuiteSummaryDto
{
    public DateTime GeneratedAt { get; set; }
    public int TotalScenarios { get; set; }
    public List<ScenarioBreakdownDto> ScenarioBreakdown { get; set; } = new();
}

/// <summary>
/// DTO for scenario breakdown in suite summary.
/// </summary>
public class ScenarioBreakdownDto
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Description { get; set; } = string.Empty;
}