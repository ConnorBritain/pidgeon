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
/// Loads medication data from embedded resources in Pidgeon.Data assembly.
/// Free tier data source with 25 top prescribed medications.
/// </summary>
public class EmbeddedMedicationDataSource : IMedicationDataSource
{
    private readonly ILogger<EmbeddedMedicationDataSource> _logger;
    private readonly Random _random;
    private readonly Assembly _dataAssembly;

    // Cached data
    private IReadOnlyList<MedicationData>? _medications;

    public EmbeddedMedicationDataSource(ILogger<EmbeddedMedicationDataSource> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();

        // Force load Pidgeon.Data assembly
        _ = Pidgeon.Data.DataAssemblyMarker.AssemblyName;

        _dataAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Pidgeon.Data")
            ?? throw new InvalidOperationException("Pidgeon.Data assembly not found");
    }

    public async Task<IReadOnlyList<MedicationData>> GetMedicationsAsync()
    {
        if (_medications != null)
            return _medications;

        const string resourceName = "data.datasets.free.medications.json";
        _medications = await LoadResourceAsync(resourceName);

        _logger.LogDebug("Loaded {MedicationCount} medications", _medications.Count);

        return _medications;
    }

    public async Task<MedicationData> GetRandomMedicationAsync()
    {
        var medications = await GetMedicationsAsync();
        return medications[_random.Next(medications.Count)];
    }

    private async Task<IReadOnlyList<MedicationData>> LoadResourceAsync(string resourceName)
    {
        using var stream = _dataAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        var data = await JsonSerializer.DeserializeAsync<List<MedicationData>>(stream)
            ?? throw new InvalidOperationException($"Failed to deserialize resource: {resourceName}");

        return data;
    }
}
