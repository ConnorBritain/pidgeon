// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.DTOs.Data;

namespace Pidgeon.Core.Application.Interfaces.Data;

/// <summary>
/// Data source for patient demographic information (names and addresses).
/// Provides standard-agnostic demographic data for use across HL7, FHIR, and NCPDP.
/// </summary>
public interface IDemographicDataSource
{
    /// <summary>
    /// Gets patient name data (first names male/female, last names).
    /// </summary>
    Task<DemographicData> GetNamesAsync();

    /// <summary>
    /// Gets address data (street, city, state, ZIP).
    /// </summary>
    Task<AddressCollection> GetAddressesAsync();

    /// <summary>
    /// Gets a random male first name.
    /// </summary>
    Task<string> GetRandomMaleFirstNameAsync();

    /// <summary>
    /// Gets a random female first name.
    /// </summary>
    Task<string> GetRandomFemaleFirstNameAsync();

    /// <summary>
    /// Gets a random last name.
    /// </summary>
    Task<string> GetRandomLastNameAsync();

    /// <summary>
    /// Gets a random address.
    /// </summary>
    Task<AddressData> GetRandomAddressAsync();

    /// <summary>
    /// Gets a random full name (first + last).
    /// </summary>
    /// <param name="gender">Gender for first name selection ("M" or "F")</param>
    Task<string> GetRandomFullNameAsync(string gender);
}
