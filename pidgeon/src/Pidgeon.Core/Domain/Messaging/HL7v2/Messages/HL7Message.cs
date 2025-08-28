// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Domain.Messaging.HL7v2.Segments;
using Pidgeon.Core.Infrastructure.Standards.Abstractions;
using Pidgeon.Core.Infrastructure.Standards.Common;
using Pidgeon.Core.Infrastructure.Standards.Common.HL7;
using Pidgeon.Core.Standards.Common;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Messages;

/// <summary>
/// Base class for all HL7 v2.x messages.
/// Captures HL7-specific concepts like encoding characters and segment structure.
/// </summary>
public abstract class HL7Message : HealthcareMessage, IStandardMessage
{
    /// <summary>
    /// Gets the HL7 encoding characters used for field, component, and escape sequences.
    /// Default: Field=|, Component=^, Repetition=~, Escape=\, Subcomponent=&
    /// </summary>
    public HL7EncodingChars Encoding { get; set; } = HL7EncodingChars.Standard;

    /// <summary>
    /// Gets the HL7 message type and trigger event (e.g., "ORM^O01").
    /// </summary>
    public virtual required HL7MessageType MessageType { get; set; }

    /// <summary>
    /// Initializes the message with default segments and structure.
    /// Override in concrete message types to set up required segments.
    /// </summary>
    public virtual void InitializeMessage()
    {
        // Default implementation - override in concrete messages
    }

    /// <summary>
    /// Creates a segment instance from a segment ID.
    /// Override in concrete message types to support specific segment types.
    /// </summary>
    /// <param name="segmentId">The segment ID</param>
    /// <returns>A segment instance or null if not supported</returns>
    protected virtual HL7Segment? CreateSegmentFromId(string segmentId)
    {
        // Default implementation - override in concrete messages
        return null;
    }

    /// <summary>
    /// Validates message-specific structure and business rules.
    /// Override in concrete message types to add specific validation.
    /// </summary>
    /// <returns>A validation result</returns>
    protected virtual Result<HL7Message> ValidateMessageStructure()
    {
        return Result<HL7Message>.Success(this);
    }


    /// <summary>
    /// Gets all segments in this message, keyed by segment ID.
    /// For repeating segments, use segment ID with sequence number (e.g., "OBX1", "OBX2").
    /// </summary>
    public Dictionary<string, HL7Segment> Segments { get; set; } = new();

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

    /// <summary>
    /// Gets the MSH segment (message header) if present.
    /// </summary>
    public MSHSegment? MSH => GetSegment<MSHSegment>("MSH");

    /// <summary>
    /// Adds a segment to this message.
    /// </summary>
    /// <param name="segment">The segment to add</param>
    public void AddSegment(HL7Segment segment)
    {
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        
        // For repeating segments, append sequence number
        var key = segment.SegmentId;
        if (Segments.ContainsKey(key))
        {
            // Find the next available sequence number
            var sequence = 2;
            while (Segments.ContainsKey($"{key}{sequence}"))
            {
                sequence++;
            }
            key = $"{key}{sequence}";
            segment.SequenceNumber = sequence;
        }
        
        Segments[key] = segment;
    }

    #region IStandardMessage Implementation

    /// <summary>
    /// Gets the message type as a string (implements IStandardMessage.MessageType).
    /// </summary>
    string IStandardMessage.MessageType => MessageType?.MessageTypeCode ?? "Unknown";

    /// <summary>
    /// Gets the standard version (implements IStandardMessage.StandardVersion).
    /// </summary>
    Version IStandardMessage.StandardVersion => Version.Parse(Version);

    /// <summary>
    /// Serializes the message to its HL7 string representation.
    /// </summary>
    /// <param name="options">Serialization options</param>
    /// <returns>A result containing the serialized message or an error</returns>
    public virtual Result<string> Serialize(SerializationOptions? options = null)
    {
        try
        {
            var segments = Segments.Values
                .OrderBy(s => s.SegmentId == "MSH" ? 0 : s.SequenceNumber)
                .Select(s => s.ToHL7String());
                
            var hl7String = string.Join(Environment.NewLine, segments);
            return Result<string>.Success(hl7String);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to serialize HL7 message: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates the message structure and content.
    /// </summary>
    /// <param name="validationMode">The validation mode to use</param>
    /// <returns>A result containing validation results</returns>
    public virtual Result<ValidationResult> Validate(ValidationMode validationMode = ValidationMode.Strict)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        // Basic HL7 validation
        var baseValidation = base.Validate();
        if (!baseValidation.IsSuccess)
        {
            errors.Add(new ValidationError
            {
                Code = "BASE_VALIDATION_FAILED",
                Message = baseValidation.Error.Message
            });
        }

        // Segment validation
        foreach (var segment in Segments.Values)
        {
            var segmentResult = segment.Validate();
            if (!segmentResult.IsSuccess)
            {
                errors.Add(new ValidationError
                {
                    Code = "SEGMENT_VALIDATION_FAILED",
                    Message = $"Segment {segment.SegmentId}: {segmentResult.Error.Message}"
                });
            }
        }

        var result = new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };

        return Result<ValidationResult>.Success(result);
    }

