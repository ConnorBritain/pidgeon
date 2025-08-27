// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Messages;

/// <summary>
/// Base class for all HL7 v2.x messages.
/// Captures HL7-specific concepts like encoding characters and segment structure.
/// </summary>
public abstract record HL7Message : HealthcareMessage
{
    /// <summary>
    /// Gets the HL7 encoding characters used for field, component, and escape sequences.
    /// Default: Field=|, Component=^, Repetition=~, Escape=\, Subcomponent=&
    /// </summary>
    public HL7EncodingChars Encoding { get; init; } = HL7EncodingChars.Standard;

    /// <summary>
    /// Gets the HL7 message type and trigger event (e.g., "ORM^O01").
    /// </summary>
    public required HL7MessageType MessageType { get; init; }

    /// <summary>
    /// Gets all segments in this message, keyed by segment ID.
    /// For repeating segments, use segment ID with sequence number (e.g., "OBX1", "OBX2").
    /// </summary>
    public Dictionary<string, HL7Segment> Segments { get; init; } = new();

    /// <summary>
    /// Validates HL7-specific message structure and encoding.
    /// </summary>
    /// <returns>A result indicating whether the HL7 message is valid</returns>
    public override Result<HealthcareMessage> Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.IsSuccess)
            return baseResult;

        if (!Standard.StartsWith("HL7"))
            return Error.Validation($"Standard must be HL7 variant, got: {Standard}", nameof(Standard));

        if (MessageType == null)
            return Error.Validation("HL7 Message Type is required", nameof(MessageType));

        if (!Segments.ContainsKey("MSH"))
            return Error.Validation("HL7 messages must contain MSH (Message Header) segment", nameof(Segments));

        return Result<HealthcareMessage>.Success(this);
    }

    /// <summary>
    /// Gets the HL7 display summary including message type and key segments.
    /// </summary>
    public override string GetDisplaySummary()
    {
        var segmentCount = Segments.Count;
        var segmentSummary = segmentCount == 1 ? "1 segment" : $"{segmentCount} segments";
        return $"HL7 {MessageType} message {MessageControlId} with {segmentSummary} from {SendingSystem} to {ReceivingSystem}";
    }

    /// <summary>
    /// Gets a segment by its ID, with optional sequence number for repeating segments.
    /// </summary>
    /// <typeparam name="T">The expected segment type</typeparam>
    /// <param name="segmentId">The segment ID (e.g., "PID", "OBX")</param>
    /// <param name="sequence">The sequence number for repeating segments (optional)</param>
    /// <returns>The segment if found and of correct type, null otherwise</returns>
    public T? GetSegment<T>(string segmentId, int? sequence = null) where T : HL7Segment
    {
        var key = sequence.HasValue ? $"{segmentId}{sequence}" : segmentId;
        return Segments.TryGetValue(key, out var segment) ? segment as T : null;
    }

    /// <summary>
    /// Gets all segments of a specific type, useful for repeating segments.
    /// </summary>
    /// <typeparam name="T">The expected segment type</typeparam>
    /// <param name="segmentId">The base segment ID (e.g., "OBX")</param>
    /// <returns>All segments matching the type</returns>
    public IEnumerable<T> GetSegments<T>(string segmentId) where T : HL7Segment
    {
        return Segments
            .Where(kvp => kvp.Key.StartsWith(segmentId))
            .Select(kvp => kvp.Value)
            .OfType<T>();
    }
}

/// <summary>
/// Represents HL7 encoding characters used in message serialization.
/// </summary>
public record HL7EncodingChars
{
    /// <summary>
    /// Field separator (usually |).
    /// </summary>
    public char FieldSeparator { get; init; } = '|';

    /// <summary>
    /// Component separator (usually ^).
    /// </summary>
    public char ComponentSeparator { get; init; } = '^';

    /// <summary>
    /// Repetition separator (usually ~).
    /// </summary>
    public char RepetitionSeparator { get; init; } = '~';

    /// <summary>
    /// Escape character (usually \).
    /// </summary>
    public char EscapeCharacter { get; init; } = '\\';

    /// <summary>
    /// Subcomponent separator (usually &).
    /// </summary>
    public char SubcomponentSeparator { get; init; } = '&';

    /// <summary>
    /// Standard HL7 encoding characters.
    /// </summary>
    public static HL7EncodingChars Standard => new();

    /// <summary>
    /// Gets the encoding characters as a string for MSH.2 (^~\&).
    /// </summary>
    public string EncodingString =>
        $"{ComponentSeparator}{RepetitionSeparator}{EscapeCharacter}{SubcomponentSeparator}";
}

/// <summary>
/// Represents an HL7 message type and trigger event.
/// </summary>
public record HL7MessageType
{
    /// <summary>
    /// Gets the message code (e.g., "ORM", "ADT", "RDE").
    /// </summary>
    public required string MessageCode { get; init; }

    /// <summary>
    /// Gets the trigger event (e.g., "O01", "A01", "O11").
    /// </summary>
    public required string TriggerEvent { get; init; }

    /// <summary>
    /// Gets the full message type (e.g., "ORM^O01").
    /// </summary>
    public string MessageTypeCode => $"{MessageCode}^{TriggerEvent}";

    /// <summary>
    /// Creates an HL7MessageType from a message type string.
    /// </summary>
    /// <param name="messageType">The message type (e.g., "ORM^O01")</param>
    /// <returns>An HL7MessageType instance</returns>
    public static HL7MessageType Parse(string messageType)
    {
        var parts = messageType.Split('^');
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid HL7 message type format: {messageType}. Expected format: CODE^EVENT");

        return new HL7MessageType
        {
            MessageCode = parts[0],
            TriggerEvent = parts[1]
        };
    }

    /// <summary>
    /// Common HL7 message types.
    /// </summary>
    public static class Common
    {
        public static HL7MessageType ORM_O01 => new() { MessageCode = "ORM", TriggerEvent = "O01" };
        public static HL7MessageType ADT_A01 => new() { MessageCode = "ADT", TriggerEvent = "A01" };
        public static HL7MessageType ADT_A08 => new() { MessageCode = "ADT", TriggerEvent = "A08" };
        public static HL7MessageType RDE_O11 => new() { MessageCode = "RDE", TriggerEvent = "O11" };
        public static HL7MessageType ORU_R01 => new() { MessageCode = "ORU", TriggerEvent = "R01" };
    }
}

/// <summary>
/// Base class for all HL7 segments.
/// </summary>
public abstract record HL7Segment
{
    /// <summary>
    /// Gets the segment ID (e.g., "MSH", "PID", "OBR").
    /// </summary>
    public abstract string SegmentId { get; }

    /// <summary>
    /// Gets the sequence number for this segment instance (for repeating segments).
    /// </summary>
    public int SequenceNumber { get; init; } = 1;

    /// <summary>
    /// Validates the segment structure and required fields.
    /// Derived segments should override to add segment-specific validation.
    /// </summary>
    /// <returns>A result indicating whether the segment is valid</returns>
    public virtual Result<HL7Segment> Validate()
    {
        if (string.IsNullOrWhiteSpace(SegmentId))
            return Error.Validation("Segment ID is required", nameof(SegmentId));

        if (SequenceNumber < 1)
            return Error.Validation("Sequence number must be positive", nameof(SequenceNumber));

        return Result<HL7Segment>.Success(this);
    }
}