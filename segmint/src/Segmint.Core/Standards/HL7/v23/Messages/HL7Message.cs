// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.HL7.Validation;
using Segmint.Core.Standards.HL7.v23.Segments;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text;
using System;
namespace Segmint.Core.Standards.HL7.v23.Messages;

/// <summary>
/// Represents a complete HL7 message containing multiple segments.
/// </summary>
public abstract class HL7Message : IEnumerable<HL7Segment>
{
    private readonly List<HL7Segment> _segments = new();

    /// <summary>
    /// Gets the message type code (e.g., "RDE", "ADT", "ACK").
    /// </summary>
    public abstract string MessageType { get; }

    /// <summary>
    /// Gets the trigger event code (e.g., "O01", "A01", "R01").
    /// </summary>
    public abstract string TriggerEvent { get; }

    /// <summary>
    /// Gets the message structure (e.g., "RDE_O01", "ADT_A01", "ACK_R01").
    /// </summary>
    public virtual string MessageStructure => $"{MessageType}_{TriggerEvent}";

    /// <summary>
    /// Gets the number of segments in this message.
    /// </summary>
    [JsonIgnore]
    public int SegmentCount => _segments.Count;

    /// <summary>
    /// Gets or sets the segment at the specified index.
    /// </summary>
    /// <param name="index">The zero-based segment index.</param>
    /// <returns>The segment at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    public HL7Segment this[int index]
    {
        get
        {
            if (index < 0 || index >= _segments.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Segment index must be between 0 and {_segments.Count - 1}.");
            
            return _segments[index];
        }
        set
        {
            if (index < 0 || index >= _segments.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Segment index must be between 0 and {_segments.Count - 1}.");
            
            _segments[index] = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HL7Message"/> class.
    /// </summary>
    protected HL7Message()
    {
        InitializeMessage();
    }

    /// <summary>
    /// Initializes the message structure with required segments.
    /// Derived classes should override this method to set up their specific segment structure.
    /// </summary>
    protected abstract void InitializeMessage();

    /// <summary>
    /// Adds a segment to this message.
    /// </summary>
    /// <param name="segment">The segment to add.</param>
    protected void AddSegment(HL7Segment segment)
    {
        _segments.Add(segment ?? throw new ArgumentNullException(nameof(segment)));
    }

    /// <summary>
    /// Inserts a segment at the specified position.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the segment.</param>
    /// <param name="segment">The segment to insert.</param>
    public void InsertSegment(int index, HL7Segment segment)
    {
        if (index < 0 || index > _segments.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index must be between 0 and {_segments.Count}.");
        
        _segments.Insert(index, segment ?? throw new ArgumentNullException(nameof(segment)));
    }

    /// <summary>
    /// Removes the segment at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the segment to remove.</param>
    public void RemoveSegmentAt(int index)
    {
        if (index < 0 || index >= _segments.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Segment index must be between 0 and {_segments.Count - 1}.");
        
        _segments.RemoveAt(index);
    }

    /// <summary>
    /// Removes the specified segment from the message.
    /// </summary>
    /// <param name="segment">The segment to remove.</param>
    /// <returns>True if the segment was found and removed; otherwise, false.</returns>
    public bool RemoveSegment(HL7Segment segment)
    {
        return _segments.Remove(segment);
    }

    /// <summary>
    /// Gets the first segment of the specified type.
    /// </summary>
    /// <typeparam name="T">The segment type to find.</typeparam>
    /// <returns>The first segment of the specified type, or null if not found.</returns>
    public T? GetSegment<T>() where T : HL7Segment
    {
        return _segments.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Gets all segments of the specified type.
    /// </summary>
    /// <typeparam name="T">The segment type to find.</typeparam>
    /// <returns>All segments of the specified type.</returns>
    public IEnumerable<T> GetSegments<T>() where T : HL7Segment
    {
        return _segments.OfType<T>();
    }

    /// <summary>
    /// Gets the first segment with the specified segment ID.
    /// </summary>
    /// <param name="segmentId">The segment ID to find (e.g., "MSH", "PID", "RXE").</param>
    /// <returns>The first segment with the specified ID, or null if not found.</returns>
    public HL7Segment? GetSegment(string segmentId)
    {
        return _segments.FirstOrDefault(s => s.SegmentId.Equals(segmentId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all segments with the specified segment ID.
    /// </summary>
    /// <param name="segmentId">The segment ID to find (e.g., "MSH", "PID", "RXE").</param>
    /// <returns>All segments with the specified ID.</returns>
    public IEnumerable<HL7Segment> GetSegments(string segmentId)
    {
        return _segments.Where(s => s.SegmentId.Equals(segmentId, StringComparison.OrdinalIgnoreCase));
    }


    /// <summary>
    /// Converts this message to its HL7 string representation.
    /// </summary>
    /// <param name="useCarriageReturn">Whether to use carriage return (\\r) as segment separator. Default is true.</param>
    /// <returns>The HL7-formatted message string.</returns>
    public virtual string ToHL7String(bool useCarriageReturn = true)
    {
        if (_segments.Count == 0)
            return string.Empty;

        var separator = useCarriageReturn ? "\r" : "\n";
        var sb = new StringBuilder();

        for (int i = 0; i < _segments.Count; i++)
        {
            if (i > 0)
                sb.Append(separator);
            
            sb.Append(_segments[i].ToHL7String());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts this message to network transport format with start/end markers.
    /// </summary>
    /// <returns>The message formatted for network transport.</returns>
    public virtual string ToNetworkString()
    {
        var hl7String = ToHL7String();
        return $"\x0B{hl7String}\x1C\r"; // Start block (VT), message, end block (FS), carriage return
    }

    /// <summary>
    /// Parses an HL7 message string into this message instance.
    /// </summary>
    /// <param name="hl7String">The HL7 message string to parse.</param>
    /// <exception cref="ArgumentException">Thrown when the message string is invalid.</exception>
    public virtual void FromHL7String(string hl7String)
    {
        if (string.IsNullOrEmpty(hl7String))
            throw new ArgumentException("HL7 string cannot be null or empty.", nameof(hl7String));

        // Clear existing segments
        _segments.Clear();

        // Split by carriage return or newline
        var segmentStrings = hl7String.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (segmentStrings.Length == 0)
            throw new ArgumentException("HL7 string must contain at least one segment.", nameof(hl7String));

        foreach (var segmentString in segmentStrings)
        {
            if (string.IsNullOrWhiteSpace(segmentString))
                continue;

            var segment = CreateSegmentFromString(segmentString);
            if (segment != null)
            {
                _segments.Add(segment);
            }
        }
    }

    /// <summary>
    /// Creates a segment instance from an HL7 segment string.
    /// </summary>
    /// <param name="segmentString">The HL7 segment string.</param>
    /// <returns>The created segment instance, or null if the segment type is not recognized.</returns>
    protected virtual HL7Segment? CreateSegmentFromString(string segmentString)
    {
        if (string.IsNullOrEmpty(segmentString) || segmentString.Length < 3)
            return null;

        var segmentId = segmentString[..3];
        
        // Create segment based on type - this should be overridden in derived classes
        // for message-specific segment creation
        HL7Segment? segment = segmentId switch
        {
            "MSH" => new Segments.MSHSegment(),
            "EVN" => new Segments.EVNSegment(),
            "PID" => new Segments.PIDSegment(),
            "ORC" => new Segments.ORCSegment(),
            "RXE" => new Segments.RXESegment(),
            _ => null
        };

        if (segment != null)
        {
            segment.FromHL7String(segmentString);
        }

        return segment;
    }

    /// <summary>
    /// Creates a copy of this message.
    /// </summary>
    /// <returns>A new message instance with the same segment values.</returns>
    public abstract HL7Message Clone();

    /// <summary>
    /// Returns an enumerator that iterates through the segments.
    /// </summary>
    public IEnumerator<HL7Segment> GetEnumerator()
    {
        return _segments.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the segments.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Returns a string representation of this message.
    /// </summary>
    public override string ToString()
    {
        return ToHL7String();
    }

    /// <summary>
    /// Validates this HL7 message and returns the validation result.
    /// </summary>
    /// <returns>A ValidationResult containing any validation issues found.</returns>
    public virtual ValidationResult Validate()
    {
        var result = new ValidationResult();
        
        // Validate that we have a message header
        if (MessageHeader == null)
        {
            result.AddIssue(ValidationIssue.SemanticError("MSG001", "Message header (MSH) is required", "MSH"));
        }
        else
        {
            // Validate MSH segment
            ValidateSegment(MessageHeader, result);
        }
        
        // Validate all segments
        foreach (var segment in _segments)
        {
            ValidateSegment(segment, result);
        }
        
        // Perform message-level validation
        ValidateMessageStructure(result);
        
        return result;
    }

    /// <summary>
    /// Validates the overall message structure.
    /// Override in derived classes for message-specific validation rules.
    /// </summary>
    /// <param name="result">The validation result to add issues to.</param>
    protected virtual void ValidateMessageStructure(ValidationResult result)
    {
        // Base implementation - can be overridden by specific message types
        if (SegmentCount == 0)
        {
            result.AddIssue(ValidationIssue.SemanticError("MSG002", "Message must contain at least one segment", "Message"));
        }
    }

    /// <summary>
    /// Validates a single segment.
    /// </summary>
    /// <param name="segment">The segment to validate.</param>
    /// <param name="result">The validation result to add issues to.</param>
    protected virtual void ValidateSegment(HL7Segment segment, ValidationResult result)
    {
        if (segment == null)
        {
            result.AddIssue(ValidationIssue.SemanticError("SEG001", "Segment cannot be null", "Unknown"));
            return;
        }
        
        // Validate required fields in the segment
        // This is a basic implementation - segments can override for specific validation
        for (int i = 0; i < segment.FieldCount; i++)
        {
            var field = segment[i];
            if (field != null && field.IsRequired && field.IsEmpty)
            {
                result.AddIssue(ValidationIssue.SemanticError("FLD001", $"Required field is empty", $"{segment.SegmentId}.{i + 1}"));
            }
        }
    }

    /// <summary>
    /// Gets summary information about this message.
    /// </summary>
    /// <returns>A string containing message type, segment count, and validation status.</returns>
    public virtual string GetSummary()
    {
        var validationResult = Validate();
        var isValid = validationResult.IsValid;
        
        return $"{MessageType}^{TriggerEvent} - {SegmentCount} segments - {(isValid ? "Valid" : $"{validationResult.ErrorCount} errors")}";
    }

    /// <summary>
    /// Clears all segments from this message.
    /// </summary>
    protected void ClearSegments()
    {
        _segments.Clear();
    }

    /// <summary>
    /// Gets or sets the message header (MSH) segment.
    /// </summary>
    public virtual MSHSegment? MessageHeader
    {
        get => GetSegment<MSHSegment>();
        set { /* Override in derived classes for specific behavior */ }
    }

}
