// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation.Algorithmic.Data;
using Pidgeon.Core.Generation.Types;
using Microsoft.Extensions.Logging;

namespace Pidgeon.Core.Generation.Algorithmic;

/// <summary>
/// Core algorithmic generation service (free tier).
/// Generates realistic healthcare data using embedded datasets without AI enhancement.
/// Deterministic when seeded, realistic patterns based on healthcare demographics.
/// </summary>
public class AlgorithmicGenerationService : IGenerationService
{
    private readonly ILogger<AlgorithmicGenerationService> _logger;
    private readonly Random _random;

    public AlgorithmicGenerationService(ILogger<AlgorithmicGenerationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();
    }

    /// <summary>
    /// Template method for executing generation operations with consistent error handling and seeding.
    /// </summary>
    private Result<T> ExecuteGeneration<T>(GenerationOptions options, int seedOffset, string entityType, Func<Random, T> generateFunc)
    {
        try
        {
            // Use seed for deterministic generation if provided
            var random = options.Seed.HasValue ? new Random(options.Seed.Value + seedOffset) : _random;
            
            var result = generateFunc(random);
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate {EntityType}", entityType);
            return Result<T>.Failure($"{char.ToUpper(entityType[0])}{entityType[1..]} generation failed: {ex.Message}");
        }
    }

    public Result<Patient> GeneratePatient(GenerationOptions options)
    {
        return ExecuteGeneration(options, 0, "patient", (random) =>
        {
            // Generate culturally-consistent name
            var (firstName, lastName, gender) = HealthcareNames.GenerateRandomName(random);
            
            // Generate age with healthcare-realistic distribution
            var age = GenerateAgeForPatientType(random);
            
            // Generate other demographics
            var mrn = GenerateMRN(random);
            var ssn = GenerateSSN(random);
            var dob = CalculateDateOfBirth(age);
            var phoneNumber = GeneratePhoneNumber(random);
            var address = GenerateAddress(random);
            
            var patient = new Patient
            {
                Id = mrn, // Use MRN as the primary ID
                MedicalRecordNumber = mrn,
                Name = PersonName.Create(lastName, firstName),
                Gender = ParseGender(gender),
                BirthDate = dob,
                SocialSecurityNumber = ssn,
                PhoneNumber = phoneNumber,
                Address = address
            };

            _logger.LogDebug("Generated patient: {MRN} - {Name}, Age {Age}", 
                mrn, $"{firstName} {lastName}", age);
            
            return patient;
        });
    }

    public Result<Medication> GenerateMedication(GenerationOptions options)
    {
        return ExecuteGeneration(options, 0, "medication", (random) =>
        {
            // Get available medications for generation
            var availableMeds = GetAvailableMedications();
            
            if (!availableMeds.Any())
            {
                throw new InvalidOperationException("No medications available for specified patient type");
            }

            var medData = availableMeds[random.Next(availableMeds.Count)];
            
            var medication = new Medication
            {
                Id = Guid.NewGuid().ToString(),
                Name = medData.BrandName ?? medData.GenericName,
                GenericName = medData.GenericName,
                Strength = medData.AvailableStrengths.FirstOrDefault() ?? "Unknown",
                DrugClass = medData.TherapeuticClass,
                ControlledSchedule = ParseControlledSchedule(medData.IsControlledSubstance)
            };

            _logger.LogDebug("Generated medication: {Name} ({Generic})", 
                medication.Name, medication.GenericName);
                
            return medication;
        });
    }

    public Result<Prescription> GeneratePrescription(GenerationOptions options)
    {
        return ExecuteGeneration(options, 1000, "prescription", (random) =>
        {
            // Generate patient and medication for this prescription
            var patientResult = GeneratePatient(options);
            if (!patientResult.IsSuccess)
                throw new InvalidOperationException($"Failed to generate patient: {patientResult.Error}");

            var medicationResult = GenerateMedication(options);
            if (!medicationResult.IsSuccess)
                throw new InvalidOperationException($"Failed to generate medication: {medicationResult.Error}");
            
            var prescription = new Prescription
            {
                Id = GeneratePrescriptionNumber(random),
                Patient = patientResult.Value,
                Medication = medicationResult.Value,
                Prescriber = GenerateProvider(random),
                Dosage = GenerateDosageInstructions(random, medicationResult.Value),
                DatePrescribed = GeneratePrescriptionDate(random),
                Instructions = GenerateSpecialInstructions(random, medicationResult.Value)
            };

            _logger.LogDebug("Generated prescription: {RxId} for {Patient}", 
                prescription.Id, prescription.Patient.Name.DisplayName);
                
            return prescription;
        });
    }

