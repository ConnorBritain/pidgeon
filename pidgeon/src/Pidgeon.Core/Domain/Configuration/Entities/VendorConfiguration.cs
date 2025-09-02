// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Configuration.Entities;

/// <summary>
/// Represents a vendor configuration inferred from analyzing sample messages.
/// Uses hierarchical addressing: VENDOR -> STANDARD -> MESSAGE_TYPE.
/// Supports incremental configuration building and evolution tracking.
/// </summary>
public record VendorConfiguration
{
    /// <summary>
    /// Hierarchical address for this configuration (e.g., "Epic-HL7v23-ADT^A01").
    /// </summary>
    [JsonPropertyName("address")]
    public ConfigurationAddress Address { get; init; } = default!;

    /// <summary>
    /// Detected vendor signature with application and facility information.
    /// </summary>
    [JsonPropertyName("signature")]
    public VendorSignature Signature { get; init; } = default!;

    /// <summary>
    /// Field usage patterns across different segment types.
    /// </summary>
    [JsonPropertyName("fieldPatterns")]
    public FieldPatterns FieldPatterns { get; init; } = default!;

    /// <summary>
    /// Message type patterns and frequencies observed in the sample set.
    /// </summary>
    [JsonPropertyName("messagePatterns")]
    public Dictionary<string, MessagePattern> MessagePatterns { get; init; } = new();

    /// <summary>
    /// Format deviations detected in the messages.
    /// </summary>
    [JsonPropertyName("formatDeviations")]
    public List<FormatDeviation> FormatDeviations { get; init; } = new();

    /// <summary>
    /// Configuration metadata including evolution tracking.
    /// </summary>
    [JsonPropertyName("metadata")]
    public ConfigurationMetadata Metadata { get; init; } = default!;

    /// <summary>
    /// Creates a new vendor configuration with the specified address.
    /// </summary>
    /// <param name="address">Hierarchical configuration address</param>
    /// <param name="signature">Vendor signature</param>
    /// <param name="fieldPatterns">Field usage patterns</param>
    /// <param name="messagePatterns">Message patterns</param>
    /// <param name="confidence">Overall confidence score</param>
    /// <param name="sampleCount">Number of messages analyzed</param>
    /// <returns>New vendor configuration</returns>
    public static VendorConfiguration Create(
        ConfigurationAddress address,
        VendorSignature signature,
        FieldPatterns fieldPatterns,
        Dictionary<string, MessagePattern> messagePatterns,
        double confidence,
        int sampleCount)
    {
        var metadata = new ConfigurationMetadata
        {
            MessagesSampled = sampleCount,
            Confidence = confidence
        };

        return new VendorConfiguration
        {
            Address = address,
            Signature = signature,
            FieldPatterns = fieldPatterns,
            MessagePatterns = messagePatterns,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Merges this configuration with additional analysis data.
    /// Supports incremental configuration building.
    /// </summary>
    /// <param name="other">Additional configuration data</param>
    /// <returns>Merged configuration</returns>
    public Result<VendorConfiguration> MergeWith(VendorConfiguration other)
    {
        if (!Address.Matches(other.Address))
            return Result<VendorConfiguration>.Failure(Error.Validation("Cannot merge configurations with different addresses", "Address"));

        // Merge message patterns
        var mergedPatterns = new Dictionary<string, MessagePattern>(MessagePatterns);
        foreach (var kvp in other.MessagePatterns)
        {
            if (mergedPatterns.ContainsKey(kvp.Key))
            {
                // Combine existing pattern with new data
                var mergeResult = mergedPatterns[kvp.Key].MergeWith(kvp.Value);
                if (mergeResult.IsFailure)
                    return Result<VendorConfiguration>.Failure($"Failed to merge pattern {kvp.Key}: {mergeResult.Error}");
                mergedPatterns[kvp.Key] = mergeResult.Value;
            }
            else
            {
                mergedPatterns[kvp.Key] = kvp.Value;
            }
        }

        // Calculate new confidence as weighted average
        var totalSamples = Metadata.MessagesSampled + other.Metadata.MessagesSampled;
        var newConfidence = totalSamples == 0 ? 0.0 : 
            (Metadata.Confidence * Metadata.MessagesSampled + 
             other.Metadata.Confidence * other.Metadata.MessagesSampled) / totalSamples;

        // Detect changes for metadata
        var changes = new List<ConfigurationChange>();
        if (Math.Abs(Metadata.Confidence - newConfidence) > 0.1)
        {
            changes.Add(new ConfigurationChange
            {
                ChangeType = ConfigurationChangeType.PatternChanged,
                Description = $"Confidence changed from {Metadata.Confidence:F2} to {newConfidence:F2}",
                ImpactScore = Math.Abs(Metadata.Confidence - newConfidence)
            });
        }

        return Result<VendorConfiguration>.Success(this with
        {
            MessagePatterns = mergedPatterns,
            Metadata = Metadata.WithUpdate(other.Metadata.MessagesSampled, newConfidence, changes)
        });
    }

    /// <summary>
    /// Gets a display-friendly summary of this configuration.
    /// </summary>
    public string GetSummary()
    {
        var messageTypes = string.Join(", ", MessagePatterns.Keys);
        return $"{Address} - {Signature.Name} v{Signature.Version} - " +
               $"{Metadata.MessagesSampled} samples - " +
               $"{Metadata.Confidence:P1} confidence - " +
               $"Types: {messageTypes}";
    }
}