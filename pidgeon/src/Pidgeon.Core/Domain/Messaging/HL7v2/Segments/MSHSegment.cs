// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Messaging.HL7v2.Common;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Segments;

/// <summary>
/// MSH - Message Header Segment.
/// Contains information about the message including sender, receiver, message type, and control information.
/// </summary>
public class MSHSegment : HL7Segment
{
    public override string SegmentId => "MSH";
    public override string DisplayName => "Message Header";

    // Field accessors (1-based indexing to match HL7 standard)
    public StringField EncodingCharacters => GetField<StringField>(1)!;
    public StringField SendingApplication => GetField<StringField>(2)!;
    public StringField SendingFacility => GetField<StringField>(3)!;
    public StringField ReceivingApplication => GetField<StringField>(4)!;
    public StringField ReceivingFacility => GetField<StringField>(5)!;
    public TimestampField DateTimeOfMessage => GetField<TimestampField>(6)!;
    public StringField Security => GetField<StringField>(7)!;
    public StringField MessageType => GetField<StringField>(8)!;
    public StringField MessageControlId => GetField<StringField>(9)!;
    public StringField ProcessingId => GetField<StringField>(10)!;
    public StringField VersionId => GetField<StringField>(11)!;
    public new NumericField SequenceNumber => GetField<NumericField>(12)!;
    public StringField ContinuationPointer => GetField<StringField>(13)!;
    public StringField AcceptAcknowledgmentType => GetField<StringField>(14)!;
    public StringField ApplicationAcknowledgmentType => GetField<StringField>(15)!;
    public StringField CountryCode => GetField<StringField>(16)!;

    /// <summary>
    /// Defines the fields for the MSH segment.
    /// </summary>
    protected override IEnumerable<SegmentFieldDefinition> GetFieldDefinitions()
    {
        return new[]
        {
            SegmentFieldDefinition.StringWithDefault(1, "Encoding Characters", "^~\\&", 4, true),    // MSH.1
            SegmentFieldDefinition.OptionalString(2, "Sending Application", 180),                    // MSH.2
            SegmentFieldDefinition.OptionalString(3, "Sending Facility", 180),                       // MSH.3
            SegmentFieldDefinition.OptionalString(4, "Receiving Application", 180),                  // MSH.4
            SegmentFieldDefinition.OptionalString(5, "Receiving Facility", 180),                     // MSH.5
            SegmentFieldDefinition.TimestampNow(6, "Date/Time of Message", true),                    // MSH.6
            SegmentFieldDefinition.OptionalString(7, "Security", 40),                                // MSH.7
            SegmentFieldDefinition.RequiredString(8, "Message Type", 7),                             // MSH.8
            SegmentFieldDefinition.RequiredString(9, "Message Control ID", 20),                      // MSH.9
            SegmentFieldDefinition.StringWithDefault(10, "Processing ID", "P", 1, true),             // MSH.10
            SegmentFieldDefinition.StringWithDefault(11, "Version ID", "2.3", 5, true),              // MSH.11
            SegmentFieldDefinition.Numeric(12, "Sequence Number"),                                   // MSH.12
            SegmentFieldDefinition.OptionalString(13, "Continuation Pointer", 180),                  // MSH.13
            SegmentFieldDefinition.OptionalString(14, "Accept Acknowledgment Type", 2),              // MSH.14
            SegmentFieldDefinition.OptionalString(15, "Application Acknowledgment Type", 2),         // MSH.15
            SegmentFieldDefinition.OptionalString(16, "Country Code", 2)                             // MSH.16
        };
    }

