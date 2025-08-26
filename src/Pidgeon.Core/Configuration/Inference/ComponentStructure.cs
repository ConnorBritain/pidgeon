namespace Pidgeon.Core.Configuration.Inference;

using System.Text.Json.Serialization;

public record ComponentStructure
{
    [JsonPropertyName("hasComponents")]
    public bool HasComponents { get; init; }

    [JsonPropertyName("componentCount")]
    public int ComponentCount { get; init; }

    [JsonPropertyName("componentSeparator")]
    public char ComponentSeparator { get; init; } = '^';

    [JsonPropertyName("componentPatterns")]
    public Dictionary<int, ComponentPattern> ComponentPatterns { get; init; } = new();
}