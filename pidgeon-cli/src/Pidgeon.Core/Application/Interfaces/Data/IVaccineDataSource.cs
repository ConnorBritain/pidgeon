// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.DTOs.Data;

namespace Pidgeon.Core.Application.Interfaces.Data;

/// <summary>
/// Data source for vaccine information (CVX codes).
/// Provides standard-agnostic vaccine data for use across HL7, FHIR, and other standards.
/// </summary>
public interface IVaccineDataSource
{
    /// <summary>
    /// Gets all available CVX vaccine codes.
    /// </summary>
    Task<IReadOnlyList<VaccineData>> GetVaccinesAsync();

    /// <summary>
    /// Gets a random vaccine.
    /// </summary>
    Task<VaccineData> GetRandomVaccineAsync();

    /// <summary>
    /// Gets vaccines by short description keyword (e.g., "COVID", "Influenza").
    /// </summary>
    Task<IReadOnlyList<VaccineData>> GetVaccinesByDescriptionAsync(string keyword);

    /// <summary>
    /// Gets a random vaccine matching a description keyword.
    /// </summary>
    Task<VaccineData?> GetRandomVaccineByDescriptionAsync(string keyword);
}
