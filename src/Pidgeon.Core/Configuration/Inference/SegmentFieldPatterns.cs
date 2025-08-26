namespace Pidgeon.Core.Configuration.Inference;

using System.Text.Json.Serialization;

public record SegmentFieldPatterns
{
    [JsonPropertyName("segmentId")]
    public string SegmentId { get; init; } = default!;

    [JsonPropertyName("fields")]
    public Dictionary<int, FieldPattern> Fields { get; init; } = new();

    [JsonPropertyName("totalSamples")]
    public int TotalSamples { get; init; }
}