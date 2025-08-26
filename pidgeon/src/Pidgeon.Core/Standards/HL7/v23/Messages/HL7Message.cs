// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Standards.Common;
using Pidgeon.Core.Standards.HL7.v23.Segments;

namespace Pidgeon.Core.Standards.HL7.v23.Messages;

/// <summary>
/// Base class for all HL7 v2.3 messages.
/// Provides common functionality for segment management, serialization, and validation.
/// </summary>
public abstract class HL7Message : IStandardMessage
{
    /// <summary>
    /// List of segments in this message.
    /// </summary>
    protected readonly List<HL7Segment> Segments = new();

    /// <summary>
    /// Gets the MSH (Message Header) segment - required for all HL7 messages.
    /// </summary>
    public MSHSegment MSH => GetSegment<MSHSegment>()!;

    public abstract string MessageType { get; }
    public string Standard => "HL7";
    public Version StandardVersion => new(2, 3);

    /// <summary>
    /// HL7 segment separator (carriage return).
    /// </summary>
    public const char SegmentSeparator = '\r';

    protected HL7Message()
    {
        InitializeMessage();
    }

    /// <summary>
    /// Initializes the message with required segments.
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract void InitializeMessage();

    /// <summary>
    /// Adds a segment to the message.
    /// </summary>
    /// <param name="segment">The segment to add</param>
    /// <param name="index">Optional index to insert at (appends if null)</param>
    protected void AddSegment(HL7Segment segment, int? index = null)
    {
        if (index.HasValue && index.Value >= 0 && index.Value <= Segments.Count)
        {
            Segments.Insert(index.Value, segment);
        }
        else
        {
            Segments.Add(segment);
        }
    }

