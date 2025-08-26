namespace Pidgeon.Core.Configuration.Inference;

using System.Text.Json.Serialization;

public record InferredConfiguration
{
    [JsonPropertyName("vendor")]
    public VendorSignature Vendor { get; init; } = default!;

    [JsonPropertyName("fieldPatterns")]
    public FieldPatterns FieldPatterns { get; init; } = default!;

    [JsonPropertyName("messagePatterns")]
    public Dictionary<string, MessagePattern> MessagePatterns { get; init; } = new();

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("analysisTimestamp")]
    public DateTime AnalysisTimestamp { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("sampleCount")]
    public int SampleCount { get; init; }

    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";
}

public record MessagePattern
{
    [JsonPropertyName("messageType")]
    public string MessageType { get; init; } = default!;

    [JsonPropertyName("frequency")]
    public int Frequency { get; init; }

    [JsonPropertyName("segmentPatterns")]
    public Dictionary<string, SegmentPattern> SegmentPatterns { get; init; } = new();
}

public record SegmentPattern
{
    [JsonPropertyName("segmentId")]
    public string SegmentId { get; init; } = default!;

    [JsonPropertyName("frequency")]
    public int Frequency { get; init; }

    [JsonPropertyName("fieldUsage")]
    public Dictionary<int, FieldUsage> FieldUsage { get; init; } = new();
}

public record FieldUsage
{
    [JsonPropertyName("position")]
    public int Position { get; init; }

    [JsonPropertyName("populationRate")]
    public double PopulationRate { get; init; }

    [JsonPropertyName("averageLength")]
    public double AverageLength { get; init; }

    [JsonPropertyName("commonPatterns")]
    public List<string> CommonPatterns { get; init; } = new();

    [JsonPropertyName("nullTolerance")]
    public double NullTolerance { get; init; }
}