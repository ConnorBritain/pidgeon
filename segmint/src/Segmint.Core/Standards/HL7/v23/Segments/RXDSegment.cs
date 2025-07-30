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
/// Represents an HL7 Pharmacy/Treatment Dispense (RXD) segment.
/// This segment contains information about the actual dispensing of medication,
/// including what was dispensed, when, by whom, and in what quantities.
/// Critical for pharmacy workflows and medication tracking.
/// Used in RDS (Pharmacy Dispense) messages.
/// </summary>
public class RXDSegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "RXD";

    /// <summary>
    /// Initializes a new instance of the <see cref="RXDSegment"/> class.
    /// </summary>
    public RXDSegment()
    {
    }

    /// <summary>
    /// Gets or sets the dispense sub-ID counter (RXD.1) - Required.
    /// Sequential number for multiple dispenses of the same prescription.
    /// </summary>
    public NumericField DispenseSubIdCounter
    {
        get => this[1] as NumericField ?? new NumericField(isRequired: true);
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the dispense/give code (RXD.2) - Required.
    /// Code for the medication that was actually dispensed.
    /// </summary>
    public CodedElementField DispenseGiveCode
    {
        get => this[2] as CodedElementField ?? new CodedElementField(isRequired: true);
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the date/time dispensed (RXD.3) - Required.
    /// When the medication was dispensed.
    /// </summary>
    public TimestampField DateTimeDispensed
    {
        get => this[3] as TimestampField ?? new TimestampField(isRequired: true);
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the actual dispense amount (RXD.4) - Required.
    /// Quantity actually dispensed.
    /// </summary>
    public NumericField ActualDispenseAmount
    {
        get => this[4] as NumericField ?? new NumericField(isRequired: true);
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the actual dispense units (RXD.5).
    /// Units for the dispensed quantity.
    /// </summary>
    public CodedElementField ActualDispenseUnits
    {
        get => this[5] as CodedElementField ?? new CodedElementField();
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the actual dosage form (RXD.6).
    /// Form of the medication that was dispensed.
    /// </summary>
    public CodedElementField ActualDosageForm
    {
        get => this[6] as CodedElementField ?? new CodedElementField();
        set => this[6] = value;
    }

    /// <summary>
    /// Gets or sets the prescription number (RXD.7) - Required.
    /// Prescription or order number being dispensed.
    /// </summary>
    public StringField PrescriptionNumber
    {
        get => this[7] as StringField ?? new StringField(isRequired: true);
        set => this[7] = value;
    }

    /// <summary>
    /// Gets or sets the number of refills remaining (RXD.8).
    /// Number of refills left after this dispense.
    /// </summary>
    public NumericField NumberOfRefillsRemaining
    {
        get => this[8] as NumericField ?? new NumericField();
        set => this[8] = value;
    }

    /// <summary>
    /// Gets or sets the dispense notes (RXD.9).
    /// Free text notes about the dispensing.
    /// </summary>
    public StringField DispenseNotes
    {
        get => this[9] as StringField ?? new StringField();
        set => this[9] = value;
    }

    /// <summary>
    /// Gets or sets the dispensing provider (RXD.10).
    /// Pharmacist or provider who dispensed the medication.
    /// </summary>
    public ExtendedCompositeIdField DispensingProvider
    {
        get => this[10] as ExtendedCompositeIdField ?? new ExtendedCompositeIdField();
        set => this[10] = value;
    }

    /// <summary>
    /// Gets or sets the substitution status (RXD.11).
    /// Whether substitution was made (Y/N/G for generic).
    /// </summary>
    public IdentifierField SubstitutionStatus
    {
        get => this[11] as IdentifierField ?? new IdentifierField();
        set => this[11] = value;
    }

    /// <summary>
    /// Gets or sets the total daily dose (RXD.12).
    /// Total amount to be taken per day.
    /// </summary>
    public CompositeQuantityField TotalDailyDose
    {
        get => this[12] as CompositeQuantityField ?? new CompositeQuantityField();
        set => this[12] = value;
    }

    /// <summary>
    /// Gets or sets the deliver-to location (RXD.13).
    /// Where the medication was delivered.
    /// </summary>
    public StringField DeliverToLocation
    {
        get => this[13] as StringField ?? new StringField();
        set => this[13] = value;
    }

    /// <summary>
    /// Gets or sets the needs human review (RXD.14).
    /// Whether the dispense needs review (Y/N).
    /// </summary>
    public IdentifierField NeedsHumanReview
    {
        get => this[14] as IdentifierField ?? new IdentifierField();
        set => this[14] = value;
    }

    /// <summary>
    /// Gets or sets the pharmacy/treatment supplier's special dispensing instructions (RXD.15).
    /// Special instructions for the patient.
    /// </summary>
    public CodedElementField PharmacyTreatmentSuppliersSpecialDispensingInstructions
    {
        get => this[15] as CodedElementField ?? new CodedElementField();
        set => this[15] = value;
    }

    /// <summary>
    /// Gets or sets the actual strength (RXD.16).
    /// Actual strength of the dispensed medication.
    /// </summary>
    public NumericField ActualStrength
    {
        get => this[16] as NumericField ?? new NumericField();
        set => this[16] = value;
    }

    /// <summary>
    /// Gets or sets the actual strength unit (RXD.17).
    /// Units for the actual strength.
    /// </summary>
    public CodedElementField ActualStrengthUnit
    {
        get => this[17] as CodedElementField ?? new CodedElementField();
        set => this[17] = value;
    }

    /// <summary>
    /// Gets or sets the substance lot number (RXD.18).
    /// Lot number of the dispensed medication.
    /// </summary>
    public StringField SubstanceLotNumber
    {
        get => this[18] as StringField ?? new StringField();
        set => this[18] = value;
    }

    /// <summary>
    /// Gets or sets the substance expiration date (RXD.19).
    /// Expiration date of the dispensed medication.
    /// </summary>
    public TimestampField SubstanceExpirationDate
    {
        get => this[19] as TimestampField ?? new TimestampField();
        set => this[19] = value;
    }

    /// <summary>
    /// Gets or sets the substance manufacturer name (RXD.20).
    /// Manufacturer of the dispensed medication.
    /// </summary>
    public CodedElementField SubstanceManufacturerName
    {
        get => this[20] as CodedElementField ?? new CodedElementField();
        set => this[20] = value;
    }

    /// <summary>
    /// Gets or sets the indication (RXD.21).
    /// Clinical indication for the medication.
    /// </summary>
    public CodedElementField Indication
    {
        get => this[21] as CodedElementField ?? new CodedElementField();
        set => this[21] = value;
    }

    /// <summary>
    /// Gets or sets the dispense package size (RXD.22).
    /// Size of the package dispensed.
    /// </summary>
    public NumericField DispensePackageSize
    {
        get => this[22] as NumericField ?? new NumericField();
        set => this[22] = value;
    }

    /// <summary>
    /// Gets or sets the dispense package size unit (RXD.23).
    /// Units for the package size.
    /// </summary>
    public CodedElementField DispensePackageSizeUnit
    {
        get => this[23] as CodedElementField ?? new CodedElementField();
        set => this[23] = value;
    }

    /// <summary>
    /// Gets or sets the dispense package method (RXD.24).
    /// Method used for packaging.
    /// </summary>
    public IdentifierField DispensePackageMethod
    {
        get => this[24] as IdentifierField ?? new IdentifierField();
        set => this[24] = value;
    }

    /// <inheritdoc />
    protected override void InitializeFields()
    {
        // RXD.1: Dispense Sub-ID Counter (Required)
        AddField(new NumericField(isRequired: true));
        
        // RXD.2: Dispense/Give Code (Required)
        AddField(new CodedElementField(isRequired: true));
        
        // RXD.3: Date/Time Dispensed (Required)
        AddField(new TimestampField(isRequired: true));
        
        // RXD.4: Actual Dispense Amount (Required)
        AddField(new NumericField(isRequired: true));
        
        // RXD.5: Actual Dispense Units
        AddField(new CodedElementField());
        
        // RXD.6: Actual Dosage Form
        AddField(new CodedElementField());
        
        // RXD.7: Prescription Number (Required)
        AddField(new StringField(isRequired: true));
        
        // RXD.8: Number of Refills Remaining
        AddField(new NumericField());
        
        // RXD.9: Dispense Notes
        AddField(new StringField());
        
        // RXD.10: Dispensing Provider
        AddField(new ExtendedCompositeIdField());
        
        // RXD.11: Substitution Status
        AddField(new IdentifierField());
        
        // RXD.12: Total Daily Dose
        AddField(new CompositeQuantityField());
        
        // RXD.13: Deliver-To Location
        AddField(new StringField());
        
        // RXD.14: Needs Human Review
        AddField(new IdentifierField());
        
        // RXD.15: Pharmacy/Treatment Supplier's Special Dispensing Instructions
        AddField(new CodedElementField());
        
        // RXD.16: Actual Strength
        AddField(new NumericField());
        
        // RXD.17: Actual Strength Unit
        AddField(new CodedElementField());
        
        // RXD.18: Substance Lot Number
        AddField(new StringField());
        
        // RXD.19: Substance Expiration Date
        AddField(new TimestampField());
        
        // RXD.20: Substance Manufacturer Name
        AddField(new CodedElementField());
        
        // RXD.21: Indication
        AddField(new CodedElementField());
        
        // RXD.22: Dispense Package Size
        AddField(new NumericField());
        
        // RXD.23: Dispense Package Size Unit
        AddField(new CodedElementField());
        
        // RXD.24: Dispense Package Method
        AddField(new IdentifierField());
    }

    /// <summary>
    /// Sets basic dispensing information.
    /// </summary>
    /// <param name="dispenseId">Dispense sub-ID</param>
    /// <param name="medicationCode">Code for dispensed medication</param>
    /// <param name="medicationName">Name of dispensed medication</param>
    /// <param name="dispensedAmount">Amount dispensed</param>
    /// <param name="units">Units for amount</param>
    /// <param name="prescriptionNumber">Prescription number</param>
    /// <param name="dispensedDate">Date/time dispensed</param>
    public void SetBasicDispense(
        int dispenseId,
        string medicationCode,
        string medicationName,
        decimal dispensedAmount,
        string units,
        string prescriptionNumber,
        DateTime? dispensedDate = null)
    {
        DispenseSubIdCounter.SetValue(dispenseId.ToString());
        DispenseGiveCode.SetComponents(medicationCode, medicationName, "NDC");
        ActualDispenseAmount.SetValue(dispensedAmount.ToString());
        ActualDispenseUnits.SetComponents(units, "", "UCUM");
        PrescriptionNumber.SetValue(prescriptionNumber);
        DateTimeDispensed.SetValue(dispensedDate ?? DateTime.Now);
    }

    /// <summary>
    /// Sets medication details.
    /// </summary>
    /// <param name="dosageForm">Dosage form</param>
    /// <param name="strength">Medication strength</param>
    /// <param name="strengthUnits">Strength units</param>
    /// <param name="lotNumber">Lot number</param>
    /// <param name="expirationDate">Expiration date</param>
    /// <param name="manufacturer">Manufacturer</param>
    public void SetMedicationDetails(
        string? dosageForm = null,
        decimal? strength = null,
        string? strengthUnits = null,
        string? lotNumber = null,
        DateTime? expirationDate = null,
        string? manufacturer = null)
    {
        if (!string.IsNullOrEmpty(dosageForm))
            ActualDosageForm.SetComponents(dosageForm);
            
        if (strength.HasValue)
            ActualStrength.SetValue(strength.Value.ToString());
            
        if (!string.IsNullOrEmpty(strengthUnits))
            ActualStrengthUnit.SetComponents(strengthUnits);
            
        if (!string.IsNullOrEmpty(lotNumber))
            SubstanceLotNumber.SetValue(lotNumber);
            
        if (expirationDate.HasValue)
            SubstanceExpirationDate.SetValue(expirationDate.Value);
            
        if (!string.IsNullOrEmpty(manufacturer))
            SubstanceManufacturerName.SetComponents(manufacturer);
    }

    /// <summary>
    /// Sets pharmacy information.
    /// </summary>
    /// <param name="pharmacist">Dispensing pharmacist</param>
    /// <param name="refillsRemaining">Number of refills remaining</param>
    /// <param name="substitutionMade">Whether substitution was made</param>
    /// <param name="dispensingNotes">Dispensing notes</param>
    /// <param name="needsReview">Whether needs human review</param>
    public void SetPharmacyInfo(
        string? pharmacist = null,
        int? refillsRemaining = null,
        bool? substitutionMade = null,
        string? dispensingNotes = null,
        bool? needsReview = null)
    {
        if (!string.IsNullOrEmpty(pharmacist))
            DispensingProvider.SetValue(pharmacist);
            
        if (refillsRemaining.HasValue)
            NumberOfRefillsRemaining.SetValue(refillsRemaining.Value.ToString());
            
        if (substitutionMade.HasValue)
            SubstitutionStatus.SetValue(substitutionMade.Value ? "Y" : "N");
            
        if (!string.IsNullOrEmpty(dispensingNotes))
            DispenseNotes.SetValue(dispensingNotes);
            
        if (needsReview.HasValue)
            NeedsHumanReview.SetValue(needsReview.Value ? "Y" : "N");
    }

    /// <summary>
    /// Sets delivery information.
    /// </summary>
    /// <param name="deliveryLocation">Where medication was delivered</param>
    /// <param name="packageSize">Package size</param>
    /// <param name="packageUnits">Package units</param>
    /// <param name="packageMethod">Packaging method</param>
    /// <param name="specialInstructions">Special dispensing instructions</param>
    public void SetDeliveryInfo(
        string? deliveryLocation = null,
        decimal? packageSize = null,
        string? packageUnits = null,
        string? packageMethod = null,
        string? specialInstructions = null)
    {
        if (!string.IsNullOrEmpty(deliveryLocation))
            DeliverToLocation.SetValue(deliveryLocation);
            
        if (packageSize.HasValue)
            DispensePackageSize.SetValue(packageSize.Value.ToString());
            
        if (!string.IsNullOrEmpty(packageUnits))
            DispensePackageSizeUnit.SetComponents(packageUnits);
            
        if (!string.IsNullOrEmpty(packageMethod))
            DispensePackageMethod.SetValue(packageMethod);
            
        if (!string.IsNullOrEmpty(specialInstructions))
            PharmacyTreatmentSuppliersSpecialDispensingInstructions.SetComponents(specialInstructions);
    }

    /// <summary>
    /// Sets clinical information.
    /// </summary>
    /// <param name="indication">Clinical indication</param>
    /// <param name="dailyDose">Total daily dose</param>
    /// <param name="dailyDoseUnits">Daily dose units</param>
    public void SetClinicalInfo(
        string? indication = null,
        decimal? dailyDose = null,
        string? dailyDoseUnits = null)
    {
        if (!string.IsNullOrEmpty(indication))
            Indication.SetComponents(indication);
            
        if (dailyDose.HasValue && !string.IsNullOrEmpty(dailyDoseUnits))
            TotalDailyDose.SetComponents(dailyDose.Value, dailyDoseUnits);
    }

    /// <summary>
    /// Determines if this is a generic substitution.
    /// </summary>
    /// <returns>True if generic substitution was made.</returns>
    public bool IsGenericSubstitution()
    {
        return SubstitutionStatus.Value == "G" || SubstitutionStatus.Value == "Y";
    }

    /// <summary>
    /// Determines if medication is expired.
    /// </summary>
    /// <returns>True if expiration date is in the past.</returns>
    public bool IsExpired()
    {
        var expirationDate = SubstanceExpirationDate.ToDateTime();
        if (expirationDate.HasValue)
            return expirationDate.Value < DateTime.Now;
        return false;
    }

    /// <summary>
    /// Gets days until expiration.
    /// </summary>
    /// <returns>Number of days until expiration, or null if no expiration date.</returns>
    public int? GetDaysUntilExpiration()
    {
        var expirationDate = SubstanceExpirationDate.ToDateTime();
        if (expirationDate.HasValue)
        {
            var timeSpan = expirationDate.Value - DateTime.Now;
            return (int)timeSpan.TotalDays;
        }
        return null;
    }

    /// <summary>
    /// Gets a display-friendly representation of the dispense.
    /// </summary>
    /// <returns>Formatted dispense string.</returns>
    public string GetDisplayValue()
    {
        var medication = DispenseGiveCode.Text ?? DispenseGiveCode.Identifier ?? "Unknown medication";
        var amount = ActualDispenseAmount.RawValue ?? "";
        var units = ActualDispenseUnits.Identifier ?? "";
        var date = DateTimeDispensed.ToDateTime()?.ToString("MM/dd/yyyy") ?? "";
        var rxNumber = PrescriptionNumber.RawValue ?? "";
        
        return $"{medication} {amount} {units} - Rx#{rxNumber} on {date}";
    }

    /// <inheritdoc />
    public override List<string> Validate()
    {
        var errors = base.Validate();

        // Validate required fields
        if (string.IsNullOrEmpty(DispenseSubIdCounter.RawValue))
            errors.Add("Dispense Sub-ID Counter (RXD.1) is required");

        if (string.IsNullOrEmpty(DispenseGiveCode.Identifier))
            errors.Add("Dispense/Give Code (RXD.2) is required");

        if (!DateTimeDispensed.ToDateTime().HasValue)
            errors.Add("Date/Time Dispensed (RXD.3) is required");

        if (string.IsNullOrEmpty(ActualDispenseAmount.RawValue))
            errors.Add("Actual Dispense Amount (RXD.4) is required");

        if (string.IsNullOrEmpty(PrescriptionNumber.RawValue))
            errors.Add("Prescription Number (RXD.7) is required");

        // Validate Y/N fields
        if (!string.IsNullOrEmpty(SubstitutionStatus.Value))
        {
            var validStatuses = new[] { "Y", "N", "G" };
            if (!validStatuses.Contains(SubstitutionStatus.Value))
            {
                errors.Add("Substitution Status (RXD.11) must be Y, N, or G");
            }
        }

        if (!string.IsNullOrEmpty(NeedsHumanReview.Value) && 
            NeedsHumanReview.Value != "Y" && NeedsHumanReview.Value != "N")
        {
            errors.Add("Needs Human Review (RXD.14) must be Y or N");
        }

        // Validate logical relationships
        if (!string.IsNullOrEmpty(NumberOfRefillsRemaining.RawValue))
        {
            if (decimal.TryParse(NumberOfRefillsRemaining.RawValue, out var refills) && refills < 0)
            {
                errors.Add("Number of Refills Remaining cannot be negative");
            }
        }

        // Validate expiration date
        var expirationDate = SubstanceExpirationDate.ToDateTime();
        if (expirationDate.HasValue && expirationDate.Value < DateTime.Now.AddDays(-1))
        {
            errors.Add("WARNING: Medication appears to be expired");
        }

        return errors;
    }

    /// <inheritdoc />
    public override HL7Segment Clone()
    {
        var clone = new RXDSegment();

        // Copy all field values
        for (int i = 1; i <= FieldCount; i++)
        {
            clone[i] = this[i]?.Clone()!;
        }

        return clone;
    }

    /// <summary>
    /// Creates a basic medication dispense RXD segment.
    /// </summary>
    /// <param name="prescriptionNumber">Prescription number</param>
    /// <param name="medicationCode">Medication NDC code</param>
    /// <param name="medicationName">Medication name</param>
    /// <param name="amount">Amount dispensed</param>
    /// <param name="units">Units</param>
    /// <param name="pharmacist">Dispensing pharmacist</param>
    /// <returns>Configured RXD segment.</returns>
    public static RXDSegment CreateBasicDispense(
        string prescriptionNumber,
        string medicationCode,
        string medicationName,
        decimal amount,
        string units,
        string? pharmacist = null)
    {
        var rxd = new RXDSegment();
        rxd.SetBasicDispense(1, medicationCode, medicationName, amount, units, prescriptionNumber);
        if (!string.IsNullOrEmpty(pharmacist))
            rxd.SetPharmacyInfo(pharmacist);
        return rxd;
    }

    /// <summary>
    /// Creates a controlled substance dispense with lot tracking.
    /// </summary>
    /// <param name="prescriptionNumber">Prescription number</param>
    /// <param name="medicationCode">Medication code</param>
    /// <param name="medicationName">Medication name</param>
    /// <param name="amount">Amount dispensed</param>
    /// <param name="units">Units</param>
    /// <param name="lotNumber">Lot number</param>
    /// <param name="expirationDate">Expiration date</param>
    /// <param name="manufacturer">Manufacturer</param>
    /// <param name="pharmacist">Dispensing pharmacist</param>
    /// <returns>Configured RXD segment for controlled substance.</returns>
    public static RXDSegment CreateControlledSubstanceDispense(
        string prescriptionNumber,
        string medicationCode,
        string medicationName,
        decimal amount,
        string units,
        string lotNumber,
        DateTime expirationDate,
        string manufacturer,
        string pharmacist)
    {
        var rxd = new RXDSegment();
        rxd.SetBasicDispense(1, medicationCode, medicationName, amount, units, prescriptionNumber);
        rxd.SetMedicationDetails(lotNumber: lotNumber, expirationDate: expirationDate, manufacturer: manufacturer);
        rxd.SetPharmacyInfo(pharmacist: pharmacist, needsReview: true);
        return rxd;
    }
}
