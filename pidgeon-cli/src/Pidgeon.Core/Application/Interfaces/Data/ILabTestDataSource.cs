// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.DTOs.Data;

namespace Pidgeon.Core.Application.Interfaces.Data;

/// <summary>
/// Data source for laboratory test information (LOINC codes).
/// Provides standard-agnostic lab test data for use across HL7, FHIR, and other standards.
/// </summary>
public interface ILabTestDataSource
{
    /// <summary>
    /// Gets all available LOINC lab test codes.
    /// </summary>
    Task<IReadOnlyList<LabTestData>> GetLabTestsAsync();

    /// <summary>
    /// Gets a random lab test.
    /// </summary>
    Task<LabTestData> GetRandomLabTestAsync();

    /// <summary>
    /// Gets lab tests by component name (e.g., "Hemoglobin", "Glucose").
    /// </summary>
    Task<IReadOnlyList<LabTestData>> GetLabTestsByComponentAsync(string component);

    /// <summary>
    /// Gets a random lab test matching a component name.
    /// </summary>
    Task<LabTestData?> GetRandomLabTestByComponentAsync(string component);
}
