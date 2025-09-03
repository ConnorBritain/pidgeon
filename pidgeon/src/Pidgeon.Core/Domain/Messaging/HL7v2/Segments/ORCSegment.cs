// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Application.DTOs;
using Pidgeon.Core.Domain.Messaging.HL7v2.Common;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Segments;

/// <summary>
/// ORC - Common Order Segment.
/// Contains common order information used across different message types.
/// </summary>
public class ORCSegment : HL7Segment
{
    public override string SegmentId => "ORC";
    public override string DisplayName => "Common Order";

    /// <summary>
    /// Defines the fields for the ORC segment.
    /// </summary>
    protected override IEnumerable<SegmentFieldDefinition> GetFieldDefinitions()
    {
        return new[]
        {
            SegmentFieldDefinition.StringWithDefault(1, "Order Control", "NW", 2, true),   // ORC.1 (NW = New order)
            SegmentFieldDefinition.OptionalString(2, "Placer Order Number", 22),            // ORC.2
            SegmentFieldDefinition.OptionalString(3, "Filler Order Number", 22),            // ORC.3
            SegmentFieldDefinition.OptionalString(4, "Placer Group Number", 22),            // ORC.4
            SegmentFieldDefinition.OptionalString(5, "Order Status", 2),                    // ORC.5
            SegmentFieldDefinition.OptionalString(6, "Response Flag", 1),                   // ORC.6
            SegmentFieldDefinition.OptionalString(7, "Quantity/Timing", 200),               // ORC.7
            SegmentFieldDefinition.OptionalString(8, "Parent", 200),                        // ORC.8
            SegmentFieldDefinition.TimestampNow(9, "Date/Time of Transaction"),             // ORC.9
            SegmentFieldDefinition.OptionalString(10, "Entered By", 80),                    // ORC.10
            SegmentFieldDefinition.OptionalString(11, "Verified By", 80),                   // ORC.11
            SegmentFieldDefinition.OptionalString(12, "Ordering Provider", 80)              // ORC.12
        };
    }

    /// <summary>
    /// Populates the ORC segment from prescription data.
    /// </summary>
    public Result<ORCSegment> PopulateFromPrescription(PrescriptionDto prescription)
    {
        try
        {
            // ORC.1 - Order Control (NW = New order)
            SetField(1, "NW");

            // ORC.2 - Placer Order Number (prescription ID)
            SetField(2, prescription.Id);

            // ORC.9 - Date/Time of Transaction
            GetField<TimestampField>(9)?.SetTypedValue(prescription.DatePrescribed);

            // ORC.12 - Ordering Provider
            if (prescription.Prescriber != null)
            {
                var providerString = $"{prescription.Prescriber.Name.DisplayName}^{prescription.Prescriber.LicenseNumber}";
                SetField(12, providerString);
            }

            return Result<ORCSegment>.Success(this);
        }
        catch (Exception ex)
        {
            return Result<ORCSegment>.Failure($"Failed to populate ORC segment: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Validates the ORC segment according to HL7 requirements.
    /// </summary>
    public override Result<HL7Segment> Validate()
    {
        var errors = new List<string>();
        
        // ORC.1 (Order Control) is required
        var orderControl = GetField<StringField>(1);
        if (orderControl?.IsEmpty != false)
            errors.Add("ORC.1 Order Control is required");
        
        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            return Result<HL7Segment>.Failure($"ORC segment validation failed: {errorMessage}");
        }
        
        return Result<HL7Segment>.Success(this);
    }
}