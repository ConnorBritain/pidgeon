// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.DTOs.Data;

namespace Pidgeon.Core.Application.Interfaces.Data;

/// <summary>
/// Data source for diagnosis code information (ICD-10).
/// Provides standard-agnostic diagnosis data for use across HL7, FHIR, and other standards.
/// </summary>
public interface IDiagnosisDataSource
{
    /// <summary>
    /// Gets all available ICD-10 diagnosis codes.
    /// </summary>
    Task<IReadOnlyList<DiagnosisData>> GetDiagnosisCodesAsync();

    /// <summary>
    /// Gets a random diagnosis code.
    /// </summary>
    Task<DiagnosisData> GetRandomDiagnosisCodeAsync();

    /// <summary>
    /// Gets diagnosis codes by category (e.g., "Cardiovascular", "Diabetes").
    /// </summary>
    Task<IReadOnlyList<DiagnosisData>> GetDiagnosisCodesByCategoryAsync(string category);

    /// <summary>
    /// Gets a random diagnosis code from a specific category.
    /// </summary>
    Task<DiagnosisData?> GetRandomDiagnosisCodeByCategoryAsync(string category);
}
