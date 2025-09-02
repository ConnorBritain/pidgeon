// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Application.DTOs;
using Pidgeon.Core.Domain.Messaging.HL7v2.Common;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Segments;

/// <summary>
/// RXE - Pharmacy/Treatment Encoded Order Segment.
/// Contains the actual prescription/medication order details.
/// </summary>
public class RXESegment : HL7Segment
{
    public override string SegmentId => "RXE";
    public override string DisplayName => "Pharmacy Encoded Order";

    // Field accessors
    public StringField QuantityTiming => GetField<StringField>(1)!;
    public StringField GiveCode => GetField<StringField>(2)!;
    public NumericField GiveAmountMin => GetField<NumericField>(3)!;
    public NumericField GiveAmountMax => GetField<NumericField>(4)!;
    public StringField GiveUnits => GetField<StringField>(5)!;
    public StringField GiveDosageForm => GetField<StringField>(6)!;
    public StringField ProviderInstructions => GetField<StringField>(7)!;

    public override void InitializeFields()
    {
        // RXE.1 - Quantity/Timing
        AddField(StringField.Optional(200));

        // RXE.2 - Give Code (Required) - Drug identifier
        AddField(StringField.Required(100));

        // RXE.3 - Give Amount - Min
        AddField(new NumericField());

        // RXE.4 - Give Amount - Max  
        AddField(new NumericField());

        // RXE.5 - Give Units
        AddField(StringField.Optional(60));

        // RXE.6 - Give Dosage Form
        AddField(StringField.Optional(60));

        // RXE.7 - Provider's Administration Instructions
        AddField(StringField.Optional(200));

        // RXE.8 - Deliver-to Location
        AddField(StringField.Optional(200));

        // RXE.9 - Substitution Status
        AddField(StringField.Optional(1));

        // RXE.10 - Dispense Amount
        AddField(new NumericField());

        // RXE.11 - Dispense Units
        AddField(StringField.Optional(60));

        // RXE.12 - Number of Refills
        AddField(new NumericField());

        // RXE.13 - Ordering Provider's DEA Number
        AddField(StringField.Optional(60));

        // RXE.14 - Pharmacist/Treatment Supplier's Verifier ID
        AddField(StringField.Optional(60));

        // RXE.15 - Prescription Number
        AddField(StringField.Optional(20));
    }

    /// <summary>
    /// Populates the RXE segment from prescription data.
    /// </summary>
    public Result<RXESegment> PopulateFromPrescription(PrescriptionDto prescription)
    {
        try
        {
            // RXE.1 - Quantity/Timing (SIG)
            if (!string.IsNullOrEmpty(prescription.Instructions))
            {
                SetField(1, prescription.Instructions);
            }

            // RXE.2 - Give Code (Required) - Drug code/name
            var drugCode = prescription.Medication.NdcCode ?? prescription.Medication.GenericName;
            SetField(2, $"{drugCode}^{prescription.Medication.DisplayName}");

            // RXE.3 - Give Amount Min (from dosage instructions)
            if (decimal.TryParse(prescription.Dosage.Dose, out var doseAmount))
            {
                GetField<NumericField>(3)?.SetTypedValue((int)doseAmount);
            }

            // RXE.5 - Give Units
            if (!string.IsNullOrEmpty(prescription.Dosage.DoseUnit))
            {
                SetField(5, prescription.Dosage.DoseUnit);
            }

            // RXE.6 - Give Dosage Form
            var dosageForm = prescription.Medication.DosageForm?.ToString() ?? "";
            if (!string.IsNullOrEmpty(dosageForm))
            {
                SetField(6, dosageForm);
            }

            // RXE.7 - Provider Instructions
            if (!string.IsNullOrEmpty(prescription.Instructions))
            {
                SetField(7, prescription.Instructions);
            }

            // RXE.10 - Dispense Amount (default to 30 if not specified)
            GetField<NumericField>(10)?.SetTypedValue(30);

            // RXE.11 - Dispense Units (default to tablets)
            SetField(11, prescription.Medication.DosageForm?.ToString() ?? "tablets");

            // RXE.12 - Number of Refills (default to 0)
            GetField<NumericField>(12)?.SetTypedValue(0);

            // RXE.13 - DEA Number
            if (prescription.Prescriber?.DeaNumber != null)
            {
                SetField(13, prescription.Prescriber.DeaNumber);
            }

            // RXE.15 - Prescription Number
            SetField(15, prescription.Id);

            return Result<RXESegment>.Success(this);
        }
        catch (Exception ex)
        {
            return Result<RXESegment>.Failure($"Failed to populate RXE segment: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a display representation of the drug information.
    /// </summary>
    public string GetDrugDisplay()
    {
        var drugCode = GiveCode.Value;
        if (string.IsNullOrEmpty(drugCode))
            return "Unknown Drug";

        // Parse drug name from composite field (code^name)
        var parts = drugCode.Split('^');
        var drugName = parts.Length > 1 ? parts[1] : parts[0];
        
        var amount = GiveAmountMin.TypedValue;
        var units = GiveUnits.Value;
        
        if (amount.HasValue && !string.IsNullOrEmpty(units))
            return $"{drugName} {amount}{units}";
        
        return drugName;
    }
    
    /// <summary>
    /// Validates the RXE segment according to HL7 requirements.
    /// </summary>
    public override Result<HL7Segment> Validate()
    {
        var errors = new List<string>();
        
        // RXE.2 (Give Code) is required
        if (GiveCode.IsEmpty)
            errors.Add("RXE.2 Give Code is required");
        
        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            return Result<HL7Segment>.Failure($"RXE segment validation failed: {errorMessage}");
        }
        
        return Result<HL7Segment>.Success(this);
    }
}