    public Result<Encounter> GenerateEncounter(GenerationOptions options)
    {
        return ExecuteGeneration(options, 2000, "encounter", (random) =>
        {
            var patientResult = GeneratePatient(options);
            if (!patientResult.IsSuccess)
                throw new InvalidOperationException($"Failed to generate patient: {patientResult.Error}");
            
            var encounter = new Encounter
            {
                Id = GenerateEncounterNumber(random),
                Patient = patientResult.Value,
                Provider = GenerateProvider(random),
                Type = GenerateEncounterTypeEnum(random),
                Status = EncounterStatus.Finished,
                StartTime = GenerateEncounterDate(random),
                Location = GenerateFacilityName(random),
                Priority = EncounterPriority.Routine
            };

            _logger.LogDebug("Generated encounter: {EncounterId} for {Patient}", 
                encounter.Id, encounter.Patient.Name.DisplayName);
                
            return encounter;
        });
    }

    public GenerationServiceInfo GetServiceInfo()
    {
        return new GenerationServiceInfo
        {
            ServiceTier = "Core (Free)",
            AIAvailable = false,
            AvailablePatientTypes = new[] { PatientType.General },
            AvailableVendorProfiles = new[] { VendorProfile.Generic },
            Dataset = new DatasetInfo
            {
                MedicationCount = HealthcareMedications.Medications.Length,
                FirstNameCount = HealthcareNames.FirstNames.Length,
                SurnameCount = HealthcareNames.LastNames.Length,
                Freshness = "Static",
                CoveragePercentage = 75,
                SpecialtyCategories = new[] { "Common Medications", "Basic Demographics" }
            },
            Limits = new UsageLimits
            {
                MaxGenerationsPerPeriod = 10,
                LimitPeriod = "Session",
                BatchProcessingAvailable = false,
                CloudAPIAvailable = false,
                MaxTeamSize = 1
            }
        };
    }

    #region Private Generation Methods

    private int GenerateAgeForPatientType(Random random)
    {
        // Generate weighted age distribution based on healthcare utilization patterns
        var ageRanges = new (int min, int max, double weight)[]
        {
            (0, 17, 0.15),    // Pediatric
            (18, 34, 0.20),   // Young adult  
            (35, 54, 0.30),   // Middle age (highest utilization)
            (55, 64, 0.20),   // Pre-geriatric
            (65, 95, 0.15)    // Geriatric
        };
        
        var totalWeight = ageRanges.Sum(r => r.weight);
        var randomValue = random.NextDouble() * totalWeight;
        var cumulativeWeight = 0.0;
        
        foreach (var (min, max, weight) in ageRanges)
        {
            cumulativeWeight += weight;
            if (randomValue <= cumulativeWeight)
                return random.Next(min, max + 1);
        }
        
        return random.Next(18, 85); // Fallback
    }

    private string GenerateMRN(Random random)
    {
        // Generic format: 8-digit numeric for free tier
        return $"{random.Next(10000000, 99999999)}";
    }

    private string GenerateSSN(Random random)
    {
        // Generate realistic-looking but fake SSN (avoid actual SSN ranges)
        var area = random.Next(900, 999); // Use 900+ range to avoid real SSNs
        var group = random.Next(10, 99);
        var serial = random.Next(1000, 9999);
        return $"{area}-{group:D2}-{serial:D4}";
    }

    private DateTime CalculateDateOfBirth(int age)
    {
        var today = DateTime.Today;
        return today.AddYears(-age).AddDays(Random.Shared.Next(-365, 365));
    }

    private string GeneratePhoneNumber(Random random)
    {
        var area = random.Next(200, 999);
        var exchange = random.Next(200, 999);
        var number = random.Next(1000, 9999);
        return $"({area}) {exchange}-{number}";
    }

    private Address GenerateAddress(Random random)
    {
        var streetNumbers = new[] { "123", "456", "789", "1010", "2525", "3030" };
        var streetNames = new[] { "Main St", "Oak Ave", "Elm St", "Park Dr", "First Ave", "Second St" };
        var cities = new[] { "Springfield", "Franklin", "Georgetown", "Clinton", "Madison", "Washington" };
        var states = new[] { "CA", "TX", "FL", "NY", "PA", "IL", "OH", "GA", "NC", "MI" };

        return new Address
        {
            Street1 = $"{streetNumbers[random.Next(streetNumbers.Length)]} {streetNames[random.Next(streetNames.Length)]}",
            City = cities[random.Next(cities.Length)],
            State = states[random.Next(states.Length)],
            PostalCode = $"{random.Next(10000, 99999)}",
            Country = "US"
        };
    }

    private List<MedicationData> GetAvailableMedications()
    {
        return HealthcareMedications.Medications
            .Where(m => m.AppropriateAgeGroups.HasFlag(AgeGroup.Adult))
            .ToList();
    }

    private Provider GenerateProvider(Random random)
    {
        var (firstName, lastName, _) = HealthcareNames.GenerateRandomName(random);
        var npi = $"1{random.Next(100000000, 999999999)}"; // NPI format
        var specialties = new[] { "Family Medicine", "Internal Medicine", "Cardiology", "Pediatrics", "Psychiatry" };
        
        return new Provider
        {
            Id = npi,
            Name = PersonName.Create(lastName, firstName),
            NpiNumber = npi,
            Specialty = specialties[random.Next(specialties.Length)]
        };
    }

