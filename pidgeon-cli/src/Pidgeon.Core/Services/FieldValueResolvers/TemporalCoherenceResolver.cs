// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Infrastructure.Standards.HL7.v23;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Resolves timestamp fields with temporal coherence, ensuring realistic time relationships:
/// - EVN.2 (Event Occurred) ≈ PV1.44 (Admit Date/Time) within 5 minutes
/// - DG1.5 (Diagnosis Date/Time) within 0-48 hours after admission
/// - OBX.14 (Observation Date/Time) within 0-24 hours after admission
/// - MSH.7 (Message Date/Time) same as event or up to 1 hour after
///
/// Priority: 92 (higher than HL7SpecificFieldResolver at 90 to intercept timestamps)
/// Implements both IFieldValueResolver and ICompositeAwareResolver because TS is defined as composite.
/// </summary>
public class TemporalCoherenceResolver : IFieldValueResolver, ICompositeAwareResolver
{
    private readonly ILogger<TemporalCoherenceResolver> _logger;
    private readonly Random _random;

    public int Priority => 92;

    /// <summary>
    /// Handles TS (Timestamp) composite type for temporal coherence.
    /// </summary>
    public bool CanHandleComposite(string dataTypeCode) => dataTypeCode == "TS" || dataTypeCode == "DTM";

    /// <summary>
    /// Defines temporal relationships between HL7 fields.
    /// Maps target field → (anchor field, min delta, max delta).
    /// </summary>
    private static readonly Dictionary<string, (string AnchorField, TimeSpan MinDelta, TimeSpan MaxDelta)> TemporalRelationships = new()
    {
        // PV1.44 (Admit Date/Time) should match EVN.2 (Event Occurred) within a few minutes
        ["PV1.44"] = ("EVN.2", TimeSpan.Zero, TimeSpan.FromMinutes(5)),

        // DG1.5 (Diagnosis Date/Time) should be within 0-48 hours after admission
        ["DG1.5"] = ("EVN.2", TimeSpan.Zero, TimeSpan.FromHours(48)),

        // OBX.14 (Observation Date/Time) should be within 0-24 hours after admission
        ["OBX.14"] = ("EVN.2", TimeSpan.Zero, TimeSpan.FromHours(24)),

        // MSH.7 (Message Date/Time) is typically "now" - same as event or up to 1 hour after
        ["MSH.7"] = ("EVN.2", TimeSpan.Zero, TimeSpan.FromHours(1)),

        // ORC.9 (Order Date/Time) should be within encounter timespan
        ["ORC.9"] = ("EVN.2", TimeSpan.Zero, TimeSpan.FromHours(24)),

        // RXE.32 (Original Order Date/Time) should match or precede current order
        ["RXE.32"] = ("ORC.9", TimeSpan.FromHours(-24), TimeSpan.Zero),
    };

    public TemporalCoherenceResolver(ILogger<TemporalCoherenceResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();
    }

    /// <summary>
    /// Handles TS/DTM composite types (called for composite data types like TS).
    /// Returns timestamp as a single-component dictionary.
    /// </summary>
    public Task<Dictionary<int, string>?> ResolveCompositeAsync(
        SegmentField parentField,
        DataType dataType,
        FieldResolutionContext context)
    {
        // Build field path (e.g., "EVN.2", "PV1.44")
        var fieldPath = $"{context.SegmentCode}.{context.FieldPosition}";

        // Generate the timestamp
        var timestamp = GenerateTimestampForField(fieldPath, context);

        if (timestamp == null)
            return Task.FromResult<Dictionary<int, string>?>(null);

        // Return as single-component dictionary (TS.1 = the timestamp value)
        var result = new Dictionary<int, string>
        {
            { 1, timestamp }
        };

        return Task.FromResult<Dictionary<int, string>?>(result);
    }

    /// <summary>
    /// Handles TS/DTM simple fields (fallback for non-composite TS fields).
    /// </summary>
    public Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        // Only handle timestamp fields (TS or DTM data types)
        if (context.Field.DataType != "TS" && context.Field.DataType != "DTM")
            return Task.FromResult<string?>(null);

        // Build field path (e.g., "EVN.2", "PV1.44")
        var fieldPath = $"{context.SegmentCode}.{context.FieldPosition}";

