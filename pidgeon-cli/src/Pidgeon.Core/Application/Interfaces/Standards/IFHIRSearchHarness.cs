// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Application.Interfaces.Standards;

/// <summary>
/// Interface for FHIR search test harness that simulates FHIR server search functionality.
/// Provides realistic search results with proper pagination, includes, and reference resolution.
/// </summary>
public interface IFHIRSearchHarnessService
{
    /// <summary>
    /// Executes a FHIR search query and returns a search result bundle.
    /// Simulates FHIR server behavior including pagination, _include parameters, and search scoring.
    /// </summary>
    /// <param name="resourceType">The FHIR resource type to search (e.g., "Patient", "Observation")</param>
    /// <param name="searchQuery">The FHIR search query with parameters</param>
    /// <param name="options">Generation options for realistic data creation</param>
    /// <returns>FHIR Bundle with type="searchset" containing search results</returns>
    Task<Result<string>> ExecuteSearchAsync(string resourceType, FHIRSearchQuery searchQuery, GenerationOptions options);

    /// <summary>
    /// Generates a realistic clinical scenario bundle with multiple related resources.
    /// Creates complex workflows like "Diabetes patient with recent lab results and medication orders".
    /// </summary>
    /// <param name="scenarioType">Type of clinical scenario to generate</param>
    /// <param name="options">Generation options for realistic data creation</param>
    /// <returns>FHIR Bundle with type="collection" containing scenario resources</returns>
    Task<Result<string>> GenerateClinicalScenarioAsync(ClinicalScenarioType scenarioType, GenerationOptions options);

    /// <summary>
    /// Validates FHIR search parameters for correctness and completeness.
    /// Checks parameter names, values, and combinations according to FHIR specification.
    /// </summary>
    /// <param name="resourceType">The resource type being searched</param>
    /// <param name="searchQuery">The search query to validate</param>
    /// <returns>Validation result with details about parameter compliance</returns>
    Result<FHIRSearchValidationResult> ValidateSearchQuery(string resourceType, FHIRSearchQuery searchQuery);
}

/// <summary>
/// Represents a FHIR search query with parameters, includes, and pagination options.
/// </summary>
public record FHIRSearchQuery
{
    /// <summary>
    /// Search parameters (e.g., "name=Smith", "birthdate=gt1990-01-01").
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Include parameters for forward references (e.g., "_include=Patient:general-practitioner").
    /// </summary>
    public IReadOnlyList<string> Include { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Reverse include parameters for reverse references (e.g., "_revinclude=Observation:patient").
    /// </summary>
    public IReadOnlyList<string> RevInclude { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Maximum number of results to return (default: 20).
    /// </summary>
    public int Count { get; init; } = 20;

    /// <summary>
    /// Offset for pagination (skip first N results).
    /// </summary>
    public int Offset { get; init; } = 0;

    /// <summary>
    /// Sort parameters (e.g., "_sort=name", "_sort=-date").
    /// </summary>
    public IReadOnlyList<string> Sort { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Search result format (json, xml).
    /// </summary>
    public string Format { get; init; } = "json";
}

/// <summary>
/// Clinical scenario types for comprehensive testing workflows.
/// </summary>
public enum ClinicalScenarioType
{
    /// <summary>
    /// Patient admission with lab orders and results.
    /// </summary>
    AdmissionWithLabs,

    /// <summary>
    /// Diabetes patient with medication management and monitoring.
    /// </summary>
    DiabetesManagement,

    /// <summary>
    /// Emergency department visit with multiple observations and procedures.
    /// </summary>
    EmergencyVisit,

    /// <summary>
    /// Surgical procedure with pre/post-op care and documentation.
    /// </summary>
    SurgicalCare,

    /// <summary>
    /// Outpatient medication refill and monitoring.
    /// </summary>
    MedicationRefill,

    /// <summary>
    /// Preventive care visit with immunizations and screenings.
    /// </summary>
    PreventiveCare,

    /// <summary>
    /// Chronic disease management with care team coordination.
    /// </summary>
    ChronicCareManagement,

    /// <summary>
    /// Pediatric well-child visit with growth and development tracking.
    /// </summary>
    PediatricWellChild
}

/// <summary>
/// Result of FHIR search query validation.
/// </summary>
public record FHIRSearchValidationResult
{
    /// <summary>
    /// Whether the search query is valid according to FHIR specification.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation error messages, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Validation warning messages for potentially problematic parameters.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Suggested improvements for search performance or accuracy.
    /// </summary>
    public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();
}