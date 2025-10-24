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
/// Loads vaccine data from embedded resources in Pidgeon.Data assembly.
/// Free tier data source with 98 active CVX vaccine codes.
/// </summary>
public class EmbeddedVaccineDataSource : IVaccineDataSource
{
    private readonly ILogger<EmbeddedVaccineDataSource> _logger;
    private readonly Random _random;
    private readonly Assembly _dataAssembly;

    // Cached data
    private IReadOnlyList<VaccineData>? _vaccines;

    public EmbeddedVaccineDataSource(ILogger<EmbeddedVaccineDataSource> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();

        // Force load Pidgeon.Data assembly
        _ = Pidgeon.Data.DataAssemblyMarker.AssemblyName;

        _dataAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Pidgeon.Data")
            ?? throw new InvalidOperationException("Pidgeon.Data assembly not found");
    }

    public async Task<IReadOnlyList<VaccineData>> GetVaccinesAsync()
    {
        if (_vaccines != null)
            return _vaccines;

        const string resourceName = "data.datasets.free.vaccines_cvx.json";
        _vaccines = await LoadResourceAsync(resourceName);

        _logger.LogDebug("Loaded {VaccineCount} CVX vaccine codes", _vaccines.Count);

        return _vaccines;
    }

    public async Task<VaccineData> GetRandomVaccineAsync()
    {
        var vaccines = await GetVaccinesAsync();
        return vaccines[_random.Next(vaccines.Count)];
    }

    public async Task<IReadOnlyList<VaccineData>> GetVaccinesByDescriptionAsync(string keyword)
    {
        var vaccines = await GetVaccinesAsync();
        return vaccines
            .Where(v => v.ShortDescription.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                       v.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<VaccineData?> GetRandomVaccineByDescriptionAsync(string keyword)
    {
        var vaccines = await GetVaccinesByDescriptionAsync(keyword);
        return vaccines.Count > 0 ? vaccines[_random.Next(vaccines.Count)] : null;
    }

    private async Task<IReadOnlyList<VaccineData>> LoadResourceAsync(string resourceName)
    {
        using var stream = _dataAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        var data = await JsonSerializer.DeserializeAsync<List<VaccineData>>(stream)
            ?? throw new InvalidOperationException($"Failed to deserialize resource: {resourceName}");

        return data;
    }
}
