// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation.Algorithmic.Data;
using Pidgeon.Core.Generation.Types;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Application.Interfaces.Reference;
using Pidgeon.Core.Application.Interfaces.Generation;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Domain.Configuration;
using Microsoft.Extensions.Logging;

namespace Pidgeon.Core.Application.Services.Generation;

/// <summary>
/// Core generation service with strategy pattern support.
/// Supports both algorithmic generation (free tier) and AI enhancement (paid tiers).
/// Strategy selection controlled by GenerationOptions.UseAI flag.
/// </summary>
internal class GenerationService : IGenerationService
{
    private readonly ILogger<GenerationService> _logger;
    private readonly IDemographicsDataService _demographicsService;
    private readonly IConstraintResolver _constraintResolver;
    private readonly ILockSessionService _lockSessionService;
    private readonly IFieldPathResolver _fieldPathResolver;
    private readonly Random _random;

    public GenerationService(
        ILogger<GenerationService> logger,
        IDemographicsDataService demographicsService,
        IConstraintResolver constraintResolver,
        ILockSessionService lockSessionService,
        IFieldPathResolver fieldPathResolver)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _demographicsService = demographicsService ?? throw new ArgumentNullException(nameof(demographicsService));
        _constraintResolver = constraintResolver ?? throw new ArgumentNullException(nameof(constraintResolver));
        _lockSessionService = lockSessionService ?? throw new ArgumentNullException(nameof(lockSessionService));
        _fieldPathResolver = fieldPathResolver ?? throw new ArgumentNullException(nameof(fieldPathResolver));
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

    /// <summary>
    /// Template method for executing async generation operations with consistent error handling and seeding.
    /// </summary>
    private Result<T> ExecuteGeneration<T>(GenerationOptions options, int seedOffset, string entityType, Func<Random, Task<T>> generateFunc)
    {
        try
        {
            // Use seed for deterministic generation if provided
            var random = options.Seed.HasValue ? new Random(options.Seed.Value + seedOffset) : _random;

            var result = generateFunc(random).GetAwaiter().GetResult();
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
        return ExecuteGeneration(options, 0, "patient", async (random) => await GeneratePatientLogicAsync(random, options));
    }

    private async Task<Patient> GeneratePatientLogicAsync(Random random, GenerationOptions options)
    {
        // Generate administrative sex using HL7 table 0001 (constraint-aware)
        var genderResult = await _constraintResolver.GenerateConstrainedValueAsync(
            "PID.8",
            new FieldConstraints
            {
                TableReference = "0001",
                DataType = "IS",
                MaxLength = 1,
                Required = false
            },
            random);

        var genderCode = genderResult.IsSuccess ? genderResult.Value?.ToString() : "U";
        var gender = ParseGender(genderCode ?? "U");

        // Generate patient identifier with HL7 constraints (PID.3)
        var identifierResult = await _constraintResolver.GenerateConstrainedValueAsync(
            "PID.3",
            new FieldConstraints
            {
                DataType = "CX",
                Required = true,
                Pattern = @"^\d{6,10}$",
                MaxLength = 10
            },
            random);

        var mrn = identifierResult.IsSuccess ? identifierResult.Value?.ToString() : GenerateMRN(random);

        // Generate birth date with HL7 date constraints (PID.7)
        var dobResult = await _constraintResolver.GenerateConstrainedValueAsync(
            "PID.7",
            new FieldConstraints
            {
                DataType = "TS",
                DateTime = new DateTimeConstraints(
                    DateTime.Today.AddYears(-120),
                    DateTime.Today,
                    "yyyyMMdd"),
                Required = false
            },
            random);

        DateTime? dob = null;
        if (dobResult.IsSuccess && DateTime.TryParseExact(
            dobResult.Value?.ToString(),
            "yyyyMMdd",
            null,
            System.Globalization.DateTimeStyles.None,
            out var parsedDate))
        {
            dob = parsedDate;
        }
        else
        {
            // Fallback to calculated date
            var age = GenerateAgeForPatientType(random);
            dob = CalculateDateOfBirth(age, random);
        }

        // Use demographics service for non-constrained fields
        var (firstName, lastName, _) = await _demographicsService.GenerateRandomNameAsync(random);
        var address = await _demographicsService.GenerateRandomAddressAsync(random);
        var phoneNumber = await GeneratePhoneNumberAsync(random);
        var ssn = GenerateSSN(random);

        var patient = new Patient
        {
            Id = mrn ?? "UNK", // Use MRN as the primary ID
            MedicalRecordNumber = mrn,
            Name = PersonName.Create(lastName, firstName),
            Gender = gender,
            BirthDate = dob,
            SocialSecurityNumber = ssn,
            PhoneNumber = phoneNumber,
            Address = address
        };

        var calculatedAge = dob.HasValue ? DateTime.Today.Year - dob.Value.Year : 0;
        _logger.LogDebug("Generated patient using demographic data: {MRN} - {Name}, Age {Age}",
            mrn, $"{firstName} {lastName}", calculatedAge);

        // Apply locked values from session if specified
        patient = await ApplyLockedValuesAsync(patient, options, "Patient");

        return patient;
    }

    public Result<Medication> GenerateMedication(GenerationOptions options)
    {
        return ExecuteGeneration(options, 0, "medication", GenerateMedicationLogic);
    }

    private Medication GenerateMedicationLogic(Random random)
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
            Id = GenerateDeterministicId(random, "MED"),
            Name = medData.BrandName ?? medData.GenericName,
            GenericName = medData.GenericName,
            Strength = medData.AvailableStrengths.FirstOrDefault() ?? "Unknown",
            DrugClass = medData.TherapeuticClass,
            ControlledSchedule = ParseControlledSchedule(medData.IsControlledSubstance)
        };

