// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Domain.Messaging.HL7v2.Segments;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Domain.Messaging.HL7v2.Common;
using Pidgeon.Core.Application.Common;
using Pidgeon.Core.Common.Constants;
using System.Linq;

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
    public virtual HL7MessageType MessageType { get; set; } = new HL7MessageType();

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
        // Support MSH segment in all HL7 messages
        return segmentId switch
        {
            "MSH" => new MSHSegment(),
            _ => null
        };
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
    /// Gets all segments in this message, maintaining HL7-required order.
    /// MSH segment should always be first, followed by other segments in proper sequence.
    /// </summary>
    public List<HL7Segment> Segments { get; set; } = new();

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

        if (!Segments.Any(s => s.SegmentId == "MSH"))
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
        if (sequence.HasValue)
        {
            // For specific sequence, find the Nth occurrence of the segment type
            var segmentsOfType = Segments.Where(s => s.SegmentId == segmentId && s is T).Cast<T>().ToList();
            return sequence.Value > 0 && sequence.Value <= segmentsOfType.Count 
                ? segmentsOfType[sequence.Value - 1] // 1-based indexing
                : null;
        }
        
        // For no sequence specified, get first occurrence
        return Segments.FirstOrDefault(s => s.SegmentId == segmentId && s is T) as T;
    }

    /// <summary>
    /// Gets all segments of a specific type, useful for repeating segments.
    /// </summary>
    /// <typeparam name="T">The expected segment type</typeparam>
    /// <param name="segmentId">The base segment ID (e.g., "OBX")</param>
    /// <returns>All segments matching the type</returns>
    public IEnumerable<T> GetSegments<T>(string segmentId) where T : HL7Segment
    {
        return Segments.Where(s => s.SegmentId == segmentId).OfType<T>();
    }

    /// <summary>
    /// Gets the MSH segment (message header) if present.
    /// </summary>
    public MSHSegment? MSH => GetSegment<MSHSegment>("MSH");

    /// <summary>
    /// Adds a segment to this message.
    /// MSH segments are automatically placed first; other segments are appended.
    /// </summary>
    /// <param name="segment">The segment to add</param>
    public void AddSegment(HL7Segment segment)
    {
        if (segment == null) throw new ArgumentNullException(nameof(segment));
        
        // Set sequence number for repeating segments
        var existingCount = Segments.Count(s => s.SegmentId == segment.SegmentId);
        segment.SequenceNumber = existingCount + 1;
        
        // MSH segment should always be first
        if (segment.SegmentId == "MSH")
        {
            // Remove any existing MSH and add at the beginning
            Segments.RemoveAll(s => s.SegmentId == "MSH");
            Segments.Insert(0, segment);
        }
        else
        {
            Segments.Add(segment);
        }
    }

    #region IStandardMessage Implementation

    /// <summary>
    /// Gets the message type as a string (implements IStandardMessage.MessageType).
    /// </summary>
    string IStandardMessage.MessageType => MessageType?.MessageTypeCode ?? "Unknown";

    /// <summary>
    /// Gets the standard version (implements IStandardMessage.StandardVersion).
    /// </summary>
    Version IStandardMessage.StandardVersion => System.Version.Parse(Version);

    /// <summary>
    /// Serializes the message to its HL7 string representation.
    /// </summary>
    /// <param name="options">Serialization options</param>
    /// <returns>A result containing the serialized message or an error</returns>
    public virtual Result<string> Serialize(SerializationOptions? options = null)
    {
        try
        {
            var segments = Segments.Select(s => s.ToHL7String());
                
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
        foreach (var segment in Segments)
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
        if (string.IsNullOrWhiteSpace(hl7Content))
            return Error.Parsing("HL7 content is empty", "HL7Message");

        try
        {
            // Split into segments
            var segmentStrings = hl7Content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            // Clear existing segments
            Segments.Clear();
            
            // Parse each segment
            foreach (var segmentString in segmentStrings)
            {
                if (string.IsNullOrWhiteSpace(segmentString)) continue;
                
                var segmentId = segmentString.Length >= 3 ? segmentString.Substring(0, 3) : "";
                var segment = CreateSegmentFromId(segmentId);
                
                if (segment != null)
                {
                    // Initialize fields first
                    segment.InitializeFields();
                    
                    // Parse the segment string
                    var parseResult = segment.ParseHL7String(segmentString);
                    if (parseResult.IsFailure)
                        return Error.Parsing($"Failed to parse {segmentId} segment: {parseResult.Error.Message}", "HL7Message");
                    
                    AddSegment(segment);
                }
            }
            
            return Result<HL7Message>.Success(this);
        }
        catch (Exception ex)
        {
            return Error.Parsing($"Exception during message parsing: {ex.Message}\nStack Trace: {ex.StackTrace}", "HL7Message");
        }
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
    public char FieldSeparator { get; init; } = HL7Constants.FieldSeparator;

    /// <summary>
    /// Component separator (usually ^).
    /// </summary>
    public char ComponentSeparator { get; init; } = HL7Constants.ComponentSeparator;

    /// <summary>
    /// Repetition separator (usually ~).
    /// </summary>
    public char RepetitionSeparator { get; init; } = HL7Constants.RepetitionSeparator;

    /// <summary>
    /// Escape character (usually \).
    /// </summary>
    public char EscapeCharacter { get; init; } = HL7Constants.EscapeCharacter;

    /// <summary>
    /// Subcomponent separator (usually &).
    /// </summary>
    public char SubcomponentSeparator { get; init; } = HL7Constants.SubcomponentSeparator;

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
    public string MessageCode { get; init; } = string.Empty;

    /// <summary>
    /// Gets the trigger event (e.g., "O01", "A01", "O11").
    /// </summary>
    public string TriggerEvent { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full message type (e.g., "ORM^O01").
    /// </summary>
    public string MessageTypeCode => $"{MessageCode}^{TriggerEvent}";

    /// <summary>
    /// Creates an HL7MessageType from a message type string.
    /// </summary>
    /// <param name="messageType">The message type (e.g., "ORM^O01")</param>
    /// <returns>A result containing an HL7MessageType instance or validation error</returns>
    public static Result<HL7MessageType> Parse(string messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType))
            return Result<HL7MessageType>.Failure(Error.Validation("Message type cannot be null or empty", "MessageType"));

        var parts = messageType.Split('^');
        if (parts.Length != 2)
            return Result<HL7MessageType>.Failure(Error.Validation($"Invalid HL7 message type format: {messageType}. Expected format: CODE^EVENT", "MessageType"));

        return Result<HL7MessageType>.Success(new HL7MessageType
        {
            MessageCode = parts[0],
            TriggerEvent = parts[1]
        });
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
    /// Protected access to fields for serialization in derived classes.
    /// </summary>
    protected IEnumerable<HL7Field> Fields => _fields.Values;
    
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
    /// Sets a field value at a specific position (1-based indexing per HL7 standard).
    /// </summary>
    /// <param name="position">The field position (1-based)</param>
    /// <param name="value">The string value to set</param>
    protected void SetField(int position, string value)
    {
        _fields[position] = new StringField(value);
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
    /// Gets the field definitions for this segment type.
    /// Override in concrete segments to define segment-specific fields.
    /// </summary>
    /// <returns>Collection of field definitions</returns>
    protected virtual IEnumerable<SegmentFieldDefinition> GetFieldDefinitions()
    {
        // Default returns empty - override in concrete segments
        return Enumerable.Empty<SegmentFieldDefinition>();
    }

    /// <summary>
    /// Initializes fields for this segment with default values.
    /// Override in concrete segments to set up required fields.
    /// </summary>
    public virtual void InitializeFields()
    {
        // Use declarative field definitions if available
        var fieldDefinitions = GetFieldDefinitions();
        if (fieldDefinitions.Any())
        {
            foreach (var definition in fieldDefinitions)
            {
                AddField(definition.Position, definition.CreateField());
            }
        }
        // Otherwise default implementation - override in concrete segments for custom logic
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
        if (string.IsNullOrWhiteSpace(segmentString))
            return Error.Parsing("Segment string is empty", "HL7Segment");

        try
        {
            // Split by field separator (|)
            var fieldValues = segmentString.Split('|');
            
            // First field is segment ID, skip it (start from index 1)
            for (int i = 1; i < fieldValues.Length; i++)
            {
                var fieldPosition = i; // 1-based position matches HL7 standard
                
                if (_fields.TryGetValue(fieldPosition, out var field))
                {
                    var setResult = field.SetValue(fieldValues[i]);
                    if (setResult.IsFailure)
                        return Error.Parsing($"Failed to set field {fieldPosition}: {setResult.Error.Message}", "HL7Segment");
                }
            }
            
            return Result<HL7Segment>.Success(this);
        }
        catch (Exception ex)
        {
            return Error.Parsing($"Exception during segment parsing: {ex.Message}", "HL7Segment");
        }
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