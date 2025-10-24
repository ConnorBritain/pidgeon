// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Infrastructure.Standards.FHIR.R4;

/// <summary>
/// Factory interface for generating FHIR R4 standards-compliant resources.
/// Follows HL7 FHIR R4 specification for resource structure and content requirements.
/// </summary>
public interface IFHIRResourceFactory
{
    /// <summary>
    /// Generates a FHIR R4 compliant Patient resource with proper JSON structure.
    /// Required elements: resourceType, id, identifier, name, gender, birthDate
    /// </summary>
    Result<string> GeneratePatient(Patient patient, GenerationOptions options);

    /// <summary>
    /// Generates a FHIR R4 compliant Practitioner resource with proper JSON structure.
    /// Required elements: resourceType, id, active, name, qualification
    /// </summary>
    Result<string> GeneratePractitioner(Provider provider, GenerationOptions options);

    /// <summary>
    /// Generates a FHIR R4 compliant Observation resource with proper JSON structure.
    /// Required elements: resourceType, id, status, category, code, subject
    /// </summary>
    Result<string> GenerateObservation(ObservationResult observation, GenerationOptions options);

    /// <summary>
    /// Generates a FHIR R4 compliant Bundle resource containing multiple resources.
    /// Required elements: resourceType, id, type, entry array with resources
    /// </summary>
    Result<string> GenerateBundle(IReadOnlyList<FHIRResource> resources, BundleType bundleType, GenerationOptions options);

    /// <summary>
    /// Generates a FHIR R4 compliant Encounter resource with proper JSON structure.
    /// Required elements: resourceType, id, status, class, subject
    /// </summary>
    Result<string> GenerateEncounter(Encounter encounter, GenerationOptions options);

    /// <summary>
    /// Generates a FHIR R4 compliant Organization resource with proper JSON structure.
    /// Required elements: resourceType, id, active, name, type
    /// </summary>
    Result<string> GenerateOrganization(string organizationName, GenerationOptions options);

    /// <summary>
    /// Generates a FHIR R4 compliant Location resource with proper JSON structure.
    /// Required elements: resourceType, id, status, name, mode
    /// </summary>
    Result<string> GenerateLocation(string locationName, GenerationOptions options);

    /// <summary>
    /// Generates a FHIR R4 compliant Medication resource with proper JSON structure.
    /// Required elements: resourceType, id, code, form
    /// </summary>
    Result<string> GenerateMedication(Medication medication, GenerationOptions options);

    /// <summary>
    /// Generates a FHIR R4 compliant MedicationRequest resource with proper JSON structure.
    /// Required elements: resourceType, id, status, intent, medicationReference, subject
    /// </summary>
    Result<string> GenerateMedicationRequest(Prescription prescription, GenerationOptions options);
}

/// <summary>
/// Represents a FHIR resource for Bundle composition.
/// </summary>
public record FHIRResource
{
    public required string ResourceType { get; init; }
    public required string Id { get; init; }
    public required string JsonContent { get; init; }
}

/// <summary>
/// FHIR Bundle type enumeration.
/// </summary>
public enum BundleType
{
    /// <summary>
    /// Collection of resources for a specific purpose.
    /// </summary>
    Collection,
    
    /// <summary>
    /// Search results bundle.
    /// </summary>
    Searchset,
    
    /// <summary>
    /// Transaction bundle for atomic operations.
    /// </summary>
    Transaction,
    
    /// <summary>
    /// Message bundle for communication.
    /// </summary>
    Message,
    
    /// <summary>
    /// Document bundle representing a clinical document.
    /// </summary>
    Document
}