    /// <summary>
    /// Gets the first segment of the specified type.
    /// </summary>
    /// <typeparam name="T">The segment type</typeparam>
    /// <returns>The segment, or null if not found</returns>
    public T? GetSegment<T>() where T : HL7Segment
    {
        return Segments.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Gets all segments of the specified type.
    /// </summary>
    /// <typeparam name="T">The segment type</typeparam>
    /// <returns>List of segments of the specified type</returns>
    public List<T> GetSegments<T>() where T : HL7Segment
    {
        return Segments.OfType<T>().ToList();
    }

    /// <summary>
    /// Gets segments by segment ID.
    /// </summary>
    /// <param name="segmentId">The segment ID (e.g., "PID")</param>
    /// <returns>List of segments with the specified ID</returns>
    public List<HL7Segment> GetSegmentsByID(string segmentId)
    {
        return Segments.Where(s => s.SegmentId.Equals(segmentId, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Removes all segments of the specified type.
    /// </summary>
    /// <typeparam name="T">The segment type</typeparam>
    /// <returns>Number of segments removed</returns>
    public int RemoveSegments<T>() where T : HL7Segment
    {
        var segmentsToRemove = Segments.OfType<T>().ToList();
        foreach (var segment in segmentsToRemove)
        {
            Segments.Remove(segment);
        }
        return segmentsToRemove.Count;
    }

    /// <summary>
    /// Removes a specific segment instance.
    /// </summary>
    /// <param name="segment">The segment to remove</param>
    /// <returns>True if removed, false if not found</returns>
    public bool RemoveSegment(HL7Segment segment)
    {
        return Segments.Remove(segment);
    }

    /// <summary>
    /// Gets the total number of segments in the message.
    /// </summary>
    public int SegmentCount => Segments.Count;

    /// <summary>
    /// Gets all segments in the message.
    /// </summary>
    /// <returns>Read-only list of segments</returns>
    public IReadOnlyList<HL7Segment> GetAllSegments()
    {
        return Segments.AsReadOnly();
    }

    /// <summary>
    /// Serializes the message to its HL7 string representation.
    /// </summary>
    /// <param name="options">Serialization options</param>
    /// <returns>A result containing the HL7 string or an error</returns>
    public Result<string> Serialize(SerializationOptions? options = null)
    {
        try
        {
            var segmentStrings = new List<string>();

            foreach (var segment in Segments)
            {
                // Skip empty segments unless specifically requested
                if (!segment.HasData && options?.IncludeOptionalFields != true)
                    continue;

                segmentStrings.Add(segment.ToHL7String());
            }

            if (!segmentStrings.Any())
                return Error.Create("MESSAGE_EMPTY", "Message contains no segments with data");

            var result = string.Join(SegmentSeparator.ToString(), segmentStrings);

            // Add final segment separator if formatting for readability
            if (options?.FormatForReadability == true)
            {
                result += SegmentSeparator;
            }

            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            return Error.Create("SERIALIZATION_ERROR", $"Failed to serialize HL7 message: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses an HL7 message string into this message object.
    /// </summary>
    /// <param name="hl7String">The HL7 message string</param>
    /// <returns>A result indicating success or failure</returns>
    public Result<HL7Message> ParseHL7String(string hl7String)
    {
        if (string.IsNullOrWhiteSpace(hl7String))
            return Error.Parsing("HL7 message string cannot be empty", "HL7Message");

        try
        {
            // Split into segments (handle both \r and \n as separators)
            var segmentStrings = hl7String
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (!segmentStrings.Any())
                return Error.Parsing("No valid segments found in HL7 message", "HL7Message");

            // Clear existing segments
            Segments.Clear();

            // Parse each segment
            foreach (var segmentString in segmentStrings)
            {
                var parseResult = ParseSegmentString(segmentString);
                if (parseResult.IsFailure)
                    return Error.Parsing($"Failed to parse segment: {parseResult.Error.Message}", "HL7Message");

                if (parseResult.Value != null)
                {
                    Segments.Add(parseResult.Value);
                }
            }

            // Ensure we have an MSH segment
            if (!Segments.Any(s => s is MSHSegment))
                return Error.Parsing("HL7 message must contain an MSH segment", "HL7Message");

            // Validate message structure
            var validation = ValidateMessageStructure();
            if (validation.IsFailure)
                return validation;

            return Result<HL7Message>.Success(this);
        }
        catch (Exception ex)
        {
            return Error.Parsing($"Exception parsing HL7 message: {ex.Message}", "HL7Message");
        }
    }

    /// <summary>
    /// Parses a single segment string and returns the appropriate segment object.
    /// </summary>
    /// <param name="segmentString">The segment string to parse</param>
    /// <returns>A result containing the parsed segment or an error</returns>
    protected virtual Result<HL7Segment?> ParseSegmentString(string segmentString)
    {
        if (string.IsNullOrWhiteSpace(segmentString))
            return Result<HL7Segment?>.Success(null);

        // Get segment ID (first 3 characters)
        if (segmentString.Length < 3)
            return Error.Parsing($"Invalid segment string: {segmentString}", "HL7Segment");

        var segmentId = segmentString.Substring(0, 3);

        // Create appropriate segment based on ID
        HL7Segment? segment = segmentId switch
        {
            "MSH" => new MSHSegment(),
            _ => CreateSegmentFromId(segmentId)
        };

        if (segment == null)
            return Error.Parsing($"Unknown segment type: {segmentId}", "HL7Segment");

        // Parse the segment
        var parseResult = segment.ParseHL7String(segmentString);
        if (parseResult.IsFailure)
            return Error.Parsing($"Failed to parse {segmentId} segment: {parseResult.Error.Message}", "HL7Segment");

        return Result<HL7Segment?>.Success(segment);
    }

    /// <summary>
    /// Creates a segment instance from a segment ID.
    /// Can be overridden by derived classes to handle message-specific segments.
    /// </summary>
    /// <param name="segmentId">The segment ID</param>
    /// <returns>A segment instance or null if unknown</returns>
    protected virtual HL7Segment? CreateSegmentFromId(string segmentId)
    {
        // Base implementation returns null - derived classes should override
        return null;
    }

    /// <summary>
    /// Validates the message structure and content.
    /// </summary>
    /// <param name="validationMode">The validation mode to use</param>
    /// <returns>A result containing validation results</returns>
    public Result<ValidationResult> Validate(ValidationMode validationMode = ValidationMode.Strict)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        try
        {
            // Validate message structure
            var structureValidation = ValidateMessageStructure();
            if (structureValidation.IsFailure)
            {
                errors.Add(new ValidationError
                {
                    Code = "STRUCTURE_ERROR",
                    Message = structureValidation.Error.Message,
                    Severity = ValidationSeverity.Error
                });
            }

            // Validate each segment
            foreach (var segment in Segments)
            {
                var segmentValidation = segment.Validate();
                if (segmentValidation.IsFailure)
                {
                    errors.Add(new ValidationError
                    {
                        Code = "SEGMENT_ERROR",
                        Message = segmentValidation.Error.Message,
                        Field = segment.SegmentId,
                        Severity = ValidationSeverity.Error
                    });
                }
            }

            // Additional validation based on mode
            if (validationMode == ValidationMode.Strict)
            {
                ValidateStrictMode(errors, warnings);
            }
            else if (validationMode == ValidationMode.Compatibility)
            {
                ValidateCompatibilityMode(errors, warnings);
            }

            var isValid = !errors.Any();
            var context = new ValidationContext
            {
                Mode = validationMode,
                AppliedRules = new[] { "MessageStructure", "SegmentValidation", validationMode.ToString() }
            };

            var result = new ValidationResult
            {
                IsValid = isValid,
                Errors = errors,
                Warnings = warnings,
                Context = context
            };

            return Result<ValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Error.Create("VALIDATION_ERROR", $"Exception during validation: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates message structure (segment order, required segments).
    /// </summary>
    /// <returns>A result indicating whether the structure is valid</returns>
    protected virtual Result<HL7Message> ValidateMessageStructure()
    {
        // Must have MSH as first segment
        if (!Segments.Any() || !(Segments.First() is MSHSegment))
            return Error.Validation("Message must start with MSH segment", "MessageStructure");

        return Result<HL7Message>.Success(this);
    }

    /// <summary>
    /// Performs strict mode validation.
    /// </summary>
    protected virtual void ValidateStrictMode(List<ValidationError> errors, List<ValidationWarning> warnings)
    {
        // Override in derived classes for message-specific strict validation
    }

    /// <summary>
    /// Performs compatibility mode validation.
    /// </summary>
    protected virtual void ValidateCompatibilityMode(List<ValidationError> errors, List<ValidationWarning> warnings)
    {
        // Override in derived classes for message-specific compatibility validation
    }

    /// <summary>
    /// Gets metadata about the message.
    /// </summary>
    /// <returns>Message metadata</returns>
    public MessageMetadata GetMetadata()
    {
        var serialized = Serialize();
        var sizeInBytes = serialized.IsSuccess ? System.Text.Encoding.UTF8.GetByteCount(serialized.Value) : 0;

        return new MessageMetadata
        {
            CreatedAt = MSH?.DateTimeOfMessage.TypedValue ?? DateTime.UtcNow,
            CreatedBy = MSH?.SendingApplication.Value,
            SizeInBytes = sizeInBytes,
            Properties = new Dictionary<string, object>
            {
                ["MessageType"] = MessageType,
                ["MessageControlId"] = MSH?.MessageControlId.Value ?? string.Empty,
                ["SegmentCount"] = SegmentCount,
                ["Standard"] = Standard,
                ["Version"] = StandardVersion.ToString()
            }
        };
    }

    /// <summary>
    /// String representation of the message.
    /// </summary>
    /// <returns>HL7 string representation</returns>
    public override string ToString()
    {
        var result = Serialize();
        return result.IsSuccess ? result.Value : $"[Error: {result.Error.Message}]";
    }
}