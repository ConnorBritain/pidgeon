// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Application.DTOs;
using Pidgeon.Core.Infrastructure.Standards.Common.HL7;
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

    public override void InitializeFields()
    {
        // ORC.1 - Order Control (Required)
        AddField(new StringField("NW", 2, true)); // NW = New order

        // ORC.2 - Placer Order Number
        AddField(StringField.Optional(22));

        // ORC.3 - Filler Order Number  
        AddField(StringField.Optional(22));

        // ORC.4 - Placer Group Number
        AddField(StringField.Optional(22));

        // ORC.5 - Order Status
        AddField(StringField.Optional(2));

        // ORC.6 - Response Flag
        AddField(StringField.Optional(1));

        // ORC.7 - Quantity/Timing
        AddField(StringField.Optional(200));

        // ORC.8 - Parent
        AddField(StringField.Optional(200));

        // ORC.9 - Date/Time of Transaction
        AddField(new TimestampField(DateTime.UtcNow));

        // ORC.10 - Entered By
        AddField(StringField.Optional(80));

        // ORC.11 - Verified By
        AddField(StringField.Optional(80));

        // ORC.12 - Ordering Provider
        AddField(StringField.Optional(80));
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