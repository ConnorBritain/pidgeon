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
/// Loads lab test data from embedded resources in Pidgeon.Data assembly.
/// Free tier data source with 49 common LOINC codes.
/// </summary>
public class EmbeddedLabTestDataSource : ILabTestDataSource
{
    private readonly ILogger<EmbeddedLabTestDataSource> _logger;
    private readonly Random _random;
    private readonly Assembly _dataAssembly;

    // Cached data
    private IReadOnlyList<LabTestData>? _labTests;

    public EmbeddedLabTestDataSource(ILogger<EmbeddedLabTestDataSource> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();

        // Force load Pidgeon.Data assembly
        _ = Pidgeon.Data.DataAssemblyMarker.AssemblyName;

        _dataAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Pidgeon.Data")
            ?? throw new InvalidOperationException("Pidgeon.Data assembly not found");
    }

    public async Task<IReadOnlyList<LabTestData>> GetLabTestsAsync()
    {
        if (_labTests != null)
            return _labTests;

        const string resourceName = "data.datasets.free.loinc_common.json";
        _labTests = await LoadResourceAsync(resourceName);

        _logger.LogDebug("Loaded {LabTestCount} LOINC lab tests", _labTests.Count);

        return _labTests;
    }

    public async Task<LabTestData> GetRandomLabTestAsync()
    {
        var labTests = await GetLabTestsAsync();
        return labTests[_random.Next(labTests.Count)];
    }

    public async Task<IReadOnlyList<LabTestData>> GetLabTestsByComponentAsync(string component)
    {
        var labTests = await GetLabTestsAsync();
        return labTests
            .Where(l => l.Component.Contains(component, StringComparison.OrdinalIgnoreCase) ||
                       l.CommonName.Contains(component, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<LabTestData?> GetRandomLabTestByComponentAsync(string component)
    {
        var labTests = await GetLabTestsByComponentAsync(component);
        return labTests.Count > 0 ? labTests[_random.Next(labTests.Count)] : null;
    }

    private async Task<IReadOnlyList<LabTestData>> LoadResourceAsync(string resourceName)
    {
        using var stream = _dataAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        var data = await JsonSerializer.DeserializeAsync<List<LabTestData>>(stream)
            ?? throw new InvalidOperationException($"Failed to deserialize resource: {resourceName}");

        return data;
    }
}
