// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Segmint.Core.Domain;
using Segmint.Core.Generation.Types;

namespace Segmint.Core.Generation;

/// <summary>
/// Core service interface for generating synthetic healthcare data.
/// Supports both algorithmic generation (free tier) and AI enhancement (subscription tiers).
/// </summary>
public interface IGenerationService
{
    /// <summary>
    /// Generates a synthetic patient with realistic healthcare demographics.
    /// </summary>
    /// <param name="options">Generation configuration including patient type, AI settings, and seeds</param>
    /// <returns>A result containing the generated patient or error information</returns>
    Result<Patient> GeneratePatient(GenerationOptions options);

    /// <summary>
    /// Generates a synthetic medication with appropriate dosing and clinical context.
    /// </summary>
    /// <param name="options">Generation configuration including clinical context and constraints</param>
    /// <returns>A result containing the generated medication or error information</returns>
    Result<Medication> GenerateMedication(GenerationOptions options);

    /// <summary>
    /// Generates a synthetic prescription with patient, medication, and provider relationships.
    /// </summary>
    /// <param name="options">Generation configuration including workflow context</param>
    /// <returns>A result containing the generated prescription or error information</returns>
    Result<Prescription> GeneratePrescription(GenerationOptions options);

    /// <summary>
    /// Generates a synthetic healthcare encounter with clinical context.
    /// </summary>
    /// <param name="options">Generation configuration including encounter type and facility</param>
    /// <returns>A result containing the generated encounter or error information</returns>
    Result<Encounter> GenerateEncounter(GenerationOptions options);

    /// <summary>
    /// Gets metadata about the generation service capabilities and current tier.
    /// </summary>
    /// <returns>Service metadata including available features and limitations</returns>
    GenerationServiceInfo GetServiceInfo();
}