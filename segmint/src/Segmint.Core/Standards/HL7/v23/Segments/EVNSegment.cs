// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Types;
using System.Collections.Generic;
using System;
namespace Segmint.Core.Standards.HL7.v23.Segments;

/// <summary>
/// Represents an HL7 Event Type (EVN) segment.
/// This segment identifies the event that triggered the message.
/// </summary>
public class EVNSegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "EVN";

    /// <summary>
    /// Initializes a new instance of the <see cref="EVNSegment"/> class.
    /// </summary>
    public EVNSegment()
    {
    }

    /// <summary>
    /// Gets or sets the event type code (EVN.1) - Required.
    /// </summary>
    public IdentifierField EventTypeCode
    {
        get => this[1] as IdentifierField ?? new IdentifierField(isRequired: true);
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the recorded date/time (EVN.2) - Required.
    /// </summary>
    public TimestampField RecordedDateTime
    {
        get => this[2] as TimestampField ?? new TimestampField(isRequired: true);
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the date/time planned event (EVN.3).
    /// </summary>
    public TimestampField DateTimePlannedEvent
    {
        get => this[3] as TimestampField ?? new TimestampField();
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the event reason code (EVN.4).
    /// </summary>
    public IdentifierField EventReasonCode
    {
        get => this[4] as IdentifierField ?? new IdentifierField();
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the operator ID (EVN.5).
    /// </summary>
    public IdentifierField OperatorId
    {
        get => this[5] as IdentifierField ?? new IdentifierField();
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the event occurred date/time (EVN.6).
    /// </summary>
    public TimestampField EventOccurred
    {
        get => this[6] as TimestampField ?? new TimestampField();
        set => this[6] = value;
    }

    /// <inheritdoc />
    protected override void InitializeFields()
    {
        // EVN.1: Event Type Code (Required)
        AddField(new IdentifierField(isRequired: true));
        
        // EVN.2: Recorded Date/Time (Required)
        AddField(new TimestampField(isRequired: true));
        
        // EVN.3: Date/Time Planned Event
        AddField(new TimestampField());
        
        // EVN.4: Event Reason Code
        AddField(new IdentifierField());
        
        // EVN.5: Operator ID
        AddField(new IdentifierField());
        
        // EVN.6: Event Occurred Date/Time
        AddField(new TimestampField());
    }

    /// <summary>
    /// Sets basic event information.
    /// </summary>
    /// <param name="eventTypeCode">The event type code (e.g., "O01", "A01", "R01").</param>
    /// <param name="recordedDateTime">When the event was recorded.</param>
    /// <param name="eventOccurred">When the event actually occurred.</param>
    /// <param name="operatorId">The ID of the operator who triggered the event.</param>
    /// <param name="eventReasonCode">The reason for the event.</param>
    public void SetBasicInfo(
        string eventTypeCode,
        DateTime? recordedDateTime = null,
        DateTime? eventOccurred = null,
        string? operatorId = null,
        string? eventReasonCode = null)
    {
        EventTypeCode.SetValue(eventTypeCode);
        
        // Set recorded time to now if not provided
        if (recordedDateTime.HasValue)
        {
            RecordedDateTime.SetValue(recordedDateTime.Value.ToString("yyyyMMddHHmmss"));
        }
        else
        {
            RecordedDateTime.SetToNow(includeFraction: true);
        }
        
        if (eventOccurred.HasValue)
        {
            EventOccurred.SetValue(eventOccurred.Value.ToString("yyyyMMddHHmmss"));
        }
        
        if (!string.IsNullOrEmpty(operatorId))
        {
            OperatorId.SetValue(operatorId);
        }
        
        if (!string.IsNullOrEmpty(eventReasonCode))
        {
            EventReasonCode.SetValue(eventReasonCode);
        }
    }

    /// <summary>
    /// Sets the event to have occurred now.
    /// </summary>
    public void SetEventOccurredNow()
    {
        EventOccurred.SetToNow(includeFraction: true);
    }

    /// <summary>
    /// Sets the recorded time to now.
    /// </summary>
    public void SetRecordedNow()
    {
        RecordedDateTime.SetToNow(includeFraction: true);
    }

    /// <summary>
    /// Sets the event type code (EVN.1).
    /// </summary>
    /// <param name="eventTypeCode">The event type code to set.</param>
    public void SetEventTypeCode(string eventTypeCode)
    {
        EventTypeCode.SetValue(eventTypeCode);
    }

    /// <summary>
    /// Sets the recorded date/time (EVN.2).
    /// </summary>
    /// <param name="dateTime">The date/time when the event was recorded.</param>
    public void SetRecordedDateTime(DateTime dateTime)
    {
        RecordedDateTime.SetValue(dateTime.ToString("yyyyMMddHHmmss"));
    }

    /// <inheritdoc />
    public override HL7Segment Clone()
    {
        var clone = new EVNSegment();
        
        // Copy all field values
        for (int i = 1; i <= FieldCount; i++)
        {
            clone[i] = this[i].Clone();
        }
        
        return clone;
    }

    /// <summary>
    /// Creates an EVN segment for RDE (pharmacy order) messages.
    /// </summary>
    /// <param name="operatorId">The operator who created the order.</param>
    /// <param name="eventOccurred">When the order event occurred.</param>
    /// <returns>A configured EVN segment for RDE messages.</returns>
    public static EVNSegment CreateForRDE(string? operatorId = null, DateTime? eventOccurred = null)
    {
        var evn = new EVNSegment();
        evn.SetBasicInfo("O01", eventOccurred: eventOccurred, operatorId: operatorId);
        return evn;
    }

    /// <summary>
    /// Creates an EVN segment for ADT (patient administration) messages.
    /// </summary>
    /// <param name="eventType">The ADT event type (A01, A03, A08, etc.).</param>
    /// <param name="operatorId">The operator who performed the action.</param>
    /// <param name="eventOccurred">When the event occurred.</param>
    /// <returns>A configured EVN segment for ADT messages.</returns>
    public static EVNSegment CreateForADT(string eventType, string? operatorId = null, DateTime? eventOccurred = null)
    {
        var evn = new EVNSegment();
        evn.SetBasicInfo(eventType, eventOccurred: eventOccurred, operatorId: operatorId);
        return evn;
    }

    /// <summary>
    /// Creates an EVN segment for ACK (acknowledgment) messages.
    /// </summary>
    /// <param name="operatorId">The operator who processed the acknowledgment.</param>
    /// <returns>A configured EVN segment for ACK messages.</returns>
    public static EVNSegment CreateForACK(string? operatorId = null)
    {
        var evn = new EVNSegment();
        evn.SetBasicInfo("R01", operatorId: operatorId);
        return evn;
    }

    /// <summary>
    /// Creates an EVN segment for RDS (dispense) messages.
    /// </summary>
    /// <param name="operatorId">The operator who dispensed the medication.</param>
    /// <param name="eventOccurred">When the dispense occurred.</param>
    /// <returns>A configured EVN segment for RDS messages.</returns>
    public static EVNSegment CreateForRDS(string? operatorId = null, DateTime? eventOccurred = null)
    {
        var evn = new EVNSegment();
        evn.SetBasicInfo("O13", eventOccurred: eventOccurred, operatorId: operatorId);
        return evn;
    }
}
