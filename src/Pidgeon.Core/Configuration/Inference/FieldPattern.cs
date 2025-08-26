namespace Pidgeon.Core.Configuration.Inference;

using System.Text.Json.Serialization;

public record FieldPattern
{
    [JsonPropertyName("fieldNumber")]
    public int FieldNumber { get; init; }

    [JsonPropertyName("populationFrequency")]
    public double PopulationFrequency { get; init; }

    [JsonPropertyName("componentStructure")]
    public ComponentStructure? ComponentStructure { get; init; }

    [JsonPropertyName("nullTolerance")]
    public double NullTolerance { get; init; }

    [JsonPropertyName("statisticalConfidence")]
    public double StatisticalConfidence { get; init; }

    [JsonPropertyName("commonValues")]
    public List<string> CommonValues { get; init; } = new();

    [JsonPropertyName("lengthDistribution")]
    public LengthDistribution LengthDistribution { get; init; } = new();

    [JsonPropertyName("dataTypePattern")]
    public string? DataTypePattern { get; init; }
}