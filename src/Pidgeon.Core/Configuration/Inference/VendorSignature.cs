namespace Pidgeon.Core.Configuration.Inference;

using System.Text.Json.Serialization;

public record VendorSignature
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = default!;

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("sendingApplication")]
    public string SendingApplication { get; init; } = default!;

    [JsonPropertyName("sendingFacility")]
    public string? SendingFacility { get; init; }

    [JsonPropertyName("encodingCharacters")]
    public string EncodingCharacters { get; init; } = @"^~\&";

    [JsonPropertyName("fieldSeparator")]
    public char FieldSeparator { get; init; } = '|';

    [JsonPropertyName("quirks")]
    public List<VendorQuirk> Quirks { get; init; } = new();

    [JsonPropertyName("detectedTimestamp")]
    public DateTime DetectedTimestamp { get; init; } = DateTime.UtcNow;
}

public record VendorQuirk
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = default!;

    [JsonPropertyName("description")]
    public string Description { get; init; } = default!;

    [JsonPropertyName("location")]
    public string Location { get; init; } = default!;

    [JsonPropertyName("frequency")]
    public double Frequency { get; init; }

    [JsonPropertyName("severity")]
    public QuirkSeverity Severity { get; init; } = QuirkSeverity.Info;
}

public enum QuirkSeverity
{
    Info,
    Warning,
    Error,
    Critical
}