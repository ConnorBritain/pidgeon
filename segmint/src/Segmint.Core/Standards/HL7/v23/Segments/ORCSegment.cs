// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Types;
using System.Collections.Generic;
using System;
namespace Segmint.Core.Standards.HL7.v23.Segments;

/// <summary>
/// Represents an HL7 Common Order (ORC) segment.
/// This segment contains information common to all orders.
/// </summary>
public class ORCSegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "ORC";

    /// <summary>
    /// Initializes a new instance of the <see cref="ORCSegment"/> class.
    /// </summary>
    public ORCSegment()
    {
    }

    /// <summary>
    /// Gets or sets the order control (ORC.1) - Required.
    /// </summary>
    public IdentifierField OrderControl
    {
        get => this[1] as IdentifierField ?? new IdentifierField(isRequired: true);
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the placer order number (ORC.2).
    /// </summary>
    public IdentifierField PlacerOrderNumber
    {
        get => this[2] as IdentifierField ?? new IdentifierField();
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the filler order number (ORC.3).
    /// </summary>
    public IdentifierField FillerOrderNumber
    {
        get => this[3] as IdentifierField ?? new IdentifierField();
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the placer group number (ORC.4).
    /// </summary>
    public IdentifierField PlacerGroupNumber
    {
        get => this[4] as IdentifierField ?? new IdentifierField();
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the order status (ORC.5).
    /// </summary>
    public IdentifierField OrderStatus
    {
        get => this[5] as IdentifierField ?? new IdentifierField();
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the response flag (ORC.6).
    /// </summary>
    public IdentifierField ResponseFlag
    {
        get => this[6] as IdentifierField ?? new IdentifierField();
        set => this[6] = value;
    }

    /// <summary>
    /// Gets or sets the quantity/timing (ORC.7).
    /// </summary>
    public StringField QuantityTiming
    {
        get => this[7] as StringField ?? new StringField();
        set => this[7] = value;
    }

    /// <summary>
    /// Gets or sets the parent order (ORC.8).
    /// </summary>
    public StringField ParentOrder
    {
        get => this[8] as StringField ?? new StringField();
        set => this[8] = value;
    }

    /// <summary>
    /// Gets or sets the date/time of transaction (ORC.9).
    /// </summary>
    public TimestampField DateTimeOfTransaction
    {
        get => this[9] as TimestampField ?? new TimestampField();
        set => this[9] = value;
    }

    /// <summary>
    /// Gets or sets the entered by (ORC.10).
    /// </summary>
    public IdentifierField EnteredBy
    {
        get => this[10] as IdentifierField ?? new IdentifierField();
        set => this[10] = value;
    }

    /// <summary>
    /// Gets or sets the verified by (ORC.11).
    /// </summary>
    public IdentifierField VerifiedBy
    {
        get => this[11] as IdentifierField ?? new IdentifierField();
        set => this[11] = value;
    }

    /// <summary>
    /// Gets or sets the ordering provider (ORC.12).
    /// </summary>
    public IdentifierField OrderingProvider
    {
        get => this[12] as IdentifierField ?? new IdentifierField();
        set => this[12] = value;
    }

    /// <summary>
    /// Gets or sets the enterer's location (ORC.13).
    /// </summary>
    public StringField EnterersLocation
    {
        get => this[13] as StringField ?? new StringField();
        set => this[13] = value;
    }

    /// <summary>
    /// Gets or sets the call back phone number (ORC.14).
    /// </summary>
    public StringField CallBackPhoneNumber
    {
        get => this[14] as StringField ?? new StringField();
        set => this[14] = value;
    }

    /// <summary>
    /// Gets or sets the order effective date/time (ORC.15).
    /// </summary>
    public TimestampField OrderEffectiveDateTime
    {
        get => this[15] as TimestampField ?? new TimestampField();
        set => this[15] = value;
    }

    /// <summary>
    /// Gets or sets the order control code reason (ORC.16).
    /// </summary>
    public CodedElementField OrderControlCodeReason
    {
        get => this[16] as CodedElementField ?? new CodedElementField();
        set => this[16] = value;
    }

    /// <summary>
    /// Gets or sets the entering organization (ORC.17).
    /// </summary>
    public CodedElementField EnteringOrganization
    {
        get => this[17] as CodedElementField ?? new CodedElementField();
        set => this[17] = value;
    }

    /// <summary>
    /// Gets or sets the entering device (ORC.18).
    /// </summary>
    public CodedElementField EnteringDevice
    {
        get => this[18] as CodedElementField ?? new CodedElementField();
        set => this[18] = value;
    }

    /// <summary>
    /// Gets or sets the action by (ORC.19).
    /// </summary>
    public IdentifierField ActionBy
    {
        get => this[19] as IdentifierField ?? new IdentifierField();
        set => this[19] = value;
    }

    /// <inheritdoc />
    protected override void InitializeFields()
    {
        // ORC.1: Order Control (Required)
        AddField(new IdentifierField(isRequired: true));
        
        // ORC.2: Placer Order Number
        AddField(new IdentifierField());
        
        // ORC.3: Filler Order Number
        AddField(new IdentifierField());
        
        // ORC.4: Placer Group Number
        AddField(new IdentifierField());
        
        // ORC.5: Order Status
        AddField(new IdentifierField());
        
        // ORC.6: Response Flag
        AddField(new IdentifierField());
        
        // ORC.7: Quantity/Timing
        AddField(new StringField());
        
        // ORC.8: Parent Order
        AddField(new StringField());
        
        // ORC.9: Date/Time of Transaction
        AddField(new TimestampField());
        
        // ORC.10: Entered By
        AddField(new IdentifierField());
        
        // ORC.11: Verified By
        AddField(new IdentifierField());
        
        // ORC.12: Ordering Provider
        AddField(new IdentifierField());
        
        // ORC.13: Enterer's Location
        AddField(new StringField());
        
        // ORC.14: Call Back Phone Number
        AddField(new StringField());
        
        // ORC.15: Order Effective Date/Time
        AddField(new TimestampField());
        
        // ORC.16: Order Control Code Reason
        AddField(new CodedElementField());
        
        // ORC.17: Entering Organization
        AddField(new CodedElementField());
        
        // ORC.18: Entering Device
        AddField(new CodedElementField());
        
        // ORC.19: Action By
        AddField(new IdentifierField());
    }

    /// <summary>
    /// Sets basic order information.
    /// </summary>
    /// <param name="orderControl">The order control code (NW=New, CA=Cancel, DC=Discontinue, etc.).</param>
    /// <param name="placerOrderNumber">The placer order number.</param>
    /// <param name="fillerOrderNumber">The filler order number.</param>
    /// <param name="orderStatus">The order status (A=Active, C=Cancelled, etc.).</param>
    /// <param name="orderingProvider">The ordering provider ID.</param>
    /// <param name="enteredBy">The user who entered the order.</param>
    public void SetBasicInfo(
        string orderControl,
        string? placerOrderNumber = null,
        string? fillerOrderNumber = null,
        string? orderStatus = null,
        string? orderingProvider = null,
        string? enteredBy = null)
    {
        OrderControl.SetValue(orderControl);
        
        if (!string.IsNullOrEmpty(placerOrderNumber))
            PlacerOrderNumber.SetValue(placerOrderNumber);
            
        if (!string.IsNullOrEmpty(fillerOrderNumber))
            FillerOrderNumber.SetValue(fillerOrderNumber);
            
        if (!string.IsNullOrEmpty(orderStatus))
            OrderStatus.SetValue(orderStatus);
            
        if (!string.IsNullOrEmpty(orderingProvider))
            OrderingProvider.SetValue(orderingProvider);
            
        if (!string.IsNullOrEmpty(enteredBy))
            EnteredBy.SetValue(enteredBy);
            
        // Set transaction time to now
        DateTimeOfTransaction.SetToNow(includeFraction: true);
    }

    /// <summary>
    /// Sets timing and scheduling information.
    /// </summary>
    /// <param name="orderEffectiveDateTime">When the order becomes effective.</param>
    /// <param name="quantityTiming">Quantity and timing instructions.</param>
    public void SetTiming(DateTime? orderEffectiveDateTime = null, string? quantityTiming = null)
    {
        if (orderEffectiveDateTime.HasValue)
            OrderEffectiveDateTime.SetValue(orderEffectiveDateTime.Value.ToString("yyyyMMddHHmmss"));
            
        if (!string.IsNullOrEmpty(quantityTiming))
            QuantityTiming.SetValue(quantityTiming);
    }

    /// <summary>
    /// Sets provider and location information.
    /// </summary>
    /// <param name="orderingProvider">The ordering provider.</param>
    /// <param name="enterersLocation">The location where order was entered.</param>
    /// <param name="callBackPhone">Callback phone number.</param>
    /// <param name="verifiedBy">The user who verified the order.</param>
    public void SetProviderInfo(
        string? orderingProvider = null,
        string? enterersLocation = null,
        string? callBackPhone = null,
        string? verifiedBy = null)
    {
        if (!string.IsNullOrEmpty(orderingProvider))
            OrderingProvider.SetValue(orderingProvider);
            
        if (!string.IsNullOrEmpty(enterersLocation))
            EnterersLocation.SetValue(enterersLocation);
            
        if (!string.IsNullOrEmpty(callBackPhone))
            CallBackPhoneNumber.SetValue(callBackPhone);
            
        if (!string.IsNullOrEmpty(verifiedBy))
            VerifiedBy.SetValue(verifiedBy);
    }

    /// <summary>
    /// Generates a unique placer order number.
    /// </summary>
    /// <param name="prefix">Optional prefix for the order number.</param>
    public void GeneratePlacerOrderNumber(string? prefix = null)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var random = Random.Shared.Next(1000, 9999);
        var orderNumber = prefix != null ? $"{prefix}_{timestamp}_{random}" : $"ORD_{timestamp}_{random}";
        PlacerOrderNumber.SetValue(orderNumber);
    }

    /// <inheritdoc />
    public override HL7Segment Clone()
    {
        var clone = new ORCSegment();
        
        // Copy all field values
        for (int i = 1; i <= FieldCount; i++)
        {
            clone[i] = this[i].Clone();
        }
        
        return clone;
    }

    /// <summary>
    /// Creates an ORC segment for a new pharmacy order.
    /// </summary>
    /// <param name="placerOrderNumber">The placer order number.</param>
    /// <param name="orderingProvider">The ordering provider ID.</param>
    /// <param name="enteredBy">The user who entered the order.</param>
    /// <param name="orderEffectiveDateTime">When the order becomes effective.</param>
    /// <returns>A configured ORC segment for new orders.</returns>
    public static ORCSegment CreateForNewOrder(
        string? placerOrderNumber = null,
        string? orderingProvider = null,
        string? enteredBy = null,
        DateTime? orderEffectiveDateTime = null)
    {
        var orc = new ORCSegment();
        orc.SetBasicInfo("NW", placerOrderNumber, orderStatus: "A", orderingProvider: orderingProvider, enteredBy: enteredBy);
        
        if (string.IsNullOrEmpty(placerOrderNumber))
            orc.GeneratePlacerOrderNumber();
            
        if (orderEffectiveDateTime.HasValue)
            orc.SetTiming(orderEffectiveDateTime);
            
        return orc;
    }

    /// <summary>
    /// Creates an ORC segment for cancelling an order.
    /// </summary>
    /// <param name="placerOrderNumber">The placer order number to cancel.</param>
    /// <param name="fillerOrderNumber">The filler order number to cancel.</param>
    /// <param name="orderingProvider">The ordering provider ID.</param>
    /// <param name="actionBy">The user who cancelled the order.</param>
    /// <param name="reasonCode">The reason for cancellation.</param>
    /// <returns>A configured ORC segment for order cancellation.</returns>
    public static ORCSegment CreateForCancelOrder(
        string placerOrderNumber,
        string? fillerOrderNumber = null,
        string? orderingProvider = null,
        string? actionBy = null,
        string? reasonCode = null)
    {
        var orc = new ORCSegment();
        orc.SetBasicInfo("CA", placerOrderNumber, fillerOrderNumber, "CA", orderingProvider);
        
        if (!string.IsNullOrEmpty(actionBy))
            orc.ActionBy.SetValue(actionBy);
            
        if (!string.IsNullOrEmpty(reasonCode))
            orc.OrderControlCodeReason.SetValue(reasonCode);
            
        return orc;
    }

    /// <summary>
    /// Creates an ORC segment for discontinuing an order.
    /// </summary>
    /// <param name="placerOrderNumber">The placer order number to discontinue.</param>
    /// <param name="fillerOrderNumber">The filler order number to discontinue.</param>
    /// <param name="orderingProvider">The ordering provider ID.</param>
    /// <param name="actionBy">The user who discontinued the order.</param>
    /// <param name="reasonCode">The reason for discontinuation.</param>
    /// <returns>A configured ORC segment for order discontinuation.</returns>
    public static ORCSegment CreateForDiscontinueOrder(
        string placerOrderNumber,
        string? fillerOrderNumber = null,
        string? orderingProvider = null,
        string? actionBy = null,
        string? reasonCode = null)
    {
        var orc = new ORCSegment();
        orc.SetBasicInfo("DC", placerOrderNumber, fillerOrderNumber, "DC", orderingProvider);
        
        if (!string.IsNullOrEmpty(actionBy))
            orc.ActionBy.SetValue(actionBy);
            
        if (!string.IsNullOrEmpty(reasonCode))
            orc.OrderControlCodeReason.SetValue(reasonCode);
            
        return orc;
    }
}