    /// <summary>
    /// Creates an MSH segment with common default values.
    /// </summary>
    /// <param name="sendingApplication">Sending application name</param>
    /// <param name="sendingFacility">Sending facility name</param>
    /// <param name="receivingApplication">Receiving application name</param>
    /// <param name="receivingFacility">Receiving facility name</param>
    /// <param name="messageType">Message type (e.g., "ADT^A01")</param>
    /// <param name="messageControlId">Unique message control ID</param>
    /// <returns>Configured MSH segment</returns>
    public static MSHSegment Create(
        string? sendingApplication = null,
        string? sendingFacility = null,
        string? receivingApplication = null,
        string? receivingFacility = null,
        string? messageType = null,
        string? messageControlId = null)
    {
        var msh = new MSHSegment();

        if (sendingApplication != null)
            msh.SendingApplication.SetValue(sendingApplication);
        
        if (sendingFacility != null)
            msh.SendingFacility.SetValue(sendingFacility);
        
        if (receivingApplication != null)
            msh.ReceivingApplication.SetValue(receivingApplication);
        
        if (receivingFacility != null)
            msh.ReceivingFacility.SetValue(receivingFacility);
        
        if (messageType != null)
            msh.MessageType.SetValue(messageType);
        
        if (messageControlId != null)
            msh.MessageControlId.SetValue(messageControlId);
        else
            msh.MessageControlId.SetValue(GenerateMessageControlId());

        return msh;
    }

    /// <summary>
    /// Generates a unique message control ID.
    /// </summary>
    /// <returns>Unique message control ID</returns>
    private static string GenerateMessageControlId()
    {
        return $"MSG{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }

    /// <summary>
    /// Sets the message type using event type and trigger event.
    /// </summary>
    /// <param name="messageCode">Message code (e.g., "ADT")</param>
    /// <param name="triggerEvent">Trigger event (e.g., "A01")</param>
    /// <returns>Result indicating success or failure</returns>
    public Result<MSHSegment> SetMessageType(string messageCode, string triggerEvent)
    {
        var messageType = $"{messageCode}^{triggerEvent}";
        var result = MessageType.SetValue(messageType);
        
        if (result.IsFailure)
            return Error.Validation($"Failed to set message type: {result.Error.Message}", "MessageType");

        return Result<MSHSegment>.Success(this);
    }

    /// <summary>
    /// Sets processing ID with validation.
    /// </summary>
    /// <param name="processingId">Processing ID (P=Production, T=Test, D=Debug)</param>
    /// <returns>Result indicating success or failure</returns>
    public Result<MSHSegment> SetProcessingId(string processingId)
    {
        var validIds = new[] { "P", "T", "D" };
        if (!validIds.Contains(processingId.ToUpper()))
        {
            return Error.Validation($"Invalid processing ID: {processingId}. Valid values are P, T, D", "ProcessingId");
        }

        var result = ProcessingId.SetValue(processingId.ToUpper());
        if (result.IsFailure)
            return Error.Validation($"Failed to set processing ID: {result.Error.Message}", "ProcessingId");

        return Result<MSHSegment>.Success(this);
    }

    /// <summary>
    /// Special serialization for MSH segment (field separator is part of the segment).
    /// </summary>
    /// <returns>HL7 string representation</returns>
    public override string ToHL7String()
    {
        // MSH is special - the field separator comes right after MSH
        var parts = new List<string> { SegmentId + FieldSeparator };
        
        // Add all fields
        foreach (var field in Fields)
        {
            parts.Add(field.ToHL7String());
        }

        // Join with field separator, but first element already has it
        return parts[0] + string.Join(FieldSeparator, parts.Skip(1));
    }


    /// <summary>
    /// Gets message type components.
    /// </summary>
    /// <returns>Tuple of (messageCode, triggerEvent) or null if not set</returns>
    public (string MessageCode, string TriggerEvent)? GetMessageTypeComponents()
    {
        var messageType = MessageType.Value;
        if (string.IsNullOrEmpty(messageType))
            return null;

        var parts = messageType.Split(ComponentSeparator);
        if (parts.Length >= 2)
        {
            return (parts[0], parts[1]);
        }

        return (messageType, string.Empty);
    }

    public override string GetDisplayValue()
    {
        var messageType = MessageType.GetDisplayValue();
        var controlId = MessageControlId.GetDisplayValue();
        var timestamp = DateTimeOfMessage.GetDisplayValue();
        
        return $"MSH: {messageType} [{controlId}] at {timestamp}";
    }
}