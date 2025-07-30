// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.Standards.HL7.v23.Segments;
using System.Collections.Generic;
using System;
namespace Segmint.Core.Standards.HL7.v23.Messages;

/// <summary>
/// Represents an HL7 ACK (General Acknowledgment) message.
/// Used to acknowledge receipt and processing status of other HL7 messages.
/// </summary>
public class ACKMessage : HL7Message
{
    private string _triggerEvent = "R01";

    /// <inheritdoc />
    public override string MessageType => "ACK";

    /// <inheritdoc />
    public override string TriggerEvent => _triggerEvent;

    /// <summary>
    /// Gets or sets the message header segment.
    /// </summary>
    public override MSHSegment? MessageHeader { get; set; }

    /// <summary>
    /// Gets or sets the message acknowledgment segment.
    /// </summary>
    public MSASegment? MessageAcknowledgment { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ACKMessage"/> class.
    /// </summary>
    /// <param name="originalMessage">The original message being acknowledged.</param>
    /// <param name="acknowledgmentCode">The acknowledgment code (AA, AE, AR, CA, CE, CR).</param>
    /// <param name="textMessage">Optional text message.</param>
    public ACKMessage(HL7Message? originalMessage = null, string acknowledgmentCode = "AA", string? textMessage = null)
    {
        InitializeSegments(originalMessage, acknowledgmentCode, textMessage);
    }

    /// <summary>
    /// Initializes the required segments for an ACK message.
    /// </summary>
    /// <param name="originalMessage">The original message being acknowledged.</param>
    /// <param name="acknowledgmentCode">The acknowledgment code.</param>
    /// <param name="textMessage">Optional text message.</param>
    private void InitializeSegments(HL7Message? originalMessage, string acknowledgmentCode, string? textMessage)
    {
        // Create required segments
        MessageHeader = new MSHSegment();
        MessageAcknowledgment = new MSASegment();

        // Set up message header
        MessageHeader.SetMessageType(MessageType, TriggerEvent);
        MessageHeader.SetProcessingId(true); // P = Production
        MessageHeader.SetVersionId("2.3");

        // If we have the original message, extract information from it
        if (originalMessage != null)
        {
            var originalMsh = originalMessage.GetSegment<MSHSegment>();
            if (originalMsh != null)
            {
                // Set trigger event based on original message
                _triggerEvent = originalMsh.TriggerEvent.RawValue;
                MessageHeader.SetTriggerEvent(TriggerEvent);
                
                // Swap sending and receiving applications
                MessageHeader.SetSendingApplication(originalMsh.ReceivingApplication.RawValue);
                MessageHeader.SetSendingFacility(originalMsh.ReceivingFacility.RawValue);
                MessageHeader.SetReceivingApplication(originalMsh.SendingApplication.RawValue);
                MessageHeader.SetReceivingFacility(originalMsh.SendingFacility.RawValue);
                
                // Set up acknowledgment
                MessageAcknowledgment.SetAcknowledgmentCode(acknowledgmentCode);
                MessageAcknowledgment.SetMessageControlId(originalMsh.MessageControlId.RawValue);
                
                if (!string.IsNullOrEmpty(textMessage))
                {
                    MessageAcknowledgment.SetTextMessage(textMessage);
                }
            }
        }
        else
        {
            // Set up basic acknowledgment
            MessageAcknowledgment.SetAcknowledgmentCode(acknowledgmentCode);
            MessageAcknowledgment.SetMessageControlId(Guid.NewGuid().ToString("N")[..20]);
            
            if (!string.IsNullOrEmpty(textMessage))
            {
                MessageAcknowledgment.SetTextMessage(textMessage);
            }
        }

        // Add segments to the message
        AddSegment(MessageHeader);
        AddSegment(MessageAcknowledgment);
    }

    /// <summary>
    /// Creates a successful acknowledgment message.
    /// </summary>
    /// <param name="originalMessage">The original message being acknowledged.</param>
    /// <param name="textMessage">Optional text message.</param>
    /// <returns>A new <see cref="ACKMessage"/> instance.</returns>
    public static ACKMessage CreateSuccessfulAck(HL7Message originalMessage, string? textMessage = null)
    {
        return new ACKMessage(originalMessage, "AA", textMessage);
    }

    /// <summary>
    /// Creates an error acknowledgment message.
    /// </summary>
    /// <param name="originalMessage">The original message being acknowledged.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorCode">The error code (optional).</param>
    /// <returns>A new <see cref="ACKMessage"/> instance.</returns>
    public static ACKMessage CreateErrorAck(HL7Message originalMessage, string errorMessage, string? errorCode = null)
    {
        var ack = new ACKMessage(originalMessage, "AE", errorMessage);
        
        if (!string.IsNullOrEmpty(errorCode))
        {
            ack.MessageAcknowledgment?.SetErrorCondition(errorCode, errorMessage);
        }
        
        return ack;
    }

    /// <summary>
    /// Creates a rejection acknowledgment message.
    /// </summary>
    /// <param name="originalMessage">The original message being acknowledged.</param>
    /// <param name="rejectionMessage">The rejection message.</param>
    /// <param name="errorCode">The error code (optional).</param>
    /// <returns>A new <see cref="ACKMessage"/> instance.</returns>
    public static ACKMessage CreateRejectionAck(HL7Message originalMessage, string rejectionMessage, string? errorCode = null)
    {
        var ack = new ACKMessage(originalMessage, "AR", rejectionMessage);
        
        if (!string.IsNullOrEmpty(errorCode))
        {
            ack.MessageAcknowledgment?.SetErrorCondition(errorCode, rejectionMessage);
        }
        
        return ack;
    }

    /// <summary>
    /// Creates a commit accept acknowledgment message.
    /// </summary>
    /// <param name="originalMessage">The original message being acknowledged.</param>
    /// <param name="textMessage">Optional text message.</param>
    /// <returns>A new <see cref="ACKMessage"/> instance.</returns>
    public static ACKMessage CreateCommitAcceptAck(HL7Message originalMessage, string? textMessage = null)
    {
        return new ACKMessage(originalMessage, "CA", textMessage);
    }

    /// <summary>
    /// Creates a commit error acknowledgment message.
    /// </summary>
    /// <param name="originalMessage">The original message being acknowledged.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorCode">The error code (optional).</param>
    /// <returns>A new <see cref="ACKMessage"/> instance.</returns>
    public static ACKMessage CreateCommitErrorAck(HL7Message originalMessage, string errorMessage, string? errorCode = null)
    {
        var ack = new ACKMessage(originalMessage, "CE", errorMessage);
        
        if (!string.IsNullOrEmpty(errorCode))
        {
            ack.MessageAcknowledgment?.SetErrorCondition(errorCode, errorMessage);
        }
        
        return ack;
    }

    /// <summary>
    /// Creates a commit reject acknowledgment message.
    /// </summary>
    /// <param name="originalMessage">The original message being acknowledged.</param>
    /// <param name="rejectionMessage">The rejection message.</param>
    /// <param name="errorCode">The error code (optional).</param>
    /// <returns>A new <see cref="ACKMessage"/> instance.</returns>
    public static ACKMessage CreateCommitRejectAck(HL7Message originalMessage, string rejectionMessage, string? errorCode = null)
    {
        var ack = new ACKMessage(originalMessage, "CR", rejectionMessage);
        
        if (!string.IsNullOrEmpty(errorCode))
        {
            ack.MessageAcknowledgment?.SetErrorCondition(errorCode, rejectionMessage);
        }
        
        return ack;
    }

    /// <summary>
    /// Sets the acknowledgment code.
    /// </summary>
    /// <param name="acknowledgmentCode">The acknowledgment code (AA, AE, AR, CA, CE, CR).</param>
    public void SetAcknowledgmentCode(string acknowledgmentCode)
    {
        if (MessageAcknowledgment == null)
            throw new InvalidOperationException("Message acknowledgment segment is not initialized.");

        MessageAcknowledgment.SetAcknowledgmentCode(acknowledgmentCode);
    }

    /// <summary>
    /// Sets the text message.
    /// </summary>
    /// <param name="textMessage">The text message.</param>
    public void SetTextMessage(string textMessage)
    {
        if (MessageAcknowledgment == null)
            throw new InvalidOperationException("Message acknowledgment segment is not initialized.");

        MessageAcknowledgment.SetTextMessage(textMessage);
    }

    /// <summary>
    /// Sets the error condition.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorDescription">The error description.</param>
    /// <param name="codingSystem">The coding system (optional).</param>
    public void SetErrorCondition(string errorCode, string? errorDescription = null, string? codingSystem = null)
    {
        if (MessageAcknowledgment == null)
            throw new InvalidOperationException("Message acknowledgment segment is not initialized.");

        MessageAcknowledgment.SetErrorCondition(errorCode, errorDescription, codingSystem);
    }

    /// <summary>
    /// Sets the sending application information.
    /// </summary>
    /// <param name="sendingApplication">The sending application name.</param>
    /// <param name="sendingFacility">The sending facility name.</param>
    public void SetSendingApplication(string sendingApplication, string? sendingFacility = null)
    {
        if (MessageHeader == null)
            throw new InvalidOperationException("Message header segment is not initialized.");

        MessageHeader.SetSendingApplication(sendingApplication);
        
        if (!string.IsNullOrEmpty(sendingFacility))
        {
            MessageHeader.SetSendingFacility(sendingFacility);
        }
    }

    /// <summary>
    /// Sets the receiving application information.
    /// </summary>
    /// <param name="receivingApplication">The receiving application name.</param>
    /// <param name="receivingFacility">The receiving facility name.</param>
    public void SetReceivingApplication(string receivingApplication, string? receivingFacility = null)
    {
        if (MessageHeader == null)
            throw new InvalidOperationException("Message header segment is not initialized.");

        MessageHeader.SetReceivingApplication(receivingApplication);
        
        if (!string.IsNullOrEmpty(receivingFacility))
        {
            MessageHeader.SetReceivingFacility(receivingFacility);
        }
    }

    /// <summary>
    /// Gets a value indicating whether this acknowledgment indicates success.
    /// </summary>
    /// <returns>True if the acknowledgment indicates success, false otherwise.</returns>
    public bool IsSuccessful()
    {
        return MessageAcknowledgment?.IsSuccessful() ?? false;
    }

    /// <summary>
    /// Gets the acknowledgment code description.
    /// </summary>
    /// <returns>A human-readable description of the acknowledgment code.</returns>
    public string GetAcknowledgmentCodeDescription()
    {
        return MessageAcknowledgment?.GetAcknowledgmentCodeDescription() ?? "Unknown";
    }

    /// <summary>
    /// Gets the acknowledged message control ID.
    /// </summary>
    /// <returns>The message control ID that was acknowledged.</returns>
    public string GetAcknowledgedMessageControlId()
    {
        return MessageAcknowledgment?.MessageControlId.RawValue ?? "Unknown";
    }

    /// <summary>
    /// Gets the text message.
    /// </summary>
    /// <returns>The text message or null if not set.</returns>
    public string? GetTextMessage()
    {
        return MessageAcknowledgment?.TextMessage.RawValue;
    }

    /// <summary>
    /// Gets a formatted display string for this message.
    /// </summary>
    /// <returns>A human-readable representation of the message.</returns>
    public string ToDisplayString()
    {
        var acknowledgmentDescription = GetAcknowledgmentCodeDescription();
        var controlId = GetAcknowledgedMessageControlId();
        var textMessage = GetTextMessage();
        
        var display = $"ACK {acknowledgmentDescription} - Control ID: {controlId}";
        
        if (!string.IsNullOrEmpty(textMessage))
        {
            display += $" - {textMessage}";
        }
        
        return display;
    }

    /// <inheritdoc />
    protected override void InitializeMessage()
    {
        InitializeSegments(null, "AA", null);
    }

    /// <summary>
    /// Creates a copy of this message.
    /// </summary>
    /// <returns>A new instance with the same segment values.</returns>
    public override HL7Message Clone()
    {
        var cloned = new ACKMessage();
        
        // Clear default segments
        cloned.ClearSegments();
        
        // Clone all segments
        foreach (var segment in this)
        {
            cloned.AddSegment(segment.Clone());
        }
        
        // Update references
        cloned.MessageHeader = cloned.GetSegment<MSHSegment>();
        cloned.MessageAcknowledgment = cloned.GetSegment<MSASegment>();
        cloned._triggerEvent = TriggerEvent;
        
        return cloned;
    }
}
