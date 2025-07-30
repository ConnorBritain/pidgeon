// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Types;
using System.Collections.Generic;
using System.Linq;
using System;
namespace Segmint.Core.Standards.HL7.v23.Segments;

/// <summary>
/// Represents an HL7 Pharmacy/Treatment Order (RXO) segment.
/// This segment contains information about a pharmacy order including medication details,
/// dosing instructions, and clinical parameters for medication management.
/// Used in ORM, ORR, and RDE messages.
/// </summary>
public class RXOSegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "RXO";

    /// <summary>
    /// Initializes a new instance of the <see cref="RXOSegment"/> class.
    /// </summary>
    public RXOSegment()
    {
    }

    /// <summary>
    /// Gets or sets the requested give code (RXO.1) - Required.
    /// Contains the medication code being ordered (NDC, GCN, etc.).
    /// </summary>
    public CodedElementField RequestedGiveCode
    {
        get => this[1] as CodedElementField ?? new CodedElementField(isRequired: true);
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the requested give amount minimum (RXO.2) - Required.
    /// The minimum amount to be given per dose.
    /// </summary>
    public NumericField RequestedGiveAmountMinimum
    {
        get => this[2] as NumericField ?? new NumericField(isRequired: true);
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the requested give amount maximum (RXO.3).
    /// The maximum amount to be given per dose.
    /// </summary>
    public NumericField RequestedGiveAmountMaximum
    {
        get => this[3] as NumericField ?? new NumericField();
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the requested give units (RXO.4) - Required.
    /// Units for the give amount (mg, mL, tablets, etc.).
    /// </summary>
    public CodedElementField RequestedGiveUnits
    {
        get => this[4] as CodedElementField ?? new CodedElementField(isRequired: true);
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the requested dosage form (RXO.5).
    /// The form of the medication (tablet, capsule, liquid, etc.).
    /// </summary>
    public CodedElementField RequestedDosageForm
    {
        get => this[5] as CodedElementField ?? new CodedElementField();
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the provider's pharmacy/treatment instructions (RXO.6).
    /// Free text instructions from the provider.
    /// </summary>
    public CodedElementField ProvidersPharmacyTreatmentInstructions
    {
        get => this[6] as CodedElementField ?? new CodedElementField();
        set => this[6] = value;
    }

    /// <summary>
    /// Gets or sets the provider's administration instructions (RXO.7).
    /// Instructions for how the medication should be administered.
    /// </summary>
    public CodedElementField ProvidersAdministrationInstructions
    {
        get => this[7] as CodedElementField ?? new CodedElementField();
        set => this[7] = value;
    }

    /// <summary>
    /// Gets or sets the deliver-to location (RXO.8).
    /// Where the medication should be delivered.
    /// </summary>
    public StringField DeliverToLocation
    {
        get => this[8] as StringField ?? new StringField();
        set => this[8] = value;
    }

    /// <summary>
    /// Gets or sets the allow substitutions (RXO.9).
    /// Whether generic substitutions are allowed (Y/N).
    /// </summary>
    public IdentifierField AllowSubstitutions
    {
        get => this[9] as IdentifierField ?? new IdentifierField();
        set => this[9] = value;
    }

    /// <summary>
    /// Gets or sets the requested dispense code (RXO.10).
    /// Code for the medication to be dispensed.
    /// </summary>
    public CodedElementField RequestedDispenseCode
    {
        get => this[10] as CodedElementField ?? new CodedElementField();
        set => this[10] = value;
    }

    /// <summary>
    /// Gets or sets the requested dispense amount (RXO.11).
    /// Total amount to be dispensed.
    /// </summary>
    public NumericField RequestedDispenseAmount
    {
        get => this[11] as NumericField ?? new NumericField();
        set => this[11] = value;
    }

    /// <summary>
    /// Gets or sets the requested dispense units (RXO.12).
    /// Units for the dispense amount.
    /// </summary>
    public CodedElementField RequestedDispenseUnits
    {
        get => this[12] as CodedElementField ?? new CodedElementField();
        set => this[12] = value;
    }

    /// <summary>
    /// Gets or sets the number of refills (RXO.13).
    /// Number of refills authorized.
    /// </summary>
    public NumericField NumberOfRefills
    {
        get => this[13] as NumericField ?? new NumericField();
        set => this[13] = value;
    }

    /// <summary>
    /// Gets or sets the ordering provider's DEA number (RXO.14).
    /// DEA registration number of the prescribing provider.
    /// </summary>
    public ExtendedCompositeIdField OrderingProvidersDEANumber
    {
        get => this[14] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField();
        set => this[14] = value;
    }

    /// <summary>
    /// Gets or sets the pharmacist/treatment supplier's verifier ID (RXO.15).
    /// Identifier of the verifying pharmacist.
    /// </summary>
    public ExtendedCompositeIdField PharmacistTreatmentSuppliersVerifierId
    {
        get => this[15] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField();
        set => this[15] = value;
    }

    /// <summary>
    /// Gets or sets the needs human review (RXO.16).
    /// Whether the order requires human review (Y/N).
    /// </summary>
    public IdentifierField NeedsHumanReview
    {
        get => this[16] as IdentifierField ?? new IdentifierField();
        set => this[16] = value;
    }

    /// <summary>
    /// Gets or sets the requested give per time unit (RXO.17).
    /// Frequency of administration.
    /// </summary>
    public StringField RequestedGivePerTimeUnit
    {
        get => this[17] as StringField ?? new StringField();
        set => this[17] = value;
    }

    /// <summary>
    /// Gets or sets the requested give strength (RXO.18).
    /// Strength of the medication.
    /// </summary>
    public NumericField RequestedGiveStrength
    {
        get => this[18] as NumericField ?? new NumericField();
        set => this[18] = value;
    }

    /// <summary>
    /// Gets or sets the requested give strength units (RXO.19).
    /// Units for the medication strength.
    /// </summary>
    public CodedElementField RequestedGiveStrengthUnits
    {
        get => this[19] as CodedElementField ?? new CodedElementField();
        set => this[19] = value;
    }

    /// <summary>
    /// Gets or sets the indication (RXO.20).
    /// Clinical indication for the medication.
    /// </summary>
    public CodedElementField Indication
    {
        get => this[20] as CodedElementField ?? new CodedElementField();
        set => this[20] = value;
    }

    /// <summary>
    /// Gets or sets the requested give rate amount (RXO.21).
    /// Rate of administration amount.
    /// </summary>
    public StringField RequestedGiveRateAmount
    {
        get => this[21] as StringField ?? new StringField();
        set => this[21] = value;
    }

    /// <summary>
    /// Gets or sets the requested give rate units (RXO.22).
    /// Units for the rate of administration.
    /// </summary>
    public CodedElementField RequestedGiveRateUnits
    {
        get => this[22] as CodedElementField ?? new CodedElementField();
        set => this[22] = value;
    }

    /// <inheritdoc />
    protected override void InitializeFields()
    {
        // RXO.1: Requested Give Code (Required)
        AddField(new CodedElementField(isRequired: true));
        
        // RXO.2: Requested Give Amount - Minimum (Required)
        AddField(new NumericField(isRequired: true));
        
        // RXO.3: Requested Give Amount - Maximum
        AddField(new NumericField());
        
        // RXO.4: Requested Give Units (Required)
        AddField(new CodedElementField(isRequired: true));
        
        // RXO.5: Requested Dosage Form
        AddField(new CodedElementField());
        
        // RXO.6: Provider's Pharmacy/Treatment Instructions
        AddField(new CodedElementField());
        
        // RXO.7: Provider's Administration Instructions
        AddField(new CodedElementField());
        
        // RXO.8: Deliver-To Location
        AddField(new StringField());
        
        // RXO.9: Allow Substitutions
        AddField(new IdentifierField());
        
        // RXO.10: Requested Dispense Code
        AddField(new CodedElementField());
        
        // RXO.11: Requested Dispense Amount
        AddField(new NumericField());
        
        // RXO.12: Requested Dispense Units
        AddField(new CodedElementField());
        
        // RXO.13: Number of Refills
        AddField(new NumericField());
        
        // RXO.14: Ordering Provider's DEA Number
        AddField(new ExtendedCompositeIdField());
        
        // RXO.15: Pharmacist/Treatment Supplier's Verifier ID
        AddField(new ExtendedCompositeIdField());
        
        // RXO.16: Needs Human Review
        AddField(new IdentifierField());
        
        // RXO.17: Requested Give Per (Time Unit)
        AddField(new StringField());
        
        // RXO.18: Requested Give Strength
        AddField(new NumericField());
        
        // RXO.19: Requested Give Strength Units
        AddField(new CodedElementField());
        
        // RXO.20: Indication
        AddField(new CodedElementField());
        
        // RXO.21: Requested Give Rate Amount
        AddField(new StringField());
        
        // RXO.22: Requested Give Rate Units
        AddField(new CodedElementField());
    }

    /// <summary>
    /// Sets basic medication order information.
    /// </summary>
    /// <param name="medicationCode">Medication code (NDC, GCN, etc.)</param>
    /// <param name="medicationName">Medication name</param>
    /// <param name="codingSystem">Coding system (NDC, GCN, etc.)</param>
    /// <param name="amount">Amount per dose</param>
    /// <param name="units">Units for the amount</param>
    /// <param name="dosageForm">Dosage form (tablet, capsule, etc.)</param>
    public void SetBasicMedication(
        string medicationCode,
        string medicationName,
        string codingSystem,
        decimal amount,
        string units,
        string? dosageForm = null)
    {
        RequestedGiveCode.SetComponents(medicationCode, medicationName, codingSystem);
        RequestedGiveAmountMinimum.SetValue(amount.ToString());
        RequestedGiveUnits.SetComponents(units, "", "UCUM");
        
        if (!string.IsNullOrEmpty(dosageForm))
            RequestedDosageForm.SetComponents(dosageForm);
    }

    /// <summary>
    /// Sets dispensing information.
    /// </summary>
    /// <param name="dispenseAmount">Total amount to dispense</param>
    /// <param name="dispenseUnits">Units for dispensing</param>
    /// <param name="refills">Number of refills</param>
    /// <param name="substitutionsAllowed">Whether substitutions are allowed</param>
    public void SetDispensingInfo(
        decimal? dispenseAmount = null,
        string? dispenseUnits = null,
        int? refills = null,
        bool? substitutionsAllowed = null)
    {
        if (dispenseAmount.HasValue)
            RequestedDispenseAmount.SetValue(dispenseAmount.Value.ToString());
            
        if (!string.IsNullOrEmpty(dispenseUnits))
            RequestedDispenseUnits.SetComponents(dispenseUnits);
            
        if (refills.HasValue)
            NumberOfRefills.SetValue(refills.Value.ToString());
            
        if (substitutionsAllowed.HasValue)
            AllowSubstitutions.SetValue(substitutionsAllowed.Value ? "Y" : "N");
    }

    /// <summary>
    /// Sets administration instructions.
    /// </summary>
    /// <param name="instructions">Administration instructions</param>
    /// <param name="frequency">Frequency of administration</param>
    /// <param name="route">Route of administration</param>
    public void SetAdministrationInstructions(
        string? instructions = null,
        string? frequency = null,
        string? route = null)
    {
        if (!string.IsNullOrEmpty(instructions))
            ProvidersAdministrationInstructions.SetComponents(instructions);
            
        if (!string.IsNullOrEmpty(frequency))
            RequestedGivePerTimeUnit.SetValue(frequency);
    }

    /// <summary>
    /// Sets prescriber information.
    /// </summary>
    /// <param name="deaNumber">DEA number of prescriber</param>
    /// <param name="needsReview">Whether order needs human review</param>
    public void SetPrescriberInfo(
        string? deaNumber = null,
        bool? needsReview = null)
    {
        if (!string.IsNullOrEmpty(deaNumber))
            OrderingProvidersDEANumber.SetValue(deaNumber);
            
        if (needsReview.HasValue)
            NeedsHumanReview.SetValue(needsReview.Value ? "Y" : "N");
    }

    /// <summary>
    /// Sets clinical information.
    /// </summary>
    /// <param name="indication">Clinical indication</param>
    /// <param name="strength">Medication strength</param>
    /// <param name="strengthUnits">Units for strength</param>
    public void SetClinicalInfo(
        string? indication = null,
        decimal? strength = null,
        string? strengthUnits = null)
    {
        if (!string.IsNullOrEmpty(indication))
            Indication.SetComponents(indication);
            
        if (strength.HasValue)
            RequestedGiveStrength.SetValue(strength.Value.ToString());
            
        if (!string.IsNullOrEmpty(strengthUnits))
            RequestedGiveStrengthUnits.SetComponents(strengthUnits);
    }

    /// <inheritdoc />
    public override List<string> Validate()
    {
        var errors = base.Validate();

        // Validate required fields
        if (string.IsNullOrEmpty(RequestedGiveCode.Identifier))
            errors.Add("Requested Give Code (RXO.1) is required");

        if (string.IsNullOrEmpty(RequestedGiveAmountMinimum.RawValue))
            errors.Add("Requested Give Amount Minimum (RXO.2) is required");

        if (string.IsNullOrEmpty(RequestedGiveUnits.Identifier))
            errors.Add("Requested Give Units (RXO.4) is required");

        // Validate logical relationships
        if (!string.IsNullOrEmpty(RequestedGiveAmountMaximum.RawValue) && 
            !string.IsNullOrEmpty(RequestedGiveAmountMinimum.RawValue))
        {
            var max = RequestedGiveAmountMaximum.ToDecimal();
            var min = RequestedGiveAmountMinimum.ToDecimal();
            if (max.HasValue && min.HasValue)
            {
                if (max.Value < min.Value)
                    errors.Add("Requested Give Amount Maximum cannot be less than minimum");
            }
        }

        // Validate Y/N fields
        if (!string.IsNullOrEmpty(AllowSubstitutions.Value) && 
            AllowSubstitutions.Value != "Y" && AllowSubstitutions.Value != "N")
        {
            errors.Add("Allow Substitutions (RXO.9) must be Y or N");
        }

        if (!string.IsNullOrEmpty(NeedsHumanReview.Value) && 
            NeedsHumanReview.Value != "Y" && NeedsHumanReview.Value != "N")
        {
            errors.Add("Needs Human Review (RXO.16) must be Y or N");
        }

        return errors;
    }

    /// <inheritdoc />
    public override HL7Segment Clone()
    {
        var clone = new RXOSegment();

        // Copy all field values
        for (int i = 1; i <= FieldCount; i++)
        {
            clone[i] = this[i]?.Clone()!;
        }

        return clone;
    }

    /// <summary>
    /// Gets a display-friendly representation of the medication order.
    /// </summary>
    /// <returns>Formatted medication order string.</returns>
    public string GetDisplayValue()
    {
        var medication = RequestedGiveCode.Text ?? RequestedGiveCode.Identifier ?? "Unknown medication";
        var amount = RequestedGiveAmountMinimum.RawValue ?? "";
        var units = RequestedGiveUnits.Identifier ?? "";
        var form = RequestedDosageForm.Identifier ?? "";

        var parts = new List<string> { medication };
        
        if (!string.IsNullOrEmpty(amount) && !string.IsNullOrEmpty(units))
            parts.Add($"{amount} {units}");
            
        if (!string.IsNullOrEmpty(form))
            parts.Add(form);

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Creates a basic prescription RXO segment.
    /// </summary>
    /// <param name="medicationCode">Medication code</param>
    /// <param name="medicationName">Medication name</param>
    /// <param name="amount">Amount per dose</param>
    /// <param name="units">Units</param>
    /// <param name="totalAmount">Total amount to dispense</param>
    /// <param name="refills">Number of refills</param>
    /// <returns>Configured RXO segment.</returns>
    public static RXOSegment CreatePrescription(
        string medicationCode,
        string medicationName,
        decimal amount,
        string units,
        decimal? totalAmount = null,
        int? refills = null)
    {
        var rxo = new RXOSegment();
        rxo.SetBasicMedication(medicationCode, medicationName, "NDC", amount, units);
        rxo.SetDispensingInfo(totalAmount, units, refills, true);
        return rxo;
    }
}
