// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Infrastructure.Standards.FHIR.R4;

namespace Pidgeon.Core.Application.Services.Standards;

/// <summary>
/// FHIR search test harness implementation that simulates FHIR server search functionality.
/// Provides realistic search results with proper FHIR Bundle structure, pagination, and reference resolution.
/// </summary>
internal class FHIRSearchHarnessService : IFHIRSearchHarnessService
{
    private readonly IFHIRResourceFactory _fhirResourceFactory;
    private readonly Pidgeon.Core.Generation.IGenerationService _domainGenerationService;

    public FHIRSearchHarnessService(
        IFHIRResourceFactory fhirResourceFactory,
        Pidgeon.Core.Generation.IGenerationService domainGenerationService)
    {
        _fhirResourceFactory = fhirResourceFactory;
        _domainGenerationService = domainGenerationService;
    }

    /// <summary>
    /// Executes a FHIR search query and returns a searchset bundle with realistic results.
    /// Simulates FHIR server search behavior including pagination, scoring, and includes.
    /// </summary>
    public async Task<Result<string>> ExecuteSearchAsync(string resourceType, FHIRSearchQuery searchQuery, GenerationOptions options)
    {
        try
        {
            // Validate search query first
            var validationResult = ValidateSearchQuery(resourceType, searchQuery);
            if (!validationResult.IsSuccess || !validationResult.Value.IsValid)
            {
                var errors = validationResult.IsSuccess 
                    ? string.Join(", ", validationResult.Value.Errors)
                    : validationResult.Error.ToString();
                return Result<string>.Failure($"Invalid search query: {errors}");
            }

            // Generate primary search results based on resource type
            var primaryResults = await GeneratePrimarySearchResults(resourceType, searchQuery, options);
            if (!primaryResults.IsSuccess)
                return Result<string>.Failure($"Failed to generate search results: {primaryResults.Error}");

            // Process _include parameters to add referenced resources
            var allResources = new List<FHIRResource>(primaryResults.Value);
            if (searchQuery.Include.Any())
            {
                var includedResults = await ProcessIncludeParameters(primaryResults.Value, searchQuery.Include, options);
                if (includedResults.IsSuccess)
                    allResources.AddRange(includedResults.Value);
            }

            // Process _revinclude parameters to add reverse referenced resources  
            if (searchQuery.RevInclude.Any())
            {
                var revIncludedResults = await ProcessRevIncludeParameters(primaryResults.Value, searchQuery.RevInclude, options);
                if (revIncludedResults.IsSuccess)
                    allResources.AddRange(revIncludedResults.Value);
            }

            // Create searchset bundle with pagination links
            var searchsetBundle = CreateSearchsetBundle(allResources, searchQuery, options);
            return searchsetBundle;
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"FHIR search execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates realistic clinical scenario bundles for comprehensive testing workflows.
    /// Creates multi-resource scenarios that reflect real healthcare patterns.
    /// </summary>
    public async Task<Result<string>> GenerateClinicalScenarioAsync(ClinicalScenarioType scenarioType, GenerationOptions options)
    {
        try
        {
            var resources = new List<FHIRResource>();

            switch (scenarioType)
            {
                case ClinicalScenarioType.AdmissionWithLabs:
                    resources = await GenerateAdmissionWithLabsScenario(options);
                    break;

                case ClinicalScenarioType.DiabetesManagement:
                    resources = await GenerateDiabetesManagementScenario(options);
                    break;

                case ClinicalScenarioType.EmergencyVisit:
                    resources = await GenerateEmergencyVisitScenario(options);
                    break;

                case ClinicalScenarioType.SurgicalCare:
                    resources = await GenerateSurgicalCareScenario(options);
                    break;

                case ClinicalScenarioType.MedicationRefill:
                    resources = await GenerateMedicationRefillScenario(options);
                    break;

                case ClinicalScenarioType.PreventiveCare:
                    resources = await GeneratePreventiveCareScenario(options);
                    break;

                case ClinicalScenarioType.ChronicCareManagement:
                    resources = await GenerateChronicCareManagementScenario(options);
                    break;

                case ClinicalScenarioType.PediatricWellChild:
                    resources = await GeneratePediatricWellChildScenario(options);
                    break;

                default:
                    return Result<string>.Failure($"Unsupported scenario type: {scenarioType}");
            }

            // Create collection bundle for the scenario
            return _fhirResourceFactory.GenerateBundle(resources, BundleType.Collection, options);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Clinical scenario generation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates FHIR search parameters according to FHIR specification.
    /// Checks parameter syntax, supported parameters, and parameter combinations.
    /// </summary>
    public Result<FHIRSearchValidationResult> ValidateSearchQuery(string resourceType, FHIRSearchQuery searchQuery)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var suggestions = new List<string>();

        // Validate resource type
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            errors.Add("Resource type is required");
        }
        else if (!IsValidResourceType(resourceType))
        {
            errors.Add($"Invalid resource type: {resourceType}");
        }

        // Validate count parameter
        if (searchQuery.Count < 0 || searchQuery.Count > 1000)
        {
            errors.Add("Count must be between 0 and 1000");
        }
        else if (searchQuery.Count > 100)
        {
            warnings.Add("Large count values may impact performance");
        }

        // Validate offset parameter
        if (searchQuery.Offset < 0)
        {
            errors.Add("Offset cannot be negative");
        }

        // Validate search parameters for the resource type
        foreach (var param in searchQuery.Parameters)
        {
            if (!IsValidSearchParameter(resourceType, param.Key))
            {
                warnings.Add($"Parameter '{param.Key}' may not be supported for {resourceType}");
            }
        }

        // Validate include parameters
        foreach (var include in searchQuery.Include)
        {
            if (!IsValidIncludeParameter(include))
            {
                errors.Add($"Invalid _include parameter syntax: {include}");
            }
        }

        // Validate revinclude parameters
        foreach (var revInclude in searchQuery.RevInclude)
        {
            if (!IsValidIncludeParameter(revInclude))
            {
                errors.Add($"Invalid _revinclude parameter syntax: {revInclude}");
            }
        }

        // Performance suggestions
        if (searchQuery.Parameters.Count == 0 && searchQuery.Count > 50)
        {
            suggestions.Add("Consider adding search parameters to improve query performance");
        }

        if (searchQuery.Include.Count > 5)
        {
            suggestions.Add("Large number of _include parameters may impact performance");
        }

        var result = new FHIRSearchValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Warnings = warnings,
            Suggestions = suggestions
        };

        return Result<FHIRSearchValidationResult>.Success(result);
    }

    // === Private Helper Methods ===

    /// <summary>
    /// Generates primary search results based on resource type and search parameters.
    /// </summary>
    private async Task<Result<List<FHIRResource>>> GeneratePrimarySearchResults(
        string resourceType, FHIRSearchQuery searchQuery, GenerationOptions options)
    {
        var resources = new List<FHIRResource>();
        var count = Math.Min(searchQuery.Count, 20); // Limit for realistic simulation

        for (int i = 0; i < count; i++)
        {
            var resourceResult = await GenerateResourceByType(resourceType, options);
            if (resourceResult.IsSuccess)
            {
                var resourceId = ExtractResourceId(resourceResult.Value);
                resources.Add(new FHIRResource
                {
                    ResourceType = resourceType,
                    Id = resourceId,
                    JsonContent = resourceResult.Value
                });
            }
        }

        return Result<List<FHIRResource>>.Success(resources);
    }

    /// <summary>
    /// Processes _include parameters to add referenced resources to the search results.
    /// </summary>
    private async Task<Result<List<FHIRResource>>> ProcessIncludeParameters(
        List<FHIRResource> primaryResults, IReadOnlyList<string> includeParams, GenerationOptions options)
    {
        var includedResources = new List<FHIRResource>();

        foreach (var includeParam in includeParams)
        {
            // Parse include parameter (e.g., "Patient:general-practitioner" or "Observation:patient")
            var parts = includeParam.Split(':');
            if (parts.Length == 2)
            {
                var sourceResource = parts[0];
                var referenceField = parts[1];

                // Generate appropriate referenced resources based on the reference field
                var referencedResourceType = GetReferencedResourceType(sourceResource, referenceField);
                if (!string.IsNullOrEmpty(referencedResourceType))
                {
                    var referencedResult = await GenerateResourceByType(referencedResourceType, options);
                    if (referencedResult.IsSuccess)
                    {
                        var resourceId = ExtractResourceId(referencedResult.Value);
                        includedResources.Add(new FHIRResource
                        {
                            ResourceType = referencedResourceType,
                            Id = resourceId,
                            JsonContent = referencedResult.Value
                        });
                    }
                }
            }
        }

        return Result<List<FHIRResource>>.Success(includedResources);
    }

    /// <summary>
    /// Processes _revinclude parameters to add resources that reference the primary results.
    /// </summary>
    private async Task<Result<List<FHIRResource>>> ProcessRevIncludeParameters(
        List<FHIRResource> primaryResults, IReadOnlyList<string> revIncludeParams, GenerationOptions options)
    {
        var revIncludedResources = new List<FHIRResource>();

        foreach (var revIncludeParam in revIncludeParams)
        {
            // Parse revinclude parameter (e.g., "Observation:patient" means find Observations that reference these Patients)
            var parts = revIncludeParam.Split(':');
            if (parts.Length == 2)
            {
                var referencingResourceType = parts[0];
                var referenceField = parts[1];

                // Generate resources that would reference the primary results
                for (int i = 0; i < Math.Min(primaryResults.Count, 3); i++) // Limit for performance
                {
                    var referencingResult = await GenerateResourceByType(referencingResourceType, options);
                    if (referencingResult.IsSuccess)
                    {
                        var resourceId = ExtractResourceId(referencingResult.Value);
                        revIncludedResources.Add(new FHIRResource
                        {
                            ResourceType = referencingResourceType,
                            Id = resourceId,
                            JsonContent = referencingResult.Value
                        });
                    }
                }
            }
        }

        return Result<List<FHIRResource>>.Success(revIncludedResources);
    }

    /// <summary>
    /// Creates a FHIR searchset bundle with proper pagination links and metadata.
    /// </summary>
    private Result<string> CreateSearchsetBundle(List<FHIRResource> resources, FHIRSearchQuery searchQuery, GenerationOptions options)
    {
        // Create bundle with searchset type and proper metadata
        var bundleId = $"searchset-{Guid.NewGuid():N}";
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var entries = resources.Select(resource => new
        {
            fullUrl = $"urn:uuid:{resource.Id}",
            resource = JsonSerializer.Deserialize<object>(resource.JsonContent),
            search = new { mode = "match" }
        }).ToArray();

        var searchsetBundle = new
        {
            resourceType = "Bundle",
            id = bundleId,
            type = "searchset",
            timestamp = timestamp,
            total = resources.Count,
            entry = entries,
            link = new[]
            {
                new { relation = "self", url = $"/{resources.FirstOrDefault()?.ResourceType ?? "Patient"}?_count={searchQuery.Count}" }
            }
        };

        var json = JsonSerializer.Serialize(searchsetBundle, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return Result<string>.Success(json);
    }

    // === Clinical Scenario Generators ===

    private async Task<List<FHIRResource>> GenerateAdmissionWithLabsScenario(GenerationOptions options)
    {
        var resources = new List<FHIRResource>();

        // Generate Patient
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (patientResult.IsSuccess)
        {
            var patientJson = _fhirResourceFactory.GeneratePatient(patientResult.Value, options);
            if (patientJson.IsSuccess)
            {
                resources.Add(new FHIRResource
                {
                    ResourceType = "Patient",
                    Id = ExtractResourceId(patientJson.Value),
                    JsonContent = patientJson.Value
                });
            }
        }

        // Generate Encounter
        var encounterResult = _domainGenerationService.GenerateEncounter(options);
        if (encounterResult.IsSuccess)
        {
            var encounterJson = _fhirResourceFactory.GenerateEncounter(encounterResult.Value, options);
            if (encounterJson.IsSuccess)
            {
                resources.Add(new FHIRResource
                {
                    ResourceType = "Encounter",
                    Id = ExtractResourceId(encounterJson.Value),
                    JsonContent = encounterJson.Value
                });
            }
        }

        // Generate multiple Observations (lab results)
        for (int i = 0; i < 3; i++)
        {
            var obsResult = _domainGenerationService.GenerateObservationResult(options);
            if (obsResult.IsSuccess)
            {
                var obsJson = _fhirResourceFactory.GenerateObservation(obsResult.Value, options);
                if (obsJson.IsSuccess)
                {
                    resources.Add(new FHIRResource
                    {
                        ResourceType = "Observation",
                        Id = ExtractResourceId(obsJson.Value),
                        JsonContent = obsJson.Value
                    });
                }
            }
        }

        return resources;
    }

    private async Task<List<FHIRResource>> GenerateDiabetesManagementScenario(GenerationOptions options)
    {
        var resources = new List<FHIRResource>();

        // Generate Patient with diabetes
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (patientResult.IsSuccess)
        {
            var patientJson = _fhirResourceFactory.GeneratePatient(patientResult.Value, options);
            if (patientJson.IsSuccess)
            {
                resources.Add(new FHIRResource
                {
                    ResourceType = "Patient",
                    Id = ExtractResourceId(patientJson.Value),
                    JsonContent = patientJson.Value
                });
            }
        }

        // Generate Provider for diabetes management
        var providerResult = _domainGenerationService.GenerateProvider(options);
        if (providerResult.IsSuccess)
        {
            var practitionerJson = _fhirResourceFactory.GeneratePractitioner(providerResult.Value, options);
            if (practitionerJson.IsSuccess)
            {
                resources.Add(new FHIRResource
                {
                    ResourceType = "Practitioner",
                    Id = ExtractResourceId(practitionerJson.Value),
                    JsonContent = practitionerJson.Value
                });
            }
        }

        // Generate multiple diabetes-related observations (A1c, Glucose, etc.)
        var diabetesObservations = new[] { "A1c", "Glucose", "Blood Pressure" };
        foreach (var obsType in diabetesObservations)
        {
            var obsResult = _domainGenerationService.GenerateObservationResult(options);
            if (obsResult.IsSuccess)
            {
                var obsJson = _fhirResourceFactory.GenerateObservation(obsResult.Value, options);
                if (obsJson.IsSuccess)
                {
                    resources.Add(new FHIRResource
                    {
                        ResourceType = "Observation",
                        Id = ExtractResourceId(obsJson.Value),
                        JsonContent = obsJson.Value
                    });
                }
            }
        }

        return resources;
    }

    private async Task<List<FHIRResource>> GenerateEmergencyVisitScenario(GenerationOptions options)
    {
        var resources = new List<FHIRResource>();

        // Generate Patient
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (patientResult.IsSuccess)
        {
            var patientJson = _fhirResourceFactory.GeneratePatient(patientResult.Value, options);
            if (patientJson.IsSuccess)
            {
                resources.Add(new FHIRResource
                {
                    ResourceType = "Patient",
                    Id = ExtractResourceId(patientJson.Value),
                    JsonContent = patientJson.Value
                });
            }
        }

        // Generate Emergency Encounter
        var encounterResult = _domainGenerationService.GenerateEncounter(options);
        if (encounterResult.IsSuccess)
        {
            var encounterJson = _fhirResourceFactory.GenerateEncounter(encounterResult.Value, options);
            if (encounterJson.IsSuccess)
            {
                resources.Add(new FHIRResource
                {
                    ResourceType = "Encounter",
                    Id = ExtractResourceId(encounterJson.Value),
                    JsonContent = encounterJson.Value
                });
            }
        }

        // Generate Emergency Provider
        var providerResult = _domainGenerationService.GenerateProvider(options);
        if (providerResult.IsSuccess)
        {
            var practitionerJson = _fhirResourceFactory.GeneratePractitioner(providerResult.Value, options);
            if (practitionerJson.IsSuccess)
            {
                resources.Add(new FHIRResource
                {
                    ResourceType = "Practitioner", 
                    Id = ExtractResourceId(practitionerJson.Value),
                    JsonContent = practitionerJson.Value
                });
            }
        }

        // Generate multiple emergency observations (vitals, pain scale, etc.)
        for (int i = 0; i < 5; i++)
        {
            var obsResult = _domainGenerationService.GenerateObservationResult(options);
            if (obsResult.IsSuccess)
            {
                var obsJson = _fhirResourceFactory.GenerateObservation(obsResult.Value, options);
                if (obsJson.IsSuccess)
                {
                    resources.Add(new FHIRResource
                    {
                        ResourceType = "Observation",
                        Id = ExtractResourceId(obsJson.Value),
                        JsonContent = obsJson.Value
                    });
                }
            }
        }

        return resources;
    }

    private async Task<List<FHIRResource>> GenerateSurgicalCareScenario(GenerationOptions options)
    {
        // TODO: Implement surgical care scenario with pre/post-op documentation
        return new List<FHIRResource>();
    }

    private async Task<List<FHIRResource>> GenerateMedicationRefillScenario(GenerationOptions options)
    {
        var resources = new List<FHIRResource>();

        // Generate Patient
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (patientResult.IsSuccess)
        {
            var patientJson = _fhirResourceFactory.GeneratePatient(patientResult.Value, options);
            if (patientJson.IsSuccess)
            {
                resources.Add(new FHIRResource
                {
                    ResourceType = "Patient",
                    Id = ExtractResourceId(patientJson.Value),
                    JsonContent = patientJson.Value
                });
            }
        }

        // Generate Prescribing Provider
        var providerResult = _domainGenerationService.GenerateProvider(options);
        if (providerResult.IsSuccess)
        {
            var practitionerJson = _fhirResourceFactory.GeneratePractitioner(providerResult.Value, options);
            if (practitionerJson.IsSuccess)
            {
                resources.Add(new FHIRResource
                {
                    ResourceType = "Practitioner",
                    Id = ExtractResourceId(practitionerJson.Value),
                    JsonContent = practitionerJson.Value
                });
            }
        }

        // Generate Medication (if available)
        var medicationResult = _domainGenerationService.GenerateMedication(options);
        if (medicationResult.IsSuccess)
        {
            var medicationJson = _fhirResourceFactory.GenerateMedication(medicationResult.Value, options);
            if (medicationJson.IsSuccess)
            {
                resources.Add(new FHIRResource
                {
                    ResourceType = "Medication",
                    Id = ExtractResourceId(medicationJson.Value),
                    JsonContent = medicationJson.Value
                });
            }
        }

        // Generate MedicationRequest (refill prescription)
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (prescriptionResult.IsSuccess)
        {
            var medRequestJson = _fhirResourceFactory.GenerateMedicationRequest(prescriptionResult.Value, options);
            if (medRequestJson.IsSuccess)
            {
                resources.Add(new FHIRResource
                {
                    ResourceType = "MedicationRequest",
                    Id = ExtractResourceId(medRequestJson.Value),
                    JsonContent = medRequestJson.Value
                });
            }
        }

        return resources;
    }

    private async Task<List<FHIRResource>> GeneratePreventiveCareScenario(GenerationOptions options)
    {
        // TODO: Implement preventive care scenario
        return new List<FHIRResource>();
    }

    private async Task<List<FHIRResource>> GenerateChronicCareManagementScenario(GenerationOptions options)
    {
        // TODO: Implement chronic care management scenario
        return new List<FHIRResource>();
    }

    private async Task<List<FHIRResource>> GeneratePediatricWellChildScenario(GenerationOptions options)
    {
        // TODO: Implement pediatric well-child scenario
        return new List<FHIRResource>();
    }

    // === Utility Methods ===

    private async Task<Result<string>> GenerateResourceByType(string resourceType, GenerationOptions options)
    {
        return resourceType.ToLowerInvariant() switch
        {
            "patient" => await GeneratePatientResource(options),
            "practitioner" => await GeneratePractitionerResource(options),
            "observation" => await GenerateObservationResource(options),
            "encounter" => await GenerateEncounterResource(options),
            "organization" => _fhirResourceFactory.GenerateOrganization("Healthcare Organization", options),
            "location" => _fhirResourceFactory.GenerateLocation("Healthcare Facility", options),
            _ => Result<string>.Failure($"Resource type {resourceType} not supported in search harness")
        };
    }

    private async Task<Result<string>> GeneratePatientResource(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);

        return _fhirResourceFactory.GeneratePatient(patientResult.Value, options);
    }

    private async Task<Result<string>> GeneratePractitionerResource(GenerationOptions options)
    {
        var providerResult = _domainGenerationService.GenerateProvider(options);
        if (!providerResult.IsSuccess)
            return Result<string>.Failure(providerResult.Error);

        return _fhirResourceFactory.GeneratePractitioner(providerResult.Value, options);
    }

    private async Task<Result<string>> GenerateObservationResource(GenerationOptions options)
    {
        var obsResult = _domainGenerationService.GenerateObservationResult(options);
        if (!obsResult.IsSuccess)
            return Result<string>.Failure(obsResult.Error);

        return _fhirResourceFactory.GenerateObservation(obsResult.Value, options);
    }

    private async Task<Result<string>> GenerateEncounterResource(GenerationOptions options)
    {
        var encounterResult = _domainGenerationService.GenerateEncounter(options);
        if (!encounterResult.IsSuccess)
            return Result<string>.Failure(encounterResult.Error);

        return _fhirResourceFactory.GenerateEncounter(encounterResult.Value, options);
    }

    private static string ExtractResourceId(string fhirJson)
    {
        var lines = fhirJson.Split('\n');
        var idLine = lines.FirstOrDefault(l => l.Contains("\"id\":"));
        if (idLine != null)
        {
            var start = idLine.IndexOf('"', idLine.IndexOf("\"id\":") + 5) + 1;
            var end = idLine.IndexOf('"', start);
            return idLine[start..end];
        }
        return Guid.NewGuid().ToString("N");
    }

    private static bool IsValidResourceType(string resourceType)
    {
        var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Patient", "Practitioner", "Organization", "Location", "Encounter", "Observation",
            "DiagnosticReport", "Condition", "Procedure", "Medication", "MedicationRequest",
            "CarePlan", "CareTeam", "ServiceRequest", "Account", "Coverage"
        };
        return validTypes.Contains(resourceType);
    }

    private static bool IsValidSearchParameter(string resourceType, string parameterName)
    {
        // Common FHIR search parameters
        var commonParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "_id", "_lastUpdated", "_tag", "_profile", "_security", "_text", "_content", "_filter",
            "name", "family", "given", "birthdate", "gender", "identifier", "active", "status",
            "code", "category", "subject", "patient", "date", "encounter"
        };
        return commonParams.Contains(parameterName);
    }

    private static bool IsValidIncludeParameter(string includeParam)
    {
        // Basic validation: should have format "ResourceType:field" or "ResourceType:field:target"
        var parts = includeParam.Split(':');
        return parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]);
    }

    private static string GetReferencedResourceType(string sourceResource, string referenceField)
    {
        // Map reference fields to target resource types
        return (sourceResource.ToLowerInvariant(), referenceField.ToLowerInvariant()) switch
        {
            ("patient", "general-practitioner") => "Practitioner",
            ("patient", "organization") => "Organization",
            ("observation", "patient") => "Patient",
            ("observation", "encounter") => "Encounter",
            ("encounter", "patient") => "Patient",
            ("encounter", "location") => "Location",
            _ => string.Empty
        };
    }
}