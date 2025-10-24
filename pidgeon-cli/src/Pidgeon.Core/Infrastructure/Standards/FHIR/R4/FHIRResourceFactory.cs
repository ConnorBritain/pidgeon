// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;
using System.Text.Json;

namespace Pidgeon.Core.Infrastructure.Standards.FHIR.R4;

/// <summary>
/// Factory implementation for generating FHIR R4 standards-compliant resources.
/// Handles all JSON construction and FHIR-specific semantics.
/// </summary>
internal class FHIRResourceFactory : IFHIRResourceFactory
{
    private readonly Pidgeon.Core.Generation.IGenerationService _domainGenerationService;

    public FHIRResourceFactory(Pidgeon.Core.Generation.IGenerationService domainGenerationService)
    {
        _domainGenerationService = domainGenerationService;
    }

    public Result<string> GeneratePatient(Patient patient, GenerationOptions options)
    {
        try
        {
            var patientId = $"patient-{patient.MedicalRecordNumber}";
            
            var fhirPatient = new
            {
                resourceType = "Patient",
                id = patientId,
                identifier = new object[]
                {
                    new
                    {
                        use = "usual",
                        type = new
                        {
                            coding = new object[]
                            {
                                new
                                {
                                    system = "http://terminology.hl7.org/CodeSystem/v2-0203",
                                    code = "MR",
                                    display = "Medical Record Number"
                                }
                            }
                        },
                        system = "http://hospital.example.org",
                        value = patient.MedicalRecordNumber
                    }
                },
                name = new object[]
                {
                    new
                    {
                        use = "official",
                        family = patient.Name.Family,
                        given = new[] { patient.Name.Given ?? "Unknown" }
                    }
                },
                gender = patient.Gender?.ToString().ToLowerInvariant() ?? "unknown",
                birthDate = patient.BirthDate?.ToString("yyyy-MM-dd")
            };
            
            var json = JsonSerializer.Serialize(fhirPatient, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            
            return Result<string>.Success(json);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to generate FHIR Patient: {ex.Message}");
        }
    }

    public Result<string> GeneratePractitioner(Provider provider, GenerationOptions options)
    {
        try
        {
            var practitionerId = $"practitioner-{Guid.NewGuid():N}";
            
            var fhirPractitioner = new
            {
                resourceType = "Practitioner",
                id = practitionerId,
                active = true,
                name = new object[]
                {
                    new
                    {
                        use = "official",
                        family = provider.Name.Family,
                        given = new[] { provider.Name.Given ?? "Unknown" },
                        prefix = new[] { "Dr." },
                        text = $"Dr. {provider.Name.DisplayName}"
                    }
                },
                telecom = new object[]
                {
                    new
                    {
                        system = "phone",
                        value = provider.PhoneNumber ?? "+1-555-0123",
                        use = "work"
                    },
                    new
                    {
                        system = "email", 
                        value = provider.EmailAddress ?? $"{provider.Name.Given?.ToLower() ?? "provider"}.{provider.Name.Family?.ToLower() ?? "unknown"}@hospital.org",
                        use = "work"
                    }
                },
                address = new object[]
                {
                    new
                    {
                        use = "work",
                        type = "physical",
                        line = new[] { "123 Medical Center Dr" },
                        city = "Healthcare City",
                        state = "HC", 
                        postalCode = "12345",
                        country = "US"
                    }
                },
                gender = "unknown",
                qualification = new object[]
                {
                    new
                    {
                        identifier = new object[]
                        {
                            new
                            {
                                use = "official",
                                type = new
                                {
                                    coding = new object[]
                                    {
                                        new
                                        {
                                            system = "http://terminology.hl7.org/CodeSystem/v2-0203",
                                            code = "MD", 
                                            display = "Medical License number"
                                        }
                                    }
                                },
                                system = "http://hl7.org/fhir/sid/us-npi",
                                value = provider.LicenseNumber
                            }
                        },
                        code = new
                        {
                            coding = new object[]
                            {
                                new
                                {
                                    system = "http://nucc.org/provider-taxonomy",
                                    code = GetNUCCCodeForSpecialty(provider.Specialty),
                                    display = provider.Specialty
                                }
                            },
                            text = provider.Specialty
                        }
                    }
                }
            };
            
            var json = JsonSerializer.Serialize(fhirPractitioner, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            
            return Result<string>.Success(json);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to generate FHIR Practitioner: {ex.Message}");
        }
    }

    public Result<string> GenerateObservation(ObservationResult observation, GenerationOptions options)
    {
        try
        {
            var observationId = $"observation-{Guid.NewGuid():N}";
            var patientId = observation.Patient?.MedicalRecordNumber != null 
                ? $"patient-{observation.Patient.MedicalRecordNumber}"
                : $"patient-{Guid.NewGuid():N}";
            
            // Generate realistic observation based on type
            var observationType = GetRandomObservationType(options.Seed ?? Environment.TickCount);
            
            var fhirObservation = new
            {
                resourceType = "Observation",
                id = observationId,
                status = "final",
                category = new object[]
                {
                    new
                    {
                        coding = new object[]
                        {
                            new
                            {
                                system = "http://terminology.hl7.org/CodeSystem/observation-category",
                                code = observationType.Category,
                                display = observationType.CategoryDisplay
                            }
                        }
                    }
                },
                code = new
                {
                    coding = new object[]
                    {
                        new
                        {
                            system = "http://loinc.org",
                            code = observationType.LoincCode,
                            display = observationType.Display
                        }
                    }
                },
                subject = new
                {
                    reference = $"Patient/{patientId}"
                },
                effectiveDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                valueQuantity = new
                {
                    value = observationType.Value,
                    unit = observationType.Unit,
                    system = "http://unitsofmeasure.org",
                    code = observationType.UcumCode
                }
            };
            
            var json = JsonSerializer.Serialize(fhirObservation, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            
            return Result<string>.Success(json);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to generate FHIR Observation: {ex.Message}");
        }
    }

    public Result<string> GenerateBundle(IReadOnlyList<FHIRResource> resources, BundleType bundleType, GenerationOptions options)
    {
        try
        {
            var bundleId = $"bundle-{Guid.NewGuid():N}";
            
            var entries = resources.Select(resource => new
            {
                fullUrl = $"urn:uuid:{resource.Id}",
                resource = JsonSerializer.Deserialize<object>(resource.JsonContent)
            }).ToArray();
            
            var fhirBundle = new
            {
                resourceType = "Bundle",
                id = bundleId,
                type = bundleType.ToString().ToLowerInvariant(),
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                total = resources.Count,
                entry = entries
            };
            
            var json = JsonSerializer.Serialize(fhirBundle, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            
            return Result<string>.Success(json);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to generate FHIR Bundle: {ex.Message}");
        }
    }

    public Result<string> GenerateEncounter(Encounter encounter, GenerationOptions options)
    {
        // TODO: Implement Encounter resource generation
        return Result<string>.Failure("FHIR Encounter generation not yet implemented");
    }

    public Result<string> GenerateOrganization(string organizationName, GenerationOptions options)
    {
        // TODO: Implement Organization resource generation  
        return Result<string>.Failure("FHIR Organization generation not yet implemented");
    }

    public Result<string> GenerateLocation(string locationName, GenerationOptions options)
    {
        // TODO: Implement Location resource generation
        return Result<string>.Failure("FHIR Location generation not yet implemented");
    }

    public Result<string> GenerateMedication(Medication medication, GenerationOptions options)
    {
        // TODO: Implement Medication resource generation
        return Result<string>.Failure("FHIR Medication generation not yet implemented");
    }

    public Result<string> GenerateMedicationRequest(Prescription prescription, GenerationOptions options)
    {
        // TODO: Implement MedicationRequest resource generation
        return Result<string>.Failure("FHIR MedicationRequest generation not yet implemented");
    }

    /// <summary>
    /// Maps provider specialty to NUCC Provider Taxonomy codes for FHIR qualification.
    /// </summary>
    private static string GetNUCCCodeForSpecialty(string? specialty)
    {
        return specialty?.ToLowerInvariant() switch
        {
            "emergency medicine" or "emergency" => "207P00000X",
            "internal medicine" or "internist" => "207R00000X", 
            "family medicine" or "family practice" => "207Q00000X",
            "pediatrics" or "pediatrician" => "208000000X",
            "cardiology" or "cardiologist" => "207RC0000X",
            "orthopedic surgery" or "orthopedics" => "207X00000X",
            "radiology" or "radiologist" => "2085R0202X",
            "anesthesiology" or "anesthesiologist" => "207L00000X",
            "psychiatry" or "psychiatrist" => "2084P0800X",
            "general surgery" or "surgeon" => "208600000X",
            _ => "207Q00000X" // Default to Family Medicine
        };
    }

    /// <summary>
    /// Gets a random observation type with proper LOINC codes and categories.
    /// </summary>
    private static ObservationType GetRandomObservationType(int seed)
    {
        var random = new Random(seed);
        var observationTypes = new[]
        {
            new ObservationType("8480-6", "Systolic blood pressure", "vital-signs", "Vital Signs", 
                               random.Next(90, 160), "mmHg", "mm[Hg]"),
            new ObservationType("8867-4", "Heart rate", "vital-signs", "Vital Signs", 
                               random.Next(60, 120), "beats/minute", "/min"),
            new ObservationType("8310-5", "Body temperature", "vital-signs", "Vital Signs", 
                               Math.Round(random.NextDouble() * (99.5 - 96.5) + 96.5, 1), "degrees F", "[degF]"),
            new ObservationType("33747-0", "General appearance", "exam", "Physical Exam", 
                               1, "Normal", "1")
        };
        
        return observationTypes[random.Next(observationTypes.Length)];
    }

    private record ObservationType(string LoincCode, string Display, string Category, string CategoryDisplay, 
                                 double Value, string Unit, string UcumCode);
}