        _logger.LogDebug("Generated medication: {Name} ({Generic})", 
            medication.Name, medication.GenericName);
            
        return medication;
    }

    public Result<Prescription> GeneratePrescription(GenerationOptions options)
    {
        return ExecuteGeneration(options, 1000, "prescription", async (random) =>
        {
            // Generate patient and medication using seeded random for consistency
            var patient = await GeneratePatientLogicAsync(random, options);
            var medication = GenerateMedicationLogic(random);

            var prescription = new Prescription
            {
                Id = GeneratePrescriptionNumber(random),
                Patient = patient,
                Medication = medication,
                Prescriber = await GenerateProviderAsync(random, options),
                Dosage = GenerateDosageInstructions(random, medication),
                DatePrescribed = GeneratePrescriptionDate(random),
                Instructions = GenerateSpecialInstructions(random, medication)
            };

            _logger.LogDebug("Generated prescription using demographic data: {RxId} for {Patient}",
                prescription.Id, prescription.Patient.Name.DisplayName);

            return prescription;
        });
    }

    public Result<Encounter> GenerateEncounter(GenerationOptions options)
    {
        return ExecuteGeneration(options, 2000, "encounter", async (random) =>
        {
            var patient = await GeneratePatientLogicAsync(random, options);

            var encounter = new Encounter
            {
                Id = GenerateEncounterNumber(random),
                Patient = patient,
                Provider = await GenerateProviderAsync(random, options),
                Type = GenerateEncounterTypeEnum(random),
                Status = EncounterStatus.Finished,
                StartTime = GenerateEncounterDate(random),
                Location = GenerateFacilityName(random),
                Priority = EncounterPriority.Routine
            };

            // Apply locked values from session if specified
            encounter = await ApplyLockedValuesAsync(encounter, options, "Encounter");

            _logger.LogDebug("Generated encounter using demographic data: {EncounterId} for {Patient}",
                encounter.Id, encounter.Patient.Name.DisplayName);

            return encounter;
        });
    }

    public Result<Provider> GenerateProvider(GenerationOptions options)
    {
        return ExecuteGeneration(options, 3000, "provider", async (random) => await GenerateProviderAsync(random, options));
    }

    public Result<ObservationResult> GenerateObservationResult(GenerationOptions options)
    {
        return ExecuteGeneration(options, 4000, "observation", GenerateObservationResult);
    }

    public GenerationServiceInfo GetServiceInfo()
    {
        // Get demographic counts from data service (blocking here for interface compatibility)
        var firstNames = _demographicsService.GetFirstNamesAsync().GetAwaiter().GetResult();
        var lastNames = _demographicsService.GetLastNamesAsync().GetAwaiter().GetResult();

        return new GenerationServiceInfo
        {
            ServiceTier = "Core (Free)",
            AIAvailable = false,
            AvailablePatientTypes = new[] { PatientType.General },
            AvailableVendorProfiles = new[] { VendorProfile.Generic },
            Dataset = new DatasetInfo
            {
                MedicationCount = HealthcareMedications.Medications.Length,
                FirstNameCount = firstNames.Count,
                SurnameCount = lastNames.Count,
                Freshness = "Data-Driven",
                CoveragePercentage = 95,
                SpecialtyCategories = new[] { "Common Medications", "Demographic Data", "Geographic Data", "Healthcare Specialties" }
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

    /// <summary>
    /// Applies locked field values from a session to an entity based on semantic path mappings.
    /// Returns a new instance with locked values applied.
    /// </summary>
    private async Task<T> ApplyLockedValuesAsync<T>(T entity, GenerationOptions options, string messageType) where T : class
    {
        if (string.IsNullOrEmpty(options.LockSessionName))
        {
            return entity;
        }

        try
        {
            var sessionResult = await _lockSessionService.GetSessionAsync(options.LockSessionName, CancellationToken.None);
            if (sessionResult.IsFailure)
            {
                _logger.LogWarning("Could not load lock session {SessionName}: {Error}",
                    options.LockSessionName, sessionResult.Error.Message);
                return entity;
            }

            var session = sessionResult.Value;
            if (!session.LockedValues.Any())
            {
                return entity;
            }

            _logger.LogDebug("Applying {Count} locked values from session {SessionName} to {EntityType}",
                session.LockedValues.Count, options.LockSessionName, typeof(T).Name);

            var modifiedEntity = entity;
            foreach (var lockedValue in session.LockedValues)
            {
                modifiedEntity = await ApplySemanticPathValue(modifiedEntity, lockedValue.FieldPath, lockedValue.Value, messageType);
            }

            return modifiedEntity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply locked values from session {SessionName}", options.LockSessionName);
            return entity; // Return entity unchanged on error
        }
    }

    /// <summary>
    /// Applies a single semantic path value to an entity, returning a modified copy.
    /// </summary>
    private async Task<T> ApplySemanticPathValue<T>(T entity, string semanticPath, string value, string messageType) where T : class
    {
        try
        {
            // For Patient entities, map common semantic paths
            if (entity is Patient patient)
            {
                var modifiedPatient = await ApplyPatientSemanticPath(patient, semanticPath, value);
                return (T)(object)modifiedPatient;
            }

            // For Provider entities, map provider semantic paths
            if (entity is Provider provider)
            {
                var modifiedProvider = await ApplyProviderSemanticPath(provider, semanticPath, value);
                return (T)(object)modifiedProvider;
            }

            // For Encounter entities, map encounter semantic paths
            if (entity is Encounter encounter)
            {
                var modifiedEncounter = await ApplyEncounterSemanticPath(encounter, semanticPath, value);
                return (T)(object)modifiedEncounter;
            }

            _logger.LogDebug("No semantic path mapping available for entity type {EntityType} and path {SemanticPath}",
                typeof(T).Name, semanticPath);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply semantic path {SemanticPath} = '{Value}' to {EntityType}",
                semanticPath, value, typeof(T).Name);
            return entity;
        }
    }

    /// <summary>
    /// Applies semantic path values to Patient entity properties, returning a modified copy.
    /// </summary>
    private async Task<Patient> ApplyPatientSemanticPath(Patient patient, string semanticPath, string value)
    {
        await Task.Yield(); // Make async

        return semanticPath.ToLowerInvariant() switch
        {
            "patient.mrn" => patient with
            {
                MedicalRecordNumber = value,
                Id = value // MRN often used as primary ID
            },

            "patient.lastname" => patient with
            {
                Name = PersonName.Create(value, patient.Name?.Given)
            },

            "patient.firstname" => patient with
            {
                Name = PersonName.Create(patient.Name?.Family, value)
            },

            "patient.dateofbirth" when DateTime.TryParseExact(value, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var dob) =>
                patient with { BirthDate = dob },

            "patient.dateofbirth" when DateTime.TryParse(value, out var dobAlt) =>
                patient with { BirthDate = dobAlt },

            "patient.sex" => patient with
            {
                Gender = value.ToUpperInvariant() switch
                {
                    "M" => Domain.Clinical.Entities.Gender.Male,
                    "F" => Domain.Clinical.Entities.Gender.Female,
                    _ => Domain.Clinical.Entities.Gender.Unknown
                }
            },

            "patient.phonenumber" => patient with { PhoneNumber = value },

            "patient.ssn" => patient with { SocialSecurityNumber = value },

            "patient.ethnicity" => patient,

            "patient.language" => patient,

            "patient.religion" => patient,

            "patient.maritalstatus" => patient,

            _ => patient
        };
    }

    /// <summary>
    /// Applies semantic path values to Provider entity properties, returning a modified copy.
    /// </summary>
    private async Task<Provider> ApplyProviderSemanticPath(Provider provider, string semanticPath, string value)
    {
        await Task.Yield(); // Make async

        return semanticPath.ToLowerInvariant() switch
        {
            "provider.id" => provider with
            {
                Id = value,
                NpiNumber = value // Often the same
            },

            "provider.lastname" => provider with
            {
                Name = PersonName.Create(value, provider.Name?.Given)
            },

            "provider.firstname" => provider with
            {
                Name = PersonName.Create(provider.Name?.Family, value)
            },

            "provider.npi" => provider,

            "provider.specialty" => provider,

            "provider.department" => provider,

            _ => provider
        };
    }

    /// <summary>
    /// Applies semantic path values to Encounter entity properties, returning a modified copy.
    /// </summary>
    private async Task<Encounter> ApplyEncounterSemanticPath(Encounter encounter, string semanticPath, string value)
    {
        await Task.Yield();

        return semanticPath.ToLowerInvariant() switch
        {
            "encounter.location" => encounter with { Location = value },

            "encounter.admissiondate" when DateTime.TryParse(value, out var admitDate) =>
                encounter,

            "encounter.class" => encounter,

            "encounter.room" => encounter,

            "encounter.bed" => encounter,

            "encounter.facility" => encounter with { Location = value },

            _ => encounter
        };
    }

    private string GenerateDeterministicId(Random random, string prefix)
    {
        var randomValue = random.Next(100000000, 999999999);
        return $"{prefix}{randomValue}";
    }

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

    private DateTime CalculateDateOfBirth(int age, Random random)
    {
        var today = DateTime.Today;
        return today.AddYears(-age).AddDays(random.Next(-365, 365));
    }

    private async Task<string> GeneratePhoneNumberAsync(Random random)
    {
        var phoneNumbers = await _demographicsService.GetPhoneNumbersAsync();

        if (phoneNumbers.Count > 0)
        {
            // Use realistic phone number patterns from our demographic datasets
            return phoneNumbers[random.Next(phoneNumbers.Count)];
        }

        // Fallback to algorithmic generation if no data available
        var area = random.Next(200, 999);
        var exchange = random.Next(200, 999);
        var number = random.Next(1000, 9999);
        return $"({area}) {exchange}-{number}";
    }


    private List<MedicationData> GetAvailableMedications()
    {
        return HealthcareMedications.Medications
            .Where(m => m.AppropriateAgeGroups.HasFlag(AgeGroup.Adult))
            .ToList();
    }

    private async Task<Provider> GenerateProviderAsync(Random random, GenerationOptions? options = null)
    {
        var (firstName, lastName, _) = await _demographicsService.GenerateRandomNameAsync(random);
        var npi = $"1{random.Next(100000000, 999999999)}"; // NPI format
        var specialties = new[] { "Family Medicine", "Internal Medicine", "Cardiology", "Pediatrics", "Psychiatry" };

        var provider = new Provider
        {
            Id = npi,
            Name = PersonName.Create(lastName, firstName),
            NpiNumber = npi,
            Specialty = specialties[random.Next(specialties.Length)]
        };

        // Apply locked values from session if specified
        if (options != null)
        {
            provider = await ApplyLockedValuesAsync(provider, options, "Provider");
        }

        return provider;
    }

    private ObservationResult GenerateObservationResult(Random random)
    {
        // Common lab tests with realistic values for multi-standard use
        var labTests = new[]
        {
            new { Name = "Complete Blood Count", Code = "CBC", Value = "WBC: 7.2, RBC: 4.5, Hgb: 14.1, Hct: 42.3", Units = "K/uL", Range = "4.0-11.0" },
            new { Name = "Basic Metabolic Panel", Code = "BMP", Value = "Na: 138, K: 4.2, Cl: 102, CO2: 24", Units = "mmol/L", Range = "135-145" },
            new { Name = "Lipid Panel", Code = "LIPID", Value = "Total: 180, HDL: 45, LDL: 110, Trig: 125", Units = "mg/dL", Range = "<200" },
            new { Name = "Hemoglobin A1c", Code = "HBA1C", Value = "6.8", Units = "%", Range = "<7.0" },
            new { Name = "Thyroid Stimulating Hormone", Code = "TSH", Value = "2.4", Units = "mIU/L", Range = "0.4-4.0" },
            new { Name = "Creatinine", Code = "CREAT", Value = "1.1", Units = "mg/dL", Range = "0.6-1.3" },
            new { Name = "Glucose", Code = "GLUC", Value = "95", Units = "mg/dL", Range = "70-99" }
        };

        var selectedTest = labTests[random.Next(labTests.Length)];
        var testId = $"LAB{random.Next(10000, 99999)}";
        
        return new ObservationResult
        {
            Id = testId,
            ObservationDescription = selectedTest.Name,
            ObservationCode = selectedTest.Code,
            Value = selectedTest.Value,
            Units = selectedTest.Units,
            ReferenceRange = selectedTest.Range,
            ResultStatus = "F", // Final
            ObservationDateTime = DateTime.Now.AddHours(-random.Next(1, 48)),
            CodingSystem = "LN", // LOINC coding system
            Category = "LAB"
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

    private string? GenerateSpecialInstructions(Random random, Medication medication)
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