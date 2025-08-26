namespace Pidgeon.Core.Configuration.Inference;

using System.Text.Json.Serialization;

public record ComponentPattern
{
    [JsonPropertyName("position")]
    public int Position { get; init; }

    [JsonPropertyName("populationRate")]
    public double PopulationRate { get; init; }

    [JsonPropertyName("averageLength")]
    public double AverageLength { get; init; }

    [JsonPropertyName("commonPattern")]
    public string? CommonPattern { get; init; }
}