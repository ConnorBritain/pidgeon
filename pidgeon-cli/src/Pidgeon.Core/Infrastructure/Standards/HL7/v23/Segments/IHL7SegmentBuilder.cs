// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;

/// <summary>
/// Interface for building individual HL7 v2.3 segments with strict standards compliance.
/// Each segment type gets its own focused builder implementation.
/// </summary>
/// <typeparam name="TInput">The input data type required to build this segment</typeparam>
public interface IHL7SegmentBuilder<in TInput>
{
    /// <summary>
    /// Builds a standards-compliant HL7 v2.3 segment.
    /// </summary>
    /// <param name="input">The data required to build this segment</param>
    /// <param name="setId">The segment set ID (for segments that support multiple instances)</param>
    /// <param name="options">Generation options for deterministic behavior</param>
    /// <returns>A properly formatted HL7 v2.3 segment string</returns>
    string Build(TInput input, int setId, GenerationOptions options);

    /// <summary>
    /// Validates an HL7 segment against v2.3 standards.
    /// </summary>
    /// <param name="segment">The segment string to validate</param>
    /// <returns>Validation result with compliance details</returns>
    SegmentValidationResult Validate(string segment);

    /// <summary>
    /// Gets metadata about this segment type for documentation and tooling.
    /// </summary>
    /// <returns>Segment metadata including required fields and validation rules</returns>
    SegmentMetadata GetMetadata();
}

/// <summary>
/// Result of segment validation with detailed compliance information.
/// </summary>
public record SegmentValidationResult
{
    public bool IsValid { get; init; }
    public string SegmentType { get; init; } = string.Empty;
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public Dictionary<int, string> FieldValidation { get; init; } = new();
}

/// <summary>
/// Metadata describing an HL7 segment type for tooling and documentation.
/// </summary>
public record SegmentMetadata
{
    public string SegmentType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<FieldMetadata> RequiredFields { get; init; } = new();
    public List<FieldMetadata> OptionalFields { get; init; } = new();
    public int MinimumFields { get; init; }
    public string HL7Version { get; init; } = "2.3";
}

/// <summary>
/// Metadata for individual HL7 segment fields.
/// </summary>
public record FieldMetadata
{
    public int FieldNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public int? MaxLength { get; init; }
    public string? ValidationPattern { get; init; }
    public string Description { get; init; } = string.Empty;
}