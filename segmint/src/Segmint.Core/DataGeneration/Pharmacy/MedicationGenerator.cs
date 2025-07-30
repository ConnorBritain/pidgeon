// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Segmint.Core.DataGeneration.Pharmacy;

/// <summary>
/// Generates realistic medication and prescription data for testing.
/// </summary>
public class MedicationGenerator : IDataGenerator<PrescriptionOrder>
{
    private readonly Random _random;
    private readonly MedicationDataSets _dataSets;

    /// <summary>
    /// Initializes a new instance of the <see cref="MedicationGenerator"/> class.
    /// </summary>
    /// <param name="seed">Random seed for reproducible generation.</param>
    public MedicationGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _dataSets = new MedicationDataSets();
    }

    /// <inheritdoc />
    public PrescriptionOrder Generate()
    {
        return Generate(new DataGenerationConstraints());
    }

    /// <inheritdoc />
    public IEnumerable<PrescriptionOrder> Generate(int count)
    {
        return Generate(count, new DataGenerationConstraints());
    }

    /// <inheritdoc />
    public PrescriptionOrder Generate(DataGenerationConstraints constraints)
    {
        var medication = GenerateMedication();
        var prescriber = GeneratePrescriber();
        var pharmacy = GeneratePharmacy();

        var prescription = new PrescriptionOrder
        {
            PrescriptionNumber = GeneratePrescriptionNumber(),
            Medication = medication,
            Quantity = GenerateQuantity(medication),
            QuantityUnits = medication.DosageForm == "liquid" ? "mL" : "each",
            Refills = GenerateRefills(medication),
            DaysSupply = medication.PrescribingInfo.TypicalDaysSupply,
            Directions = GenerateDirections(medication),
            Prescriber = prescriber,
            Pharmacy = pharmacy,
            OrderDateTime = GenerateOrderDateTime(constraints.DateRange),
            GenericSubstitutionAllowed = _random.NextDouble() < 0.8,
            Notes = GenerateNotes(),
            PriorAuthNumber = medication.IsControlledSubstance && _random.NextDouble() < 0.3 
                ? GeneratePriorAuthNumber() : null
        };

        return prescription;
    }

    /// <inheritdoc />
    public IEnumerable<PrescriptionOrder> Generate(int count, DataGenerationConstraints constraints)
    {
        for (int i = 0; i < count; i++)
        {
            yield return Generate(constraints);
        }
    }

    /// <summary>
    /// Generates a specific type of medication.
    /// </summary>
    /// <param name="category">Therapeutic category filter.</param>
    /// <returns>Generated medication data.</returns>
    public MedicationData GenerateMedication(string? category = null)
    {
        var medications = category == null 
            ? _dataSets.CommonMedications 
            : _dataSets.CommonMedications.Where(m => m.TherapeuticCategory.Equals(category, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (medications.Length == 0)
            medications = _dataSets.CommonMedications;

        var baseMedication = medications[_random.Next(medications.Length)];
        
        return new MedicationData
        {
            NDC = GenerateNDC(),
            GenericName = baseMedication.GenericName,
            BrandName = baseMedication.BrandName,
            Strength = baseMedication.Strength,
            StrengthUnits = baseMedication.StrengthUnits,
            DosageForm = baseMedication.DosageForm,
            Route = baseMedication.Route,
            TherapeuticCategory = baseMedication.TherapeuticCategory,
            DEASchedule = baseMedication.DEASchedule,
            Manufacturer = GenerateManufacturer(),
            PrescribingInfo = baseMedication.PrescribingInfo
        };
    }

    private string GenerateNDC()
    {
        // NDC format: 5-4-2 or 4-4-2
        var labeler = _random.Next(10000, 99999);
        var product = _random.Next(1000, 9999);
        var package = _random.Next(10, 99);
        return $"{labeler:D5}-{product:D4}-{package:D2}";
    }

    private string GenerateManufacturer()
    {
        var manufacturers = new[]
        {
            "Pfizer", "Johnson & Johnson", "Merck", "AbbVie", "Novartis", "Roche", "GSK",
            "Sanofi", "AstraZeneca", "Bristol Myers Squibb", "Eli Lilly", "Amgen",
            "Gilead Sciences", "Teva", "Mylan", "Sandoz", "Apotex", "Lupin"
        };
        return manufacturers[_random.Next(manufacturers.Length)];
    }

    private PrescriberInfo GeneratePrescriber()
    {
        var specialties = new[]
        {
            "Family Medicine", "Internal Medicine", "Cardiology", "Endocrinology",
            "Neurology", "Psychiatry", "Orthopedics", "Dermatology", "Pediatrics", "Oncology"
        };

        var titles = new[] { "Dr.", "MD", "DO", "NP", "PA" };
        var firstNames = new[] { "John", "Sarah", "Michael", "Jennifer", "David", "Lisa", "Robert", "Karen" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };

        var firstName = firstNames[_random.Next(firstNames.Length)];
        var lastName = lastNames[_random.Next(lastNames.Length)];
        var title = titles[_random.Next(titles.Length)];

        return new PrescriberInfo
        {
            ProviderId = $"PROV{_random.Next(100000, 999999)}",
            Name = $"{title} {firstName} {lastName}",
            DEANumber = GenerateDEANumber(),
            NPI = GenerateNPI(),
            Specialty = specialties[_random.Next(specialties.Length)],
            PhoneNumber = GeneratePhoneNumber(),
            Address = GenerateAddress()
        };
    }

    private PharmacyInfo GeneratePharmacy()
    {
        var pharmacyChains = new[]
        {
            "CVS Pharmacy", "Walgreens", "Rite Aid", "Walmart Pharmacy", "Kroger Pharmacy",
            "Safeway Pharmacy", "Publix Pharmacy", "Meijer Pharmacy", "Independent Pharmacy"
        };

        var chainName = pharmacyChains[_random.Next(pharmacyChains.Length)];
        var isChain = chainName != "Independent Pharmacy";
        var location = isChain ? GenerateLocation() : "";
        var pharmacyName = isChain ? $"{chainName} #{_random.Next(1000, 9999)}" : GenerateIndependentPharmacyName();

        return new PharmacyInfo
        {
            PharmacyId = $"PHARM{_random.Next(10000, 99999)}",
            Name = isChain ? $"{chainName} {location}" : pharmacyName,
            NCPDP = GenerateNCPDP(),
            DEANumber = GenerateDEANumber(),
            PhoneNumber = GeneratePhoneNumber(),
            Address = GenerateAddress(),
            PharmacistInCharge = GeneratePharmacistName(),
            Is24Hour = _random.NextDouble() < 0.1,
            ChainName = isChain ? chainName : null
        };
    }

    private string GenerateLocation()
    {
        var locations = new[] { "Main St", "Shopping Center", "Medical Plaza", "Downtown", "Westside", "Eastside" };
        return locations[_random.Next(locations.Length)];
    }

    private string GenerateIndependentPharmacyName()
    {
        var prefixes = new[] { "Community", "Family", "Care", "Health", "Medical", "Professional" };
        var suffixes = new[] { "Pharmacy", "Drug Store", "Apothecary", "Medications" };
        var prefix = prefixes[_random.Next(prefixes.Length)];
        var suffix = suffixes[_random.Next(suffixes.Length)];
        return $"{prefix} {suffix}";
    }

    private string GenerateDEANumber()
    {
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var letter1 = letters[_random.Next(letters.Length)];
        var letter2 = letters[_random.Next(letters.Length)];
        var digits = _random.Next(1000000, 9999999);
        return $"{letter1}{letter2}{digits:D7}";
    }

    private string GenerateNPI()
    {
        // NPI is 10 digits starting with 1 or 2
        var prefix = _random.Next(2) + 1;
        var suffix = _random.Next(100000000, 999999999);
        return $"{prefix}{suffix:D9}";
    }

    private string GenerateNCPDP()
    {
        return _random.Next(1000000, 9999999).ToString("D7");
    }

    private string GeneratePhoneNumber()
    {
        var areaCode = _random.Next(200, 999);
        var exchange = _random.Next(200, 999);
        var number = _random.Next(1000, 9999);
        return $"({areaCode:D3}) {exchange:D3}-{number:D4}";
    }

    private string GenerateAddress()
    {
        var streetNumber = _random.Next(1, 9999);
        var streetNames = new[] { "Main St", "Oak Ave", "First St", "Medical Dr", "Health Blvd", "Care Way" };
        var streetName = streetNames[_random.Next(streetNames.Length)];
        return $"{streetNumber} {streetName}";
    }

    private string GeneratePharmacistName()
    {
        var firstNames = new[] { "John", "Sarah", "Michael", "Jennifer", "David", "Lisa", "Robert", "Karen" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
        var firstName = firstNames[_random.Next(firstNames.Length)];
        var lastName = lastNames[_random.Next(lastNames.Length)];
        return $"PharmD {firstName} {lastName}";
    }

    private string GeneratePrescriptionNumber()
    {
        var formats = new[] { "RX{0:D8}", "P{0:D9}", "{0:D10}" };
        var format = formats[_random.Next(formats.Length)];
        var number = _random.Next(10000000, 999999999);
        return string.Format(format, number);
    }

    private decimal GenerateQuantity(MedicationData medication)
    {
        return medication.DosageForm switch
        {
            "liquid" => _random.Next(60, 480), // mL
            "tablet" or "capsule" => medication.PrescribingInfo.TypicalQuantity,
            "cream" or "ointment" => _random.Next(15, 60), // grams
            "injection" => _random.Next(1, 10), // vials
            _ => _random.Next(30, 90)
        };
    }

    private int GenerateRefills(MedicationData medication)
    {
        if (medication.IsControlledSubstance)
        {
            return medication.DEASchedule switch
            {
                "II" => 0, // No refills for Schedule II
                "III" or "IV" => _random.Next(0, 6), // Up to 5 refills
                "V" => _random.Next(0, 12), // Up to 11 refills
                _ => _random.Next(0, 6)
            };
        }
        return medication.PrescribingInfo.TypicalRefills;
    }

    private string GenerateDirections(MedicationData medication)
    {
        if (medication.PrescribingInfo.CommonSigs.Length > 0)
        {
            return medication.PrescribingInfo.CommonSigs[_random.Next(medication.PrescribingInfo.CommonSigs.Length)];
        }

        var defaultSigs = new[]
        {
            "Take 1 tablet by mouth once daily",
            "Take 1 tablet by mouth twice daily",
            "Take 1 tablet by mouth three times daily",
            "Take 1-2 tablets by mouth as needed for pain",
            "Apply thin layer to affected area twice daily"
        };

        return defaultSigs[_random.Next(defaultSigs.Length)];
    }

    private DateTime GenerateOrderDateTime(DateRange? range = null)
    {
        var defaultRange = new DateRange(DateTime.Now.AddDays(-30), DateTime.Now);
        var dateRange = range ?? defaultRange;
        
        var days = (dateRange.End - dateRange.Start).Days;
        var randomDays = _random.Next(days + 1);
        var baseDate = dateRange.Start.AddDays(randomDays);
        
        // Add random time during business hours (8 AM - 6 PM)
        var hour = _random.Next(8, 18);
        var minute = _random.Next(0, 60);
        
        return baseDate.Date.AddHours(hour).AddMinutes(minute);
    }

    private string? GenerateNotes()
    {
        if (_random.NextDouble() < 0.3) // 30% chance of notes
        {
            var notes = new[]
            {
                "Patient allergic to penicillin",
                "Take with food",
                "Do not take with dairy products",
                "May cause drowsiness",
                "Patient counseled on side effects",
                "Follow up in 2 weeks",
                "Monitor blood pressure"
            };
            return notes[_random.Next(notes.Length)];
        }
        return null;
    }

    private string GeneratePriorAuthNumber()
    {
        return $"PA{_random.Next(100000, 999999)}";
    }
}

/// <summary>
/// Contains realistic medication data sets.
/// </summary>
internal class MedicationDataSets
{
    public MedicationData[] CommonMedications { get; } = new[]
    {
        // Cardiovascular
        new MedicationData
        {
            GenericName = "lisinopril",
            BrandName = "Prinivil",
            Strength = "10",
            StrengthUnits = "mg",
            DosageForm = "tablet",
            Route = "oral",
            TherapeuticCategory = "ACE Inhibitor",
            PrescribingInfo = new PrescribingInfo
            {
                TypicalDose = "10mg",
                TypicalFrequency = "once daily",
                TypicalQuantity = 30,
                TypicalDaysSupply = 30,
                TypicalRefills = 5,
                Indications = new[] { "Hypertension", "Heart failure" },
                CommonSigs = new[] { "Take 1 tablet by mouth once daily", "Take 1 tablet by mouth twice daily" }
            }
        },
        
        // Diabetes
        new MedicationData
        {
            GenericName = "metformin",
            BrandName = "Glucophage",
            Strength = "500",
            StrengthUnits = "mg",
            DosageForm = "tablet",
            Route = "oral",
            TherapeuticCategory = "Antidiabetic",
            PrescribingInfo = new PrescribingInfo
            {
                TypicalDose = "500mg",
                TypicalFrequency = "twice daily",
                TypicalQuantity = 60,
                TypicalDaysSupply = 30,
                TypicalRefills = 5,
                Indications = new[] { "Type 2 diabetes" },
                CommonSigs = new[] { "Take 1 tablet by mouth twice daily with meals" }
            }
        },

        // Pain Management
        new MedicationData
        {
            GenericName = "oxycodone",
            BrandName = "OxyContin",
            Strength = "5",
            StrengthUnits = "mg",
            DosageForm = "tablet",
            Route = "oral",
            TherapeuticCategory = "Opioid Analgesic",
            DEASchedule = "II",
            PrescribingInfo = new PrescribingInfo
            {
                TypicalDose = "5mg",
                TypicalFrequency = "every 4-6 hours",
                TypicalQuantity = 20,
                TypicalDaysSupply = 7,
                TypicalRefills = 0,
                Indications = new[] { "Severe pain" },
                CommonSigs = new[] { "Take 1 tablet by mouth every 4-6 hours as needed for pain" }
            }
        },

        // Antibiotics
        new MedicationData
        {
            GenericName = "amoxicillin",
            BrandName = "Amoxil",
            Strength = "500",
            StrengthUnits = "mg",
            DosageForm = "capsule",
            Route = "oral",
            TherapeuticCategory = "Antibiotic",
            PrescribingInfo = new PrescribingInfo
            {
                TypicalDose = "500mg",
                TypicalFrequency = "three times daily",
                TypicalQuantity = 21,
                TypicalDaysSupply = 7,
                TypicalRefills = 0,
                Indications = new[] { "Bacterial infections" },
                CommonSigs = new[] { "Take 1 capsule by mouth three times daily for 7 days" }
            }
        },

        // Mental Health
        new MedicationData
        {
            GenericName = "sertraline",
            BrandName = "Zoloft",
            Strength = "50",
            StrengthUnits = "mg",
            DosageForm = "tablet",
            Route = "oral",
            TherapeuticCategory = "Antidepressant",
            PrescribingInfo = new PrescribingInfo
            {
                TypicalDose = "50mg",
                TypicalFrequency = "once daily",
                TypicalQuantity = 30,
                TypicalDaysSupply = 30,
                TypicalRefills = 5,
                Indications = new[] { "Depression", "Anxiety" },
                CommonSigs = new[] { "Take 1 tablet by mouth once daily" }
            }
        }
    };
}