namespace Pidgeon.Core.Configuration.Inference;

using System.Text.Json.Serialization;

public record LengthDistribution
{
    [JsonPropertyName("minimum")]
    public int Minimum { get; init; }

    [JsonPropertyName("maximum")]
    public int Maximum { get; init; }

    [JsonPropertyName("average")]
    public double Average { get; init; }

    [JsonPropertyName("median")]
    public double Median { get; init; }

    [JsonPropertyName("standardDeviation")]
    public double StandardDeviation { get; init; }
}