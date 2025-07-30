// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace Segmint.Core.DataGeneration.Pharmacy;

/// <summary>
/// Represents medication information for synthetic data generation.
/// </summary>
public class MedicationData
{
    /// <summary>
    /// NDC (National Drug Code) number.
    /// </summary>
    public string NDC { get; set; } = "";

    /// <summary>
    /// Generic medication name.
    /// </summary>
    public string GenericName { get; set; } = "";

    /// <summary>
    /// Brand/trade name.
    /// </summary>
    public string? BrandName { get; set; }

    /// <summary>
    /// Drug strength (e.g., "250", "10").
    /// </summary>
    public string Strength { get; set; } = "";

    /// <summary>
    /// Strength units (e.g., "mg", "mcg", "units").
    /// </summary>
    public string StrengthUnits { get; set; } = "";

    /// <summary>
    /// Dosage form (e.g., "tablet", "capsule", "liquid").
    /// </summary>
    public string DosageForm { get; set; } = "";

    /// <summary>
    /// Route of administration (e.g., "oral", "injection", "topical").
    /// </summary>
    public string Route { get; set; } = "";

    /// <summary>
    /// Therapeutic category.
    /// </summary>
    public string TherapeuticCategory { get; set; } = "";

    /// <summary>
    /// DEA schedule (if controlled substance).
    /// </summary>
    public string? DEASchedule { get; set; }

    /// <summary>
    /// Manufacturer name.
    /// </summary>
    public string Manufacturer { get; set; } = "";

    /// <summary>
    /// Typical prescribing information.
    /// </summary>
    public PrescribingInfo PrescribingInfo { get; set; } = new();

    /// <summary>
    /// Whether this is a controlled substance.
    /// </summary>
    public bool IsControlledSubstance => !string.IsNullOrEmpty(DEASchedule);

    /// <summary>
    /// Gets the full medication name with strength.
    /// </summary>
    public string FullName => string.IsNullOrEmpty(BrandName) 
        ? $"{GenericName} {Strength}{StrengthUnits}"
        : $"{BrandName} ({GenericName}) {Strength}{StrengthUnits}";
}

/// <summary>
/// Prescribing information for a medication.
/// </summary>
public class PrescribingInfo
{
    /// <summary>
    /// Typical starting dose.
    /// </summary>
    public string TypicalDose { get; set; } = "";

    /// <summary>
    /// Typical frequency (e.g., "once daily", "twice daily").
    /// </summary>
    public string TypicalFrequency { get; set; } = "";

    /// <summary>
    /// Typical quantity dispensed.
    /// </summary>
    public int TypicalQuantity { get; set; }

    /// <summary>
    /// Typical days supply.
    /// </summary>
    public int TypicalDaysSupply { get; set; }

    /// <summary>
    /// Typical number of refills.
    /// </summary>
    public int TypicalRefills { get; set; }

    /// <summary>
    /// Common indications for use.
    /// </summary>
    public string[] Indications { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Common patient instructions (SIG).
    /// </summary>
    public string[] CommonSigs { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Represents prescription order information.
/// </summary>
public class PrescriptionOrder
{
    /// <summary>
    /// Prescription number.
    /// </summary>
    public string PrescriptionNumber { get; set; } = "";

    /// <summary>
    /// Medication being prescribed.
    /// </summary>
    public MedicationData Medication { get; set; } = new();

    /// <summary>
    /// Quantity to dispense.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Quantity units.
    /// </summary>
    public string QuantityUnits { get; set; } = "";

    /// <summary>
    /// Number of refills.
    /// </summary>
    public int Refills { get; set; }

    /// <summary>
    /// Days supply.
    /// </summary>
    public int DaysSupply { get; set; }

    /// <summary>
    /// Patient directions (SIG).
    /// </summary>
    public string Directions { get; set; } = "";

    /// <summary>
    /// Prescriber information.
    /// </summary>
    public PrescriberInfo Prescriber { get; set; } = new();

    /// <summary>
    /// Pharmacy information.
    /// </summary>
    public PharmacyInfo Pharmacy { get; set; } = new();

    /// <summary>
    /// Order date and time.
    /// </summary>
    public DateTime OrderDateTime { get; set; }

    /// <summary>
    /// Whether generic substitution is allowed.
    /// </summary>
    public bool GenericSubstitutionAllowed { get; set; } = true;

    /// <summary>
    /// Special instructions or notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Prior authorization number (if required).
    /// </summary>
    public string? PriorAuthNumber { get; set; }
}

/// <summary>
/// Prescriber information.
/// </summary>
public class PrescriberInfo
{
    /// <summary>
    /// Prescriber ID.
    /// </summary>
    public string ProviderId { get; set; } = "";

    /// <summary>
    /// Prescriber's name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// DEA number.
    /// </summary>
    public string DEANumber { get; set; } = "";

    /// <summary>
    /// NPI (National Provider Identifier).
    /// </summary>
    public string NPI { get; set; } = "";

    /// <summary>
    /// Specialty.
    /// </summary>
    public string Specialty { get; set; } = "";

    /// <summary>
    /// Phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = "";

    /// <summary>
    /// Address.
    /// </summary>
    public string Address { get; set; } = "";
}

/// <summary>
/// Pharmacy information.
/// </summary>
public class PharmacyInfo
{
    /// <summary>
    /// Pharmacy ID.
    /// </summary>
    public string PharmacyId { get; set; } = "";

    /// <summary>
    /// Pharmacy name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// NCPDP (National Council for Prescription Drug Programs) ID.
    /// </summary>
    public string NCPDP { get; set; } = "";

    /// <summary>
    /// DEA number.
    /// </summary>
    public string DEANumber { get; set; } = "";

    /// <summary>
    /// Phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = "";

    /// <summary>
    /// Address.
    /// </summary>
    public string Address { get; set; } = "";

    /// <summary>
    /// Pharmacist in charge.
    /// </summary>
    public string PharmacistInCharge { get; set; } = "";

    /// <summary>
    /// Whether this is a 24-hour pharmacy.
    /// </summary>
    public bool Is24Hour { get; set; }

    /// <summary>
    /// Pharmacy chain name (if applicable).
    /// </summary>
    public string? ChainName { get; set; }
}

/// <summary>
/// Insurance/coverage information for prescriptions.
/// </summary>
public class PrescriptionInsurance
{
    /// <summary>
    /// Primary insurance plan.
    /// </summary>
    public string PlanName { get; set; } = "";

    /// <summary>
    /// Insurance member ID.
    /// </summary>
    public string MemberId { get; set; } = "";

    /// <summary>
    /// Group number.
    /// </summary>
    public string? GroupNumber { get; set; }

    /// <summary>
    /// BIN (Bank Identification Number).
    /// </summary>
    public string? BIN { get; set; }

    /// <summary>
    /// PCN (Processor Control Number).
    /// </summary>
    public string? PCN { get; set; }

    /// <summary>
    /// Copay amount.
    /// </summary>
    public decimal? Copay { get; set; }

    /// <summary>
    /// Whether prior authorization is required.
    /// </summary>
    public bool PriorAuthRequired { get; set; }
}