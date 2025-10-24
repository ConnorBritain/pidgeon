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
/// Loads diagnosis code data from embedded resources in Pidgeon.Data assembly.
/// Free tier data source with 99 common ICD-10 codes.
/// </summary>
public class EmbeddedDiagnosisDataSource : IDiagnosisDataSource
{
    private readonly ILogger<EmbeddedDiagnosisDataSource> _logger;
    private readonly Random _random;
    private readonly Assembly _dataAssembly;

    // Cached data
    private IReadOnlyList<DiagnosisData>? _diagnosisCodes;

    public EmbeddedDiagnosisDataSource(ILogger<EmbeddedDiagnosisDataSource> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();

        // Force load Pidgeon.Data assembly
        _ = Pidgeon.Data.DataAssemblyMarker.AssemblyName;

        _dataAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Pidgeon.Data")
            ?? throw new InvalidOperationException("Pidgeon.Data assembly not found");
    }

    public async Task<IReadOnlyList<DiagnosisData>> GetDiagnosisCodesAsync()
    {
        if (_diagnosisCodes != null)
            return _diagnosisCodes;

        const string resourceName = "data.datasets.free.icd10_common.json";
        _diagnosisCodes = await LoadResourceAsync(resourceName);

        _logger.LogDebug("Loaded {DiagnosisCodeCount} ICD-10 diagnosis codes", _diagnosisCodes.Count);

        return _diagnosisCodes;
    }

    public async Task<DiagnosisData> GetRandomDiagnosisCodeAsync()
    {
        var codes = await GetDiagnosisCodesAsync();
        return codes[_random.Next(codes.Count)];
    }

    public async Task<IReadOnlyList<DiagnosisData>> GetDiagnosisCodesByCategoryAsync(string category)
    {
        var codes = await GetDiagnosisCodesAsync();
        return codes
            .Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<DiagnosisData?> GetRandomDiagnosisCodeByCategoryAsync(string category)
    {
        var codes = await GetDiagnosisCodesByCategoryAsync(category);
        return codes.Count > 0 ? codes[_random.Next(codes.Count)] : null;
    }

    private async Task<IReadOnlyList<DiagnosisData>> LoadResourceAsync(string resourceName)
    {
        using var stream = _dataAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        var data = await JsonSerializer.DeserializeAsync<List<DiagnosisData>>(stream)
            ?? throw new InvalidOperationException($"Failed to deserialize resource: {resourceName}");

        return data;
    }
}
