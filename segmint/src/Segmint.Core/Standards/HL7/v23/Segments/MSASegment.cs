// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Types;
using System.Collections.Generic;
namespace Segmint.Core.Standards.HL7.v23.Segments;

/// <summary>
/// Represents the HL7 MSA (Message Acknowledgment) segment.
/// Contains acknowledgment information for received messages.
/// </summary>
public class MSASegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "MSA";

    /// <summary>
    /// Initializes a new instance of the <see cref="MSASegment"/> class.
    /// </summary>
    public MSASegment()
    {
        InitializeFields();
    }

    /// <summary>
    /// Initializes the segment fields according to HL7 v2.3 specification.
    /// </summary>
    protected override void InitializeFields()
    {
        // MSA.1 - Acknowledgment Code
        AddField(new CodedValueField(isRequired: true));
        
        // MSA.2 - Message Control ID
        AddField(new StringField(maxLength: 20, isRequired: true));
        
        // MSA.3 - Text Message
        AddField(new StringField(maxLength: 80, isRequired: false));
        
        // MSA.4 - Expected Sequence Number
        AddField(new NumericField(isRequired: false));
        
        // MSA.5 - Delayed Acknowledgment Type
        AddField(new CodedValueField(isRequired: false));
        
        // MSA.6 - Error Condition
        AddField(new CodedElementField(isRequired: false));
    }

    /// <summary>
    /// Gets or sets the acknowledgment code.
    /// </summary>
    public CodedValueField AcknowledgmentCode
    {
        get => (CodedValueField)this[1];
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the message control ID.
    /// </summary>
    public StringField MessageControlId
    {
        get => (StringField)this[2];
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the text message.
    /// </summary>
    public StringField TextMessage
    {
        get => (StringField)this[3];
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the expected sequence number.
    /// </summary>
    public NumericField ExpectedSequenceNumber
    {
        get => (NumericField)this[4];
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the delayed acknowledgment type.
    /// </summary>
    public CodedValueField DelayedAcknowledgmentType
    {
        get => (CodedValueField)this[5];
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the error condition.
    /// </summary>
    public CodedElementField ErrorCondition
    {
        get => (CodedElementField)this[6];
        set => this[6] = value;
    }

    /// <summary>
    /// Creates a successful acknowledgment.
    /// </summary>
    /// <param name="messageControlId">The message control ID being acknowledged.</param>
    /// <param name="textMessage">Optional text message.</param>
    /// <returns>A new <see cref="MSASegment"/> instance.</returns>
    public static MSASegment CreateSuccessfulAck(string messageControlId, string? textMessage = null)
    {
        var segment = new MSASegment();
        segment.SetAcknowledgmentCode("AA"); // Application Accept
        segment.SetMessageControlId(messageControlId);
        
        if (!string.IsNullOrEmpty(textMessage))
        {
            segment.SetTextMessage(textMessage);
        }
        
        return segment;
    }

    /// <summary>
    /// Creates an error acknowledgment.
    /// </summary>
    /// <param name="messageControlId">The message control ID being acknowledged.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorCode">The error code (optional).</param>
    /// <returns>A new <see cref="MSASegment"/> instance.</returns>
    public static MSASegment CreateErrorAck(string messageControlId, string errorMessage, string? errorCode = null)
    {
        var segment = new MSASegment();
        segment.SetAcknowledgmentCode("AE"); // Application Error
        segment.SetMessageControlId(messageControlId);
        segment.SetTextMessage(errorMessage);
        
        if (!string.IsNullOrEmpty(errorCode))
        {
            segment.SetErrorCondition(errorCode, errorMessage);
        }
        
        return segment;
    }

    /// <summary>
    /// Creates a rejection acknowledgment.
    /// </summary>
    /// <param name="messageControlId">The message control ID being acknowledged.</param>
    /// <param name="rejectionMessage">The rejection message.</param>
    /// <param name="errorCode">The error code (optional).</param>
    /// <returns>A new <see cref="MSASegment"/> instance.</returns>
    public static MSASegment CreateRejectionAck(string messageControlId, string rejectionMessage, string? errorCode = null)
    {
        var segment = new MSASegment();
        segment.SetAcknowledgmentCode("AR"); // Application Reject
        segment.SetMessageControlId(messageControlId);
        segment.SetTextMessage(rejectionMessage);
        
        if (!string.IsNullOrEmpty(errorCode))
        {
            segment.SetErrorCondition(errorCode, rejectionMessage);
        }
        
        return segment;
    }

    /// <summary>
    /// Creates a commit accept acknowledgment.
    /// </summary>
    /// <param name="messageControlId">The message control ID being acknowledged.</param>
    /// <param name="textMessage">Optional text message.</param>
    /// <returns>A new <see cref="MSASegment"/> instance.</returns>
    public static MSASegment CreateCommitAcceptAck(string messageControlId, string? textMessage = null)
    {
        var segment = new MSASegment();
        segment.SetAcknowledgmentCode("CA"); // Commit Accept
        segment.SetMessageControlId(messageControlId);
        
        if (!string.IsNullOrEmpty(textMessage))
        {
            segment.SetTextMessage(textMessage);
        }
        
        return segment;
    }

    /// <summary>
    /// Creates a commit error acknowledgment.
    /// </summary>
    /// <param name="messageControlId">The message control ID being acknowledged.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorCode">The error code (optional).</param>
    /// <returns>A new <see cref="MSASegment"/> instance.</returns>
    public static MSASegment CreateCommitErrorAck(string messageControlId, string errorMessage, string? errorCode = null)
    {
        var segment = new MSASegment();
        segment.SetAcknowledgmentCode("CE"); // Commit Error
        segment.SetMessageControlId(messageControlId);
        segment.SetTextMessage(errorMessage);
        
        if (!string.IsNullOrEmpty(errorCode))
        {
            segment.SetErrorCondition(errorCode, errorMessage);
        }
        
        return segment;
    }

    /// <summary>
    /// Creates a commit reject acknowledgment.
    /// </summary>
    /// <param name="messageControlId">The message control ID being acknowledged.</param>
    /// <param name="rejectionMessage">The rejection message.</param>
    /// <param name="errorCode">The error code (optional).</param>
    /// <returns>A new <see cref="MSASegment"/> instance.</returns>
    public static MSASegment CreateCommitRejectAck(string messageControlId, string rejectionMessage, string? errorCode = null)
    {
        var segment = new MSASegment();
        segment.SetAcknowledgmentCode("CR"); // Commit Reject
        segment.SetMessageControlId(messageControlId);
        segment.SetTextMessage(rejectionMessage);
        
        if (!string.IsNullOrEmpty(errorCode))
        {
            segment.SetErrorCondition(errorCode, rejectionMessage);
        }
        
        return segment;
    }

    /// <summary>
    /// Sets the acknowledgment code.
    /// </summary>
    /// <param name="code">The acknowledgment code (AA, AE, AR, CA, CE, CR).</param>
    public void SetAcknowledgmentCode(string code)
    {
        var allowedCodes = new[] { "AA", "AE", "AR", "CA", "CE", "CR" };
        var codedValue = new CodedValueField(code, allowedCodes, isRequired: true);
        AcknowledgmentCode = codedValue;
    }

    /// <summary>
    /// Sets the message control ID.
    /// </summary>
    /// <param name="messageControlId">The message control ID.</param>
    public void SetMessageControlId(string messageControlId)
    {
        MessageControlId.SetValue(messageControlId);
    }

    /// <summary>
    /// Sets the text message.
    /// </summary>
    /// <param name="textMessage">The text message.</param>
    public void SetTextMessage(string textMessage)
    {
        TextMessage.SetValue(textMessage);
    }

    /// <summary>
    /// Sets the error condition.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorDescription">The error description.</param>
    /// <param name="codingSystem">The coding system (optional).</param>
    public void SetErrorCondition(string errorCode, string? errorDescription = null, string? codingSystem = null)
    {
        ErrorCondition = CodedElementField.Create(errorCode, errorDescription, codingSystem);
    }

    /// <summary>
    /// Gets the acknowledgment code description.
    /// </summary>
    /// <returns>A human-readable description of the acknowledgment code.</returns>
    public string GetAcknowledgmentCodeDescription()
    {
        return AcknowledgmentCode.RawValue switch
        {
            "AA" => "Application Accept",
            "AE" => "Application Error",
            "AR" => "Application Reject",
            "CA" => "Commit Accept",
            "CE" => "Commit Error",
            "CR" => "Commit Reject",
            _ => AcknowledgmentCode.RawValue
        };
    }

    /// <summary>
    /// Gets a value indicating whether this acknowledgment indicates success.
    /// </summary>
    /// <returns>True if the acknowledgment indicates success, false otherwise.</returns>
    public bool IsSuccessful()
    {
        return AcknowledgmentCode.RawValue switch
        {
            "AA" or "CA" => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets a formatted display string for this acknowledgment.
    /// </summary>
    /// <returns>A human-readable representation of the acknowledgment.</returns>
    public string ToDisplayString()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(AcknowledgmentCode.RawValue))
        {
            parts.Add($"[{GetAcknowledgmentCodeDescription()}]");
        }
        
        if (!string.IsNullOrEmpty(MessageControlId.RawValue))
        {
            parts.Add($"Control ID: {MessageControlId.RawValue}");
        }
        
        if (!string.IsNullOrEmpty(TextMessage.RawValue))
        {
            parts.Add($"Message: {TextMessage.RawValue}");
        }
        
        if (!string.IsNullOrEmpty(ErrorCondition.RawValue))
        {
            parts.Add($"Error: {ErrorCondition.ToDisplayString()}");
        }
        
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Creates a copy of this segment.
    /// </summary>
    /// <returns>A new instance with the same field values.</returns>
    public override HL7Segment Clone()
    {
        var cloned = new MSASegment();
        for (var i = 1; i <= FieldCount; i++)
        {
            cloned[i] = this[i].Clone();
        }
        return cloned;
    }
}