    private int GenerateQuantity(Random random, Medication medication)
    {
        return medication.DosageForm switch
        {
            DosageForm.Tablet or DosageForm.Capsule => new[] { 30, 60, 90 }[random.Next(3)],
            DosageForm.Liquid => new[] { 100, 150, 200, 240, 300 }[random.Next(5)],
            DosageForm.Injectable => new[] { 1, 2, 3, 5 }[random.Next(4)],
            DosageForm.Cream or DosageForm.Ointment => new[] { 15, 30, 45, 60 }[random.Next(4)],
            _ => 30
        };
    }

    private int GenerateDaysSupply(Random random, Medication medication)
    {
        return medication.RequiresDeaRegistration() ? 
            new[] { 7, 14, 30 }[random.Next(3)] :
            new[] { 30, 60, 90 }[random.Next(3)];
    }

    private int GenerateRefills(Random random, Medication medication)
    {
        return medication.RequiresDeaRegistration() ?
            random.Next(0, 6) :
            random.Next(0, 12);
    }

    private string GenerateSpecialInstructions(Random random, Medication medication)
    {
        var instructions = new[] 
        {
            "Take with food",
            "Take on empty stomach", 
            "Do not crush or chew",
            "May cause drowsiness",
            "Avoid alcohol",
            "Take at bedtime",
            "Take with plenty of water",
            null
        };
        
        return instructions[random.Next(instructions.Length)];
    }

    private DateTime GeneratePrescriptionDate(Random random)
    {
        return DateTime.Today.AddDays(-random.Next(0, 90));
    }

    private string GeneratePrescriptionNumber(Random random)
    {
        return $"RX{random.Next(1000000, 9999999)}";
    }

    private string GenerateEncounterNumber(Random random)
    {
        return $"E{DateTime.Now:yyyyMMdd}{random.Next(1000, 9999)}";
    }

    private DateTime GenerateEncounterDate(Random random)
    {
        return DateTime.Today.AddDays(-random.Next(0, 30));
    }


    private string GenerateFacilityName(Random random)
    {
        return new[] 
        { 
            "Primary Care Clinic", 
            "Family Health Center", 
            "Medical Associates",
            "City General Hospital",
            "Regional Medical Center",
            "Community Health Center"
        }[random.Next(6)];
    }

    private DateTime? GenerateAdmissionDate(Random random)
    {
        return random.Next(0, 4) == 0 ? DateTime.Today.AddDays(-random.Next(0, 7)) : null;
    }

    private DateTime? GenerateDischargeDate(Random random)
    {
        return random.Next(0, 6) == 0 ? DateTime.Today.AddDays(-random.Next(0, 3)) : null;
    }

    private static Gender ParseGender(string genderString)
    {
        return genderString?.ToUpperInvariant() switch
        {
            "M" => Gender.Male,
            "F" => Gender.Female,
            _ => Gender.Unknown
        };
    }

    private static EncounterType GenerateEncounterTypeEnum(Random random)
    {
        return new[] 
        { 
            EncounterType.Outpatient, 
            EncounterType.Inpatient,
            EncounterType.Emergency,
            EncounterType.Observation
        }[random.Next(4)];
    }


    private DosageInstructions GenerateDosageInstructions(Random random, Medication medication)
    {
        var frequencies = new[] { "QD", "BID", "TID", "QID", "Q6H", "Q8H", "Q12H" };
        var doses = new[] { "0.5", "1", "1.5", "2", "2.5" };
        var units = medication.DosageForm switch
        {
            DosageForm.Tablet or DosageForm.Capsule => "tablet",
            DosageForm.Liquid => "mL",
            DosageForm.Injectable => "unit",
            DosageForm.Cream or DosageForm.Ointment => "gram",
            _ => "dose"
        };

        var route = medication.DosageForm switch
        {
            DosageForm.Tablet or DosageForm.Capsule or DosageForm.Liquid => RouteOfAdministration.Oral,
            DosageForm.Injectable => RouteOfAdministration.Subcutaneous,
            DosageForm.Topical or DosageForm.Cream or DosageForm.Ointment => RouteOfAdministration.Topical,
            _ => RouteOfAdministration.Oral
        };

        return new DosageInstructions
        {
            Dose = doses[random.Next(doses.Length)],
            DoseUnit = units,
            Frequency = frequencies[random.Next(frequencies.Length)],
            Route = route,
            Quantity = GenerateQuantity(random, medication),
            DaysSupply = GenerateDaysSupply(random, medication),
            Refills = GenerateRefills(random, medication)
        };
    }

    private static DosageForm? ParseDosageForm(string? form)
    {
        return form?.ToLowerInvariant() switch
        {
            "tablet" => DosageForm.Tablet,
            "capsule" => DosageForm.Capsule,
            "liquid" => DosageForm.Liquid,
            "injection" => DosageForm.Injectable,
            "cream" => DosageForm.Cream,
            "ointment" => DosageForm.Ointment,
            "topical" => DosageForm.Topical,
            "inhaler" => DosageForm.Inhaler,
            _ => null
        };
    }

    private static ControlledSubstanceSchedule? ParseControlledSchedule(bool isControlled)
    {
        return isControlled ? ControlledSubstanceSchedule.ScheduleII : null;
    }

    #endregion
}