        var timestamp = GenerateTimestampForField(fieldPath, context);
        return Task.FromResult<string?>(timestamp);
    }

    /// <summary>
    /// Core timestamp generation logic used by both composite and simple field paths.
    /// </summary>
    private string? GenerateTimestampForField(string fieldPath, FieldResolutionContext context)
    {
        // Check if this field has a temporal relationship
        if (!TemporalRelationships.TryGetValue(fieldPath, out var relationship))
        {
            // No temporal relationship - check if this might be an anchor field (EVN.2)
            if (fieldPath == "EVN.2")
            {
                return GenerateAndRecordAnchorTimestamp(fieldPath, context);
            }

            // Not a known temporal field - let it fall through to HL7SpecificFieldResolver
            return null;
        }

        // Try to get the anchor timestamp
        var anchorTime = GetAnchorTimestamp(relationship.AnchorField, context);

        if (!anchorTime.HasValue)
        {
            _logger.LogDebug("Temporal anchor {AnchorField} not yet generated for {FieldPath}, falling through",
                relationship.AnchorField, fieldPath);

            // Anchor not available yet - fall through to default timestamp generation
            return null;
        }

        // Generate timestamp relative to anchor
        var relativeTime = GenerateRelativeTimestamp(anchorTime.Value, relationship.MinDelta, relationship.MaxDelta);
        var formattedTimestamp = FormatHL7Timestamp(relativeTime, context.Field.DataType);

        // Record this timestamp for future references
        RecordTimestamp(fieldPath, relativeTime, context);

        _logger.LogDebug("Generated temporal timestamp for {FieldPath}: {Timestamp} (relative to {AnchorField})",
            fieldPath, formattedTimestamp, relationship.AnchorField);

        return formattedTimestamp;
    }

    /// <summary>
    /// Generates and records EVN.2 as the primary temporal anchor.
    /// </summary>
    private string GenerateAndRecordAnchorTimestamp(string fieldPath, FieldResolutionContext context)
    {
        // Check if anchor already generated in this message
        if (context.GenerationContext.GeneratedTimestamps.TryGetValue(fieldPath, out var existingTime))
        {
            return FormatHL7Timestamp(existingTime, context.Field.DataType);
        }

        // Generate new anchor timestamp (recent past - within last 7 days)
        var daysAgo = _random.Next(0, 7);
        var hoursAgo = _random.Next(0, 24);
        var minutesAgo = _random.Next(0, 60);

        var anchorTime = DateTime.Now
            .AddDays(-daysAgo)
            .AddHours(-hoursAgo)
            .AddMinutes(-minutesAgo);

        // Record the anchor timestamp (Dictionary is mutable reference type, so this persists)
        RecordTimestamp(fieldPath, anchorTime, context);

        _logger.LogDebug("Generated encounter anchor timestamp {FieldPath}: {Timestamp}",
            fieldPath, FormatHL7Timestamp(anchorTime, context.Field.DataType));

        return FormatHL7Timestamp(anchorTime, context.Field.DataType);
    }

    /// <summary>
    /// Gets the anchor timestamp for a field from the generation context.
    /// </summary>
    private DateTime? GetAnchorTimestamp(string anchorFieldPath, FieldResolutionContext context)
    {
        // Check if the anchor timestamp has been generated yet
        if (context.GenerationContext.GeneratedTimestamps.TryGetValue(anchorFieldPath, out var timestamp))
            return timestamp;

        return null;
    }

    /// <summary>
    /// Generates a timestamp relative to an anchor with realistic variation.
    /// </summary>
    private DateTime GenerateRelativeTimestamp(DateTime anchor, TimeSpan minDelta, TimeSpan maxDelta)
    {
        // Calculate random delta within the specified range
        var deltaRange = maxDelta - minDelta;
        var randomDelta = TimeSpan.FromSeconds(_random.NextDouble() * deltaRange.TotalSeconds);
        var actualDelta = minDelta + randomDelta;

        return anchor.Add(actualDelta);
    }

    /// <summary>
    /// Records a timestamp in the context for future field references.
    /// </summary>
    private void RecordTimestamp(string fieldPath, DateTime timestamp, FieldResolutionContext context)
    {
        context.GenerationContext.GeneratedTimestamps[fieldPath] = timestamp;
    }

    /// <summary>
    /// Formats a DateTime as an HL7 timestamp (TS or DTM).
    /// </summary>
    private string FormatHL7Timestamp(DateTime timestamp, string dataType)
    {
        // HL7 v2.3 timestamp format: YYYYMMDDHHMMSS
        // DTM is also in the same format
        return timestamp.ToString("yyyyMMddHHmmss");
    }
}
