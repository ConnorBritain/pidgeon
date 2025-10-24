// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.DTOs.Data;
using Pidgeon.Core.Application.Interfaces.Data;
using System.Reflection;
using System.Text.Json;

namespace Pidgeon.Core.Infrastructure.Data;

/// <summary>
/// Loads demographic data from embedded resources in Pidgeon.Data assembly.
/// Free tier data source with curated names and addresses.
/// </summary>
public class EmbeddedDemographicDataSource : IDemographicDataSource
{
    private readonly ILogger<EmbeddedDemographicDataSource> _logger;
    private readonly Random _random;
    private readonly Assembly _dataAssembly;

    // Cached data
    private DemographicData? _names;
    private AddressCollection? _addresses;

    public EmbeddedDemographicDataSource(ILogger<EmbeddedDemographicDataSource> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();

        // Force load Pidgeon.Data assembly
        _ = Pidgeon.Data.DataAssemblyMarker.AssemblyName;

        _dataAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Pidgeon.Data")
            ?? throw new InvalidOperationException("Pidgeon.Data assembly not found");
    }

    public async Task<DemographicData> GetNamesAsync()
    {
        if (_names != null)
            return _names;

        const string resourceName = "data.datasets.free.patient_names.json";
        _names = await LoadResourceAsync<DemographicData>(resourceName);

        _logger.LogDebug("Loaded {MaleCount} male names, {FemaleCount} female names, {LastCount} last names",
            _names.FirstNames.Male.Count, _names.FirstNames.Female.Count, _names.LastNames.Count);

        return _names;
    }

    public async Task<AddressCollection> GetAddressesAsync()
    {
        if (_addresses != null)
            return _addresses;

        const string resourceName = "data.datasets.free.addresses.json";
        _addresses = await LoadResourceAsync<AddressCollection>(resourceName);

        _logger.LogDebug("Loaded {AddressCount} addresses from {RegionCount} regions",
            _addresses.Addresses.Count, _addresses.Metadata?.Regions?.Count ?? 0);

        return _addresses;
    }

    public async Task<string> GetRandomMaleFirstNameAsync()
    {
        var names = await GetNamesAsync();
        return names.FirstNames.Male[_random.Next(names.FirstNames.Male.Count)];
    }

    public async Task<string> GetRandomFemaleFirstNameAsync()
    {
        var names = await GetNamesAsync();
        return names.FirstNames.Female[_random.Next(names.FirstNames.Female.Count)];
    }

    public async Task<string> GetRandomLastNameAsync()
    {
        var names = await GetNamesAsync();
        return names.LastNames[_random.Next(names.LastNames.Count)];
    }

    public async Task<AddressData> GetRandomAddressAsync()
    {
        var addresses = await GetAddressesAsync();
        return addresses.Addresses[_random.Next(addresses.Addresses.Count)];
    }

    public async Task<string> GetRandomFullNameAsync(string gender)
    {
        var names = await GetNamesAsync();
        var firstName = gender?.ToUpperInvariant() switch
        {
            "M" or "MALE" => names.FirstNames.Male[_random.Next(names.FirstNames.Male.Count)],
            "F" or "FEMALE" => names.FirstNames.Female[_random.Next(names.FirstNames.Female.Count)],
            _ => _random.Next(2) == 0
                ? names.FirstNames.Male[_random.Next(names.FirstNames.Male.Count)]
                : names.FirstNames.Female[_random.Next(names.FirstNames.Female.Count)]
        };

        var lastName = names.LastNames[_random.Next(names.LastNames.Count)];
        return $"{firstName} {lastName}";
    }

    private async Task<T> LoadResourceAsync<T>(string resourceName)
    {
        using var stream = _dataAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        var data = await JsonSerializer.DeserializeAsync<T>(stream)
            ?? throw new InvalidOperationException($"Failed to deserialize resource: {resourceName}");

        return data;
    }
}
