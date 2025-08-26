namespace Pidgeon.Core.Configuration.Inference;

using System.Text.Json.Serialization;

public record FieldPatterns
{
    [JsonPropertyName("segmentPatterns")]
    public Dictionary<string, SegmentFieldPatterns> SegmentPatterns { get; init; } = new();

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("analysisDate")]
    public DateTime AnalysisDate { get; init; } = DateTime.UtcNow;
}