// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Types;
using System.Collections.Generic;
using System;
namespace Segmint.Core.Standards.HL7.v23.Segments;

/// <summary>
/// Wrapper for GiveAmountMinimum to provide Quantity and Units properties.
/// </summary>
public class GiveAmountWrapper
{
    private readonly CodedElementField _field;

    public GiveAmountWrapper(CodedElementField field)
    {
        _field = field;
    }

    /// <summary>
    /// Gets or sets the quantity value.
    /// </summary>
    public string Quantity
    {
        get => _field.Identifier;
        set => _field.Identifier = value;
    }

    /// <summary>
    /// Gets or sets the units value.
    /// </summary>
    public string Units
    {
        get => _field.Text;
        set => _field.Text = value;
    }

    /// <summary>
    /// Gets the underlying CodedElementField.
    /// </summary>
    public CodedElementField Field => _field;

    public static implicit operator CodedElementField(GiveAmountWrapper wrapper) => wrapper._field;
    public static implicit operator GiveAmountWrapper(CodedElementField field) => new(field);
}

/// <summary>
/// Represents an HL7 Pharmacy/Treatment Encoded Order (RXE) segment.
/// This segment contains detailed pharmacy order information.
/// </summary>
public class RXESegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "RXE";

    /// <summary>
    /// Initializes a new instance of the <see cref="RXESegment"/> class.
    /// </summary>
    public RXESegment()
    {
    }

    /// <summary>
    /// Gets or sets the quantity/timing (RXE.1) - Required.
    /// </summary>
    public TimingQuantityField QuantityTiming
    {
        get => this[1] as TimingQuantityField ?? new TimingQuantityField(isRequired: true);
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the give code (RXE.2) - Required.
    /// </summary>
    public CodedElementField GiveCode
    {
        get => this[2] as CodedElementField ?? new CodedElementField(value: null, isRequired: true);
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the give amount minimum (RXE.3) - Required.
    /// </summary>
    public GiveAmountWrapper GiveAmountMinimum
    {
        get => new GiveAmountWrapper(this[3] as CodedElementField ?? new CodedElementField(isRequired: true));
        set => this[3] = value.Field;
    }

    /// <summary>
    /// Gets or sets the give amount maximum (RXE.4).
    /// </summary>
    public NumericField GiveAmountMaximum
    {
        get => this[4] as NumericField ?? new NumericField();
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the give units (RXE.5) - Required.
    /// </summary>
    public CodedElementField GiveUnits
    {
        get => this[5] as CodedElementField ?? new CodedElementField(value: null, isRequired: true);
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the give dosage form (RXE.6).
    /// </summary>
    public CodedElementField GiveDosageForm
    {
        get => this[6] as CodedElementField ?? new CodedElementField();
        set => this[6] = value;
    }

    /// <summary>
    /// Gets or sets the provider's administration instructions (RXE.7).
    /// </summary>
    public StringField ProvidersAdministrationInstructions
    {
        get => this[7] as StringField ?? new StringField();
        set => this[7] = value;
    }

    /// <summary>
    /// Gets or sets the deliver-to location (RXE.8).
    /// </summary>
    public StringField DeliverToLocation
    {
        get => this[8] as StringField ?? new StringField();
        set => this[8] = value;
    }

    /// <summary>
    /// Gets or sets the substitution status (RXE.9).
    /// </summary>
    public IdentifierField SubstitutionStatus
    {
        get => this[9] as IdentifierField ?? new IdentifierField();
        set => this[9] = value;
    }

    /// <summary>
    /// Gets or sets the dispense amount (RXE.10).
    /// </summary>
    public NumericField DispenseAmount
    {
        get => this[10] as NumericField ?? new NumericField();
        set => this[10] = value;
    }

    /// <summary>
    /// Gets or sets the dispense units (RXE.11).
    /// </summary>
    public CodedElementField DispenseUnits
    {
        get => this[11] as CodedElementField ?? new CodedElementField();
        set => this[11] = value;
    }

    /// <summary>
    /// Gets or sets the number of refills (RXE.12).
    /// </summary>
    public NumericField NumberOfRefills
    {
        get => this[12] as NumericField ?? new NumericField();
        set => this[12] = value;
    }

    /// <summary>
    /// Gets or sets the ordering provider's DEA number (RXE.13).
    /// </summary>
    public IdentifierField OrderingProvidersDEANumber
    {
        get => this[13] as IdentifierField ?? new IdentifierField();
        set => this[13] = value;
    }

    /// <summary>
    /// Gets or sets the pharmacist/treatment supplier's verifier ID (RXE.14).
    /// </summary>
    public IdentifierField PharmacistTreatmentSuppliersVerifierId
    {
        get => this[14] as IdentifierField ?? new IdentifierField();
        set => this[14] = value;
    }

    /// <summary>
    /// Gets or sets the prescription number (RXE.15).
    /// </summary>
    public StringField PrescriptionNumber
    {
        get => this[15] as StringField ?? new StringField();
        set => this[15] = value;
    }

    /// <summary>
    /// Gets or sets the number of refills remaining (RXE.16).
    /// </summary>
    public NumericField NumberOfRefillsRemaining
    {
        get => this[16] as NumericField ?? new NumericField();
        set => this[16] = value;
    }

    /// <summary>
    /// Gets or sets the number of refills/doses dispensed (RXE.17).
    /// </summary>
    public NumericField NumberOfRefillsDosesDispensed
    {
        get => this[17] as NumericField ?? new NumericField();
        set => this[17] = value;
    }

    /// <summary>
    /// Gets or sets the D/T of most recent refill or dose dispensed (RXE.18).
    /// </summary>
    public TimestampField DateTimeOfMostRecentRefillOrDoseDispensed
    {
        get => this[18] as TimestampField ?? new TimestampField();
        set => this[18] = value;
    }

    /// <summary>
    /// Gets or sets the total daily dose (RXE.19).
    /// </summary>
    public StringField TotalDailyDose
    {
        get => this[19] as StringField ?? new StringField();
        set => this[19] = value;
    }

    /// <summary>
    /// Gets or sets the needs human review (RXE.20).
    /// </summary>
    public IdentifierField NeedsHumanReview
    {
        get => this[20] as IdentifierField ?? new IdentifierField();
        set => this[20] = value;
    }

    /// <summary>
    /// Gets or sets the pharmacy/treatment supplier's special dispensing instructions (RXE.21).
    /// </summary>
    public StringField PharmacyTreatmentSuppliersSpecialDispensingInstructions
    {
        get => this[21] as StringField ?? new StringField();
        set => this[21] = value;
    }

    /// <summary>
    /// Gets or sets the provider pharmacy treatment instructions (alias for test compatibility).
    /// </summary>
    public StringField ProviderPharmacyTreatmentInstructions
    {
        get => PharmacyTreatmentSuppliersSpecialDispensingInstructions;
        set => PharmacyTreatmentSuppliersSpecialDispensingInstructions = value;
    }

    /// <inheritdoc />
    protected override void InitializeFields()
    {
        // RXE.1: Quantity/Timing (Required)
        AddField(new TimingQuantityField(isRequired: true));
        
        // RXE.2: Give Code (Required)
        AddField(new CodedElementField(isRequired: true));
        
        // RXE.3: Give Amount - Minimum (Required)
        AddField(new CodedElementField(isRequired: true));
        
        // RXE.4: Give Amount - Maximum
        AddField(new NumericField());
        
        // RXE.5: Give Units (Required)
        AddField(new CodedElementField(isRequired: true));
        
        // RXE.6: Give Dosage Form
        AddField(new CodedElementField());
        
        // RXE.7: Provider's Administration Instructions
        AddField(new StringField());
        
        // RXE.8: Deliver-to Location
        AddField(new StringField());
        
        // RXE.9: Substitution Status
        AddField(new IdentifierField());
        
        // RXE.10: Dispense Amount
        AddField(new NumericField());
        
        // RXE.11: Dispense Units
        AddField(new CodedElementField());
        
        // RXE.12: Number of Refills
        AddField(new NumericField());
        
        // RXE.13: Ordering Provider's DEA Number
        AddField(new IdentifierField());
        
        // RXE.14: Pharmacist/Treatment Supplier's Verifier ID
        AddField(new IdentifierField());
        
        // RXE.15: Prescription Number
        AddField(new StringField());
        
        // RXE.16: Number of Refills Remaining
        AddField(new NumericField());
        
        // RXE.17: Number of Refills/Doses Dispensed
        AddField(new NumericField());
        
        // RXE.18: D/T of Most Recent Refill or Dose Dispensed
        AddField(new TimestampField());
        
        // RXE.19: Total Daily Dose
        AddField(new StringField());
        
        // RXE.20: Needs Human Review
        AddField(new IdentifierField());
        
        // RXE.21: Pharmacy/Treatment Supplier's Special Dispensing Instructions
        AddField(new StringField());
    }

    /// <summary>
    /// Sets basic medication order information.
    /// </summary>
    /// <param name="drugCode">The drug code (NDC, RxNorm, etc.).</param>
    /// <param name="drugName">The drug name.</param>
    /// <param name="codingSystem">The coding system used (NDC, RXNORM, etc.).</param>
    /// <param name="giveAmount">The amount to give per dose.</param>
    /// <param name="giveUnits">The units for the give amount.</param>
    /// <param name="quantityTiming">The quantity and timing instructions.</param>
    /// <param name="dosageForm">The dosage form (tablet, capsule, etc.).</param>
    public void SetBasicMedicationInfo(
        string drugCode,
        string drugName,
        string? codingSystem = null,
        decimal? giveAmount = null,
        string? giveUnits = null,
        string? quantityTiming = null,
        string? dosageForm = null)
    {
        // Set give code (drug)
        var giveCodeValue = string.IsNullOrEmpty(codingSystem) 
            ? $"{drugCode}^{drugName}" 
            : $"{drugCode}^{drugName}^{codingSystem}";
        GiveCode.SetValue(giveCodeValue);
        
        // Set give amount if provided
        if (giveAmount.HasValue)
        {
            GiveAmountMinimum.Quantity = giveAmount.Value.ToString();
        }
        
        // Set give units if provided
        if (!string.IsNullOrEmpty(giveUnits))
        {
            GiveUnits.SetValue(giveUnits);
        }
        
        // Set quantity/timing if provided
        if (!string.IsNullOrEmpty(quantityTiming))
        {
            QuantityTiming.SetValue(quantityTiming);
        }
        
        // Set dosage form if provided
        if (!string.IsNullOrEmpty(dosageForm))
        {
            GiveDosageForm.SetValue(dosageForm);
        }
    }

    /// <summary>
    /// Sets dispensing information.
    /// </summary>
    /// <param name="dispenseAmount">The amount to dispense.</param>
    /// <param name="dispenseUnits">The units for dispense amount.</param>
    /// <param name="numberOfRefills">The number of refills allowed.</param>
    /// <param name="prescriptionNumber">The prescription number.</param>
    /// <param name="substitutionStatus">Substitution allowed status.</param>
    public void SetDispensingInfo(
        decimal? dispenseAmount = null,
        string? dispenseUnits = null,
        int? numberOfRefills = null,
        string? prescriptionNumber = null,
        string? substitutionStatus = null)
    {
        if (dispenseAmount.HasValue)
            DispenseAmount.SetValue(dispenseAmount.Value.ToString());
            
        if (!string.IsNullOrEmpty(dispenseUnits))
            DispenseUnits.SetValue(dispenseUnits);
            
        if (numberOfRefills.HasValue)
        {
            NumberOfRefills.SetValue(numberOfRefills.Value.ToString());
            NumberOfRefillsRemaining.SetValue(numberOfRefills.Value.ToString());
        }
        
        if (!string.IsNullOrEmpty(prescriptionNumber))
            PrescriptionNumber.SetValue(prescriptionNumber);
            
        if (!string.IsNullOrEmpty(substitutionStatus))
            SubstitutionStatus.SetValue(substitutionStatus);
    }

    /// <summary>
    /// Sets provider and pharmacy information.
    /// </summary>
    /// <param name="orderingProviderDEA">The ordering provider's DEA number.</param>
    /// <param name="pharmacistVerifierId">The pharmacist verifier ID.</param>
    /// <param name="deliverToLocation">The delivery location.</param>
    /// <param name="adminInstructions">Administration instructions.</param>
    /// <param name="dispensingInstructions">Special dispensing instructions.</param>
    public void SetProviderInfo(
        string? orderingProviderDEA = null,
        string? pharmacistVerifierId = null,
        string? deliverToLocation = null,
        string? adminInstructions = null,
        string? dispensingInstructions = null)
    {
        if (!string.IsNullOrEmpty(orderingProviderDEA))
            OrderingProvidersDEANumber.SetValue(orderingProviderDEA);
            
        if (!string.IsNullOrEmpty(pharmacistVerifierId))
            PharmacistTreatmentSuppliersVerifierId.SetValue(pharmacistVerifierId);
            
        if (!string.IsNullOrEmpty(deliverToLocation))
            DeliverToLocation.SetValue(deliverToLocation);
            
        if (!string.IsNullOrEmpty(adminInstructions))
            ProvidersAdministrationInstructions.SetValue(adminInstructions);
            
        if (!string.IsNullOrEmpty(dispensingInstructions))
            PharmacyTreatmentSuppliersSpecialDispensingInstructions.SetValue(dispensingInstructions);
    }

    /// <summary>
    /// Sets daily dosing information.
    /// </summary>
    /// <param name="totalDailyDose">The total daily dose amount.</param>
    /// <param name="doseUnits">The units for the daily dose.</param>
    public void SetDailyDosing(decimal totalDailyDose, string doseUnits)
    {
        TotalDailyDose.SetValue($"{totalDailyDose}^{doseUnits}");
    }

    /// <summary>
    /// Generates a unique prescription number.
    /// </summary>
    /// <param name="prefix">Optional prefix for the prescription number.</param>
    public void GeneratePrescriptionNumber(string? prefix = null)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd");
        var random = Random.Shared.Next(100000, 999999);
        var rxNumber = prefix != null ? $"{prefix}{timestamp}{random}" : $"RX{timestamp}{random}";
        PrescriptionNumber.SetValue(rxNumber);
    }

    /// <inheritdoc />
    public override HL7Segment Clone()
    {
        var clone = new RXESegment();
        
        // Copy all field values
        for (int i = 1; i <= FieldCount; i++)
        {
            clone[i] = this[i].Clone();
        }
        
        return clone;
    }

    /// <summary>
    /// Creates an RXE segment for a standard medication order.
    /// </summary>
    /// <param name="drugCode">The drug code.</param>
    /// <param name="drugName">The drug name.</param>
    /// <param name="strength">The drug strength/amount.</param>
    /// <param name="strengthUnits">The units for strength.</param>
    /// <param name="dosageForm">The dosage form.</param>
    /// <param name="dispenseQuantity">Quantity to dispense.</param>
    /// <param name="refills">Number of refills.</param>
    /// <param name="sig">Directions for use.</param>
    /// <returns>A configured RXE segment.</returns>
    public static RXESegment CreateStandardOrder(
        string drugCode,
        string drugName,
        decimal strength,
        string strengthUnits,
        string dosageForm,
        decimal? dispenseQuantity = null,
        int? refills = null,
        string? sig = null)
    {
        var rxe = new RXESegment();
        
        rxe.SetBasicMedicationInfo(
            drugCode, 
            drugName, 
            "NDC", 
            strength, 
            strengthUnits, 
            sig, 
            dosageForm);
            
        if (dispenseQuantity.HasValue)
        {
            rxe.SetDispensingInfo(
                dispenseQuantity, 
                strengthUnits, 
                refills);
        }
        
        rxe.GeneratePrescriptionNumber();
        
        return rxe;
    }
}