    /// <summary>
    /// Gets metadata about the message.
    /// </summary>
    /// <returns>Message metadata</returns>
    public virtual MessageMetadata GetMetadata()
    {
        return new MessageMetadata
        {
            MessageType = ((IStandardMessage)this).MessageType,
            Standard = Standard,
            Version = ((IStandardMessage)this).StandardVersion.ToString(),
            SegmentCount = Segments.Count,
            CreatedAt = Timestamp,
            ControlId = MessageControlId
        };
    }

    /// <summary>
    /// Parses HL7 message content and updates this message's state.
    /// </summary>
    /// <param name="hl7Content">The complete HL7 message content</param>
    /// <returns>A result indicating success or failure</returns>
    public virtual Result<HL7Message> ParseHL7String(string hl7Content)
    {
        // TODO: Implement proper parsing that updates message state from hl7Content
        // This should parse segments and populate the Segments dictionary
        // For now, return success to allow compilation
        return Result<HL7Message>.Success(this);
    }

    #endregion
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
public abstract class HL7Segment
{
    /// <summary>
    /// Gets the segment ID (e.g., "MSH", "PID", "OBR").
    /// </summary>
    public abstract string SegmentId { get; }

    /// <summary>
    /// Gets the sequence number for this segment instance (for repeating segments).
    /// </summary>
    public int SequenceNumber { get; set; } = 1;

    /// <summary>
    /// Gets the display name for this segment type.
    /// Override in concrete segments to provide meaningful names.
    /// </summary>
    public virtual string DisplayName => SegmentId;

    /// <summary>
    /// Dictionary to store field values by position (1-based indexing per HL7 standard).
    /// </summary>
    private readonly Dictionary<int, HL7Field> _fields = new Dictionary<int, HL7Field>();
    
    /// <summary>
    /// Current field position counter for AddField operations.
    /// </summary>
    private int _currentFieldPosition = 1;

    /// <summary>
    /// Gets a field value by position (1-based indexing per HL7 standard).
    /// </summary>
    /// <typeparam name="T">The expected field type</typeparam>
    /// <param name="position">The field position (1-based)</param>
    /// <returns>The field value, or null if not found</returns>
    protected T? GetField<T>(int position) where T : HL7Field
    {
        if (_fields.TryGetValue(position, out var field))
        {
            return field as T;
        }
        return null;
    }

    /// <summary>
    /// Adds a field at the next available position.
    /// </summary>
    /// <param name="field">The field to add</param>
    protected void AddField(HL7Field field)
    {
        _fields[_currentFieldPosition] = field;
        _currentFieldPosition++;
    }

    /// <summary>
    /// Adds a field at a specific position.
    /// </summary>
    /// <param name="position">The field position (1-based)</param>
    /// <param name="field">The field to add</param>
    protected void AddField(int position, HL7Field field)
    {
        _fields[position] = field;
        if (position >= _currentFieldPosition)
        {
            _currentFieldPosition = position + 1;
        }
    }

    /// <summary>
    /// Gets all fields in position order.
    /// </summary>
    /// <returns>Fields ordered by position</returns>
    protected IEnumerable<KeyValuePair<int, HL7Field>> GetAllFields()
    {
        return _fields.OrderBy(kvp => kvp.Key);
    }

    /// <summary>
    /// Initializes fields for this segment with default values.
    /// Override in concrete segments to set up required fields.
    /// </summary>
    public virtual void InitializeFields()
    {
        // Default implementation - override in concrete segments
    }

    /// <summary>
    /// Converts the segment to HL7 string representation.
    /// Override in concrete segments to implement proper serialization.
    /// </summary>
    public virtual string ToHL7String()
    {
        return SegmentId; // Basic implementation - override for full serialization
    }

    /// <summary>
    /// Gets a display-friendly representation of the segment.
    /// Override in concrete segments to provide meaningful summaries.
    /// </summary>
    public virtual string GetDisplayValue()
    {
        return $"{DisplayName} ({SegmentId})";
    }

    /// <summary>
    /// Parses HL7 segment string and updates this segment's fields.
    /// </summary>
    /// <param name="segmentString">The HL7 segment string to parse</param>
    /// <returns>A result indicating success or failure</returns>
    public virtual Result<HL7Segment> ParseHL7String(string segmentString)
    {
        // TODO: Implement proper segment parsing that populates fields from segmentString
        // This should split by field separator and populate the _fields dictionary
        // For now, return success to allow compilation
        return Result<HL7Segment>.Success(this);
    }

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