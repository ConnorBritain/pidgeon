// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Types;
using System.Collections.Generic;
using System;
namespace Segmint.Core.Standards.HL7.v23.Segments;

/// <summary>
/// Represents an HL7 Message Header (MSH) segment.
/// This segment contains information about the message itself and is required in all HL7 messages.
/// </summary>
public class MSHSegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "MSH";

    /// <summary>
    /// Initializes a new instance of the <see cref="MSHSegment"/> class.
    /// </summary>
    public MSHSegment()
    {
    }

    /// <summary>
    /// Gets or sets the field separator (MSH.1). Default is "|".
    /// </summary>
    public StringField FieldSeparator => this[1] as StringField ?? new StringField("|", isRequired: true);

    /// <summary>
    /// Gets or sets the encoding characters (MSH.2). Default is ^~\&amp;.
    /// </summary>
    public StringField EncodingCharacters => this[2] as StringField ?? new StringField("^~\\&", isRequired: true);

    /// <summary>
    /// Gets or sets the sending application (MSH.3).
    /// </summary>
    public StringField SendingApplication
    {
        get => this[3] as StringField ?? new StringField();
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the sending facility (MSH.4).
    /// </summary>
    public StringField SendingFacility
    {
        get => this[4] as StringField ?? new StringField();
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the receiving application (MSH.5).
    /// </summary>
    public StringField ReceivingApplication
    {
        get => this[5] as StringField ?? new StringField();
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the receiving facility (MSH.6).
    /// </summary>
    public StringField ReceivingFacility
    {
        get => this[6] as StringField ?? new StringField();
        set => this[6] = value;
    }

    /// <summary>
    /// Gets or sets the date/time of message (MSH.7).
    /// </summary>
    public TimestampField DateTimeOfMessage
    {
        get => this[7] as TimestampField ?? new TimestampField(isRequired: true);
        set => this[7] = value;
    }

    /// <summary>
    /// Gets or sets the security (MSH.8).
    /// </summary>
    public StringField Security
    {
        get => this[8] as StringField ?? new StringField();
        set => this[8] = value;
    }

    /// <summary>
    /// Gets or sets the message type (MSH.9).
    /// </summary>
    public CodedElementField MessageType
    {
        get => this[9] as CodedElementField ?? new CodedElementField(isRequired: true);
        set => this[9] = value;
    }

    /// <summary>
    /// Gets the trigger event from the message type (MSH.9.2).
    /// </summary>
    public StringField TriggerEvent
    {
        get
        {
            var triggerEventValue = MessageType.GetComponent(2) ?? "";
            return new StringField(triggerEventValue, isRequired: true);
        }
    }

    /// <summary>
    /// Gets or sets the message control ID (MSH.10).
    /// </summary>
    public StringField MessageControlId
    {
        get => this[10] as StringField ?? new StringField(isRequired: true);
        set => this[10] = value;
    }

    /// <summary>
    /// Gets or sets the processing ID (MSH.11).
    /// </summary>
    public IdentifierField ProcessingId
    {
        get => this[11] as IdentifierField ?? new IdentifierField(isRequired: true);
        set => this[11] = value;
    }

    /// <summary>
    /// Gets or sets the version ID (MSH.12).
    /// </summary>
    public StringField VersionId
    {
        get => this[12] as StringField ?? new StringField("2.3", isRequired: true);
        set => this[12] = value;
    }

    /// <summary>
    /// Gets or sets the sequence number (MSH.13).
    /// </summary>
    public StringField SequenceNumber
    {
        get => this[13] as StringField ?? new StringField();
        set => this[13] = value;
    }

    /// <summary>
    /// Gets or sets the continuation pointer (MSH.14).
    /// </summary>
    public StringField ContinuationPointer
    {
        get => this[14] as StringField ?? new StringField();
        set => this[14] = value;
    }

    /// <summary>
    /// Gets or sets the accept acknowledgment type (MSH.15).
    /// </summary>
    public IdentifierField AcceptAcknowledgmentType
    {
        get => this[15] as IdentifierField ?? new IdentifierField();
        set => this[15] = value;
    }

    /// <summary>
    /// Gets or sets the application acknowledgment type (MSH.16).
    /// </summary>
    public IdentifierField ApplicationAcknowledgmentType
    {
        get => this[16] as IdentifierField ?? new IdentifierField();
        set => this[16] = value;
    }

    /// <summary>
    /// Gets or sets the country code (MSH.17).
    /// </summary>
    public IdentifierField CountryCode
    {
        get => this[17] as IdentifierField ?? new IdentifierField();
        set => this[17] = value;
    }

    /// <summary>
    /// Gets or sets the character set (MSH.18).
    /// </summary>
    public IdentifierField CharacterSet
    {
        get => this[18] as IdentifierField ?? new IdentifierField();
        set => this[18] = value;
    }

    /// <summary>
    /// Gets or sets the principal language of message (MSH.19).
    /// </summary>
    public CodedElementField PrincipalLanguageOfMessage
    {
        get => this[19] as CodedElementField ?? new CodedElementField();
        set => this[19] = value;
    }

    /// <inheritdoc />
    protected override void InitializeFields()
    {
        // MSH.1: Field Separator (always "|")
        AddField(new StringField("|", isRequired: true));
        
        // MSH.2: Encoding Characters (always "^~\&")
        AddField(new StringField("^~\\&", isRequired: true));
        
        // MSH.3: Sending Application
        AddField(new StringField());
        
        // MSH.4: Sending Facility
        AddField(new StringField());
        
        // MSH.5: Receiving Application
        AddField(new StringField());
        
        // MSH.6: Receiving Facility
        AddField(new StringField());
        
        // MSH.7: Date/Time of Message (required)
        AddField(new TimestampField(isRequired: true));
        
        // MSH.8: Security
        AddField(new StringField());
        
        // MSH.9: Message Type (required)
        AddField(new CodedElementField(isRequired: true));
        
        // MSH.10: Message Control ID (required)
        AddField(new StringField(isRequired: true));
        
        // MSH.11: Processing ID (required)
        AddField(new IdentifierField(isRequired: true));
        
        // MSH.12: Version ID (required, default to 2.3)
        AddField(new StringField("2.3", isRequired: true));
        
        // MSH.13: Sequence Number
        AddField(new StringField());
        
        // MSH.14: Continuation Pointer
        AddField(new StringField());
        
        // MSH.15: Accept Acknowledgment Type
        AddField(new IdentifierField());
        
        // MSH.16: Application Acknowledgment Type
        AddField(new IdentifierField());
        
        // MSH.17: Country Code
        AddField(new IdentifierField());
        
        // MSH.18: Character Set
        AddField(new IdentifierField());
        
        // MSH.19: Principal Language of Message
        AddField(new CodedElementField());
    }

    /// <summary>
    /// Sets basic message header information.
    /// </summary>
    /// <param name="sendingApplication">The sending application name.</param>
    /// <param name="sendingFacility">The sending facility name.</param>
    /// <param name="receivingApplication">The receiving application name.</param>
    /// <param name="receivingFacility">The receiving facility name.</param>
    /// <param name="messageType">The message type (e.g., "RDE^O01^RDE_O01").</param>
    /// <param name="messageControlId">The unique message control ID.</param>
    /// <param name="processingId">The processing ID (P=Production, T=Test, D=Debug).</param>
    public void SetBasicInfo(
        string? sendingApplication = null,
        string? sendingFacility = null,
        string? receivingApplication = null,
        string? receivingFacility = null,
        string? messageType = null,
        string? messageControlId = null,
        string? processingId = "P")
    {
        if (!string.IsNullOrEmpty(sendingApplication))
            SendingApplication.SetValue(sendingApplication);
            
        if (!string.IsNullOrEmpty(sendingFacility))
            SendingFacility.SetValue(sendingFacility);
            
        if (!string.IsNullOrEmpty(receivingApplication))
            ReceivingApplication.SetValue(receivingApplication);
            
        if (!string.IsNullOrEmpty(receivingFacility))
            ReceivingFacility.SetValue(receivingFacility);
            
        if (!string.IsNullOrEmpty(messageType))
            MessageType.SetValue(messageType);
            
        if (!string.IsNullOrEmpty(messageControlId))
            MessageControlId.SetValue(messageControlId);
            
        if (!string.IsNullOrEmpty(processingId))
            ProcessingId.SetValue(processingId);
            
        // Set timestamp to now if not already set
        if (DateTimeOfMessage.IsEmpty)
            DateTimeOfMessage.SetToNow(includeFraction: true);
    }

    /// <summary>
    /// Sets the message type using standard HL7 format.
    /// </summary>
    /// <param name="messageCode">The message code (e.g., "RDE", "ADT", "ACK").</param>
    /// <param name="triggerEvent">The trigger event (e.g., "O01", "A01", "R01").</param>
    /// <param name="messageStructure">The message structure (optional, defaults to messageCode_triggerEvent).</param>
    public void SetMessageType(string messageCode, string triggerEvent, string? messageStructure = null)
    {
        messageStructure ??= $"{messageCode}_{triggerEvent}";
        var messageTypeValue = $"{messageCode}^{triggerEvent}^{messageStructure}";
        MessageType.SetValue(messageTypeValue);
    }

    /// <summary>
    /// Generates a unique message control ID based on timestamp and random component.
    /// </summary>
    /// <param name="prefix">Optional prefix for the control ID.</param>
    public void GenerateMessageControlId(string? prefix = null)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var random = Random.Shared.Next(1000, 9999);
        var controlId = prefix != null ? $"{prefix}_{timestamp}_{random}" : $"{timestamp}_{random}";
        MessageControlId.SetValue(controlId);
    }

    /// <summary>
    /// Sets the message control ID to a specific value.
    /// </summary>
    /// <param name="messageControlId">The message control ID to set.</param>
    public void SetMessageControlId(string messageControlId)
    {
        MessageControlId.SetValue(messageControlId);
    }

    /// <summary>
    /// Sets standard processing ID values.
    /// </summary>
    /// <param name="isProduction">True for production (P), false for test (T).</param>
    public void SetProcessingId(bool isProduction = true)
    {
        ProcessingId.SetValue(isProduction ? "P" : "T");
    }

    /// <summary>
    /// Sets standard acknowledgment types.
    /// </summary>
    /// <param name="acceptAck">Accept acknowledgment type (AL=Always, NE=Never, ER=Error/Reject, SU=Success).</param>
    /// <param name="appAck">Application acknowledgment type (AL=Always, NE=Never, ER=Error/Reject, SU=Success).</param>
    public void SetAcknowledgmentTypes(string? acceptAck = null, string? appAck = null)
    {
        if (!string.IsNullOrEmpty(acceptAck))
            AcceptAcknowledgmentType.SetValue(acceptAck);
            
        if (!string.IsNullOrEmpty(appAck))
            ApplicationAcknowledgmentType.SetValue(appAck);
    }

    /// <inheritdoc />
    public override HL7Segment Clone()
    {
        var clone = new MSHSegment();
        
        // Copy all field values
        for (int i = 1; i <= FieldCount; i++)
        {
            clone[i] = this[i].Clone();
        }
        
        return clone;
    }

    /// <summary>
    /// Creates a standard MSH segment for RDE messages.
    /// </summary>
    /// <param name="sendingApp">The sending application.</param>
    /// <param name="sendingFacility">The sending facility.</param>
    /// <param name="receivingApp">The receiving application.</param>
    /// <param name="receivingFacility">The receiving facility.</param>
    /// <param name="isProduction">Whether this is a production message.</param>
    /// <returns>A configured MSH segment for RDE messages.</returns>
    public static MSHSegment CreateForRDE(
        string sendingApp,
        string sendingFacility,
        string receivingApp,
        string receivingFacility,
        bool isProduction = false)
    {
        var msh = new MSHSegment();
        msh.SetBasicInfo(sendingApp, sendingFacility, receivingApp, receivingFacility);
        msh.SetMessageType("RDE", "O01");
        msh.GenerateMessageControlId("RDE");
        msh.SetProcessingId(isProduction);
        msh.SetAcknowledgmentTypes("AL", "NE");
        
        return msh;
    }

    /// <summary>
    /// Creates a standard MSH segment for ADT messages.
    /// </summary>
    /// <param name="sendingApp">The sending application.</param>
    /// <param name="sendingFacility">The sending facility.</param>
    /// <param name="receivingApp">The receiving application.</param>
    /// <param name="receivingFacility">The receiving facility.</param>
    /// <param name="eventType">The ADT event type (A01, A03, A08, etc.).</param>
    /// <param name="isProduction">Whether this is a production message.</param>
    /// <returns>A configured MSH segment for ADT messages.</returns>
    public static MSHSegment CreateForADT(
        string sendingApp,
        string sendingFacility,
        string receivingApp,
        string receivingFacility,
        string eventType = "A01",
        bool isProduction = false)
    {
        var msh = new MSHSegment();
        msh.SetBasicInfo(sendingApp, sendingFacility, receivingApp, receivingFacility);
        msh.SetMessageType("ADT", eventType);
        msh.GenerateMessageControlId("ADT");
        msh.SetProcessingId(isProduction);
        msh.SetAcknowledgmentTypes("AL", "NE");
        
        return msh;
    }

    /// <summary>
    /// Creates a standard MSH segment for ACK messages.
    /// </summary>
    /// <param name="sendingApp">The sending application.</param>
    /// <param name="sendingFacility">The sending facility.</param>
    /// <param name="receivingApp">The receiving application.</param>
    /// <param name="receivingFacility">The receiving facility.</param>
    /// <param name="originalControlId">The message control ID from the original message being acknowledged.</param>
    /// <param name="isProduction">Whether this is a production message.</param>
    /// <returns>A configured MSH segment for ACK messages.</returns>
    public static MSHSegment CreateForACK(
        string sendingApp,
        string sendingFacility,
        string receivingApp,
        string receivingFacility,
        string originalControlId,
        bool isProduction = false)
    {
        var msh = new MSHSegment();
        msh.SetBasicInfo(sendingApp, sendingFacility, receivingApp, receivingFacility);
        msh.SetMessageType("ACK", "R01");
        msh.MessageControlId.SetValue(originalControlId); // ACK uses the original control ID
        msh.SetProcessingId(isProduction);
        msh.SetAcknowledgmentTypes("NE", "NE"); // ACKs typically don't generate further ACKs
        
        return msh;
    }

    /// <summary>
    /// Sets the version ID (MSH.12).
    /// </summary>
    /// <param name="versionId">The HL7 version (e.g., "2.3").</param>
    public void SetVersionId(string versionId)
    {
        VersionId.SetValue(versionId);
    }

    /// <summary>
    /// Sets the trigger event code within the message type.
    /// </summary>
    /// <param name="triggerEvent">The trigger event code (e.g., "O01", "A01").</param>
    public void SetTriggerEvent(string triggerEvent)
    {
        // Update the message type with new trigger event while preserving message code
        var currentMessageType = MessageType.GetPrimaryValue();
        if (!string.IsNullOrEmpty(currentMessageType))
        {
            var parts = currentMessageType.Split('^');
            if (parts.Length > 0)
            {
                SetMessageType(parts[0], triggerEvent);
            }
        }
    }

    /// <summary>
    /// Sets the sending application (MSH.3).
    /// </summary>
    /// <param name="sendingApplication">The sending application name.</param>
    public void SetSendingApplication(string sendingApplication)
    {
        SendingApplication.SetValue(sendingApplication);
    }

    /// <summary>
    /// Sets the sending facility (MSH.4).
    /// </summary>
    /// <param name="sendingFacility">The sending facility name.</param>
    public void SetSendingFacility(string sendingFacility)
    {
        SendingFacility.SetValue(sendingFacility);
    }

    /// <summary>
    /// Sets the receiving application (MSH.5).
    /// </summary>
    /// <param name="receivingApplication">The receiving application name.</param>
    public void SetReceivingApplication(string receivingApplication)
    {
        ReceivingApplication.SetValue(receivingApplication);
    }

    /// <summary>
    /// Sets the receiving facility (MSH.6).
    /// </summary>
    /// <param name="receivingFacility">The receiving facility name.</param>
    public void SetReceivingFacility(string receivingFacility)
    {
        ReceivingFacility.SetValue(receivingFacility);
    }
}
