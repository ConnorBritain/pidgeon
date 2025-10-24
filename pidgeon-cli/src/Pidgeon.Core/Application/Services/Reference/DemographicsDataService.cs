// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Reference;
using Pidgeon.Core.Domain.Clinical.Entities;

namespace Pidgeon.Core.Application.Services.Reference;

/// <summary>
/// Service for accessing demographic datasets for realistic data generation.
/// Replaces hardcoded arrays with data-driven approach using JSON tables.
/// </summary>
public class DemographicsDataService : IDemographicsDataService
{
    private readonly ILogger<DemographicsDataService> _logger;
    private readonly string _dataBasePath;
    private readonly bool _useEmbeddedResources;
    private readonly Assembly? _dataAssembly;
    private readonly Dictionary<string, List<string>> _cachedTables = new();
    private readonly object _cacheLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public DemographicsDataService(ILogger<DemographicsDataService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Try embedded resources first, fallback to file system
        // Check all loaded assemblies for embedded resources since resources are in Pidgeon.Data assembly
        var resourcePrefix = "data.standards.hl7v23.tables";
        var dataAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => HasEmbeddedResources(a, resourcePrefix));


        if (dataAssembly != null)
        {
            _useEmbeddedResources = true;
            _dataAssembly = dataAssembly;
            _dataBasePath = string.Empty; // Not needed for embedded resources
            _logger.LogInformation("DemographicsDataService using embedded resources from {Assembly}", dataAssembly.GetName().Name);
        }
        else
        {
            _useEmbeddedResources = false;
            _dataAssembly = null;
            var currentDir = Directory.GetCurrentDirectory();
            _dataBasePath = Path.Combine(currentDir, "data", "standards", "hl7v23", "tables");
            _logger.LogInformation("DemographicsDataService using file system path: {DataPath}", _dataBasePath);
        }
    }

    public async Task<List<string>> GetFirstNamesAsync(string? gender = null)
    {
        var tableName = gender?.ToLowerInvariant() switch
        {
            "male" or "m" => "FirstNameMale",
            "female" or "f" => "FirstNameFemale",
            _ => "FirstName"
        };

        return await GetTableValuesAsync(tableName);
    }

    public async Task<List<string>> GetLastNamesAsync()
    {
        return await GetTableValuesAsync("LastName");
    }

    public async Task<List<string>> GetZipCodesAsync()
    {
        return await GetTableValuesAsync("ZipCode");
    }

    public async Task<List<string>> GetCitiesAsync()
    {
        return await GetTableValuesAsync("City");
    }

    public async Task<List<string>> GetStatesAsync()
    {
        return await GetTableValuesAsync("State");
    }

    public async Task<List<string>> GetStreetsAsync()
    {
        return await GetTableValuesAsync("Street");
    }

    public async Task<List<string>> GetPhoneNumbersAsync()
    {
        return await GetTableValuesAsync("PhoneNumber");
    }

    public async Task<List<string>> GetTableValuesAsync(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return new List<string>();

        // Check cache first
        lock (_cacheLock)
        {
            if (_cachedTables.TryGetValue(tableName, out var cachedValues))
                return cachedValues;
        }

        try
        {
            string json;
            if (_useEmbeddedResources && _dataAssembly != null)
            {
                var resourceName = $"data.standards.hl7v23.tables.{tableName}.json";
                json = await LoadEmbeddedResourceAsync(_dataAssembly, resourceName);
            }
            else
            {
                var tableFile = Path.Combine(_dataBasePath, $"{tableName}.json");
                if (!File.Exists(tableFile))
                {
                    _logger.LogWarning("Table file not found: {TableFile}", tableFile);
                    return new List<string>();
                }
                json = await File.ReadAllTextAsync(tableFile);
            }
            var tableData = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            var values = new List<string>();

            // Extract values from different possible structures
            if (tableData.TryGetProperty("values", out var valuesElement))
            {
                if (valuesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in valuesElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var value = item.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                                values.Add(value);
                        }
                        else if (item.ValueKind == JsonValueKind.Object)
                        {
                            // Try common property names for the actual value
                            var valueProps = new[] { "value", "name", "display", "code" };
                            foreach (var prop in valueProps)
                            {
                                if (item.TryGetProperty(prop, out var propElement) &&
                                    propElement.ValueKind == JsonValueKind.String)
                                {
                                    var value = propElement.GetString();
                                    if (!string.IsNullOrWhiteSpace(value))
                                    {
                                        values.Add(value);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // If no values found yet, try to extract from table entries
            if (values.Count == 0 && tableData.TryGetProperty("entries", out var entriesElement))
            {
                if (entriesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entry in entriesElement.EnumerateArray())
                    {
                        if (entry.TryGetProperty("value", out var valueElement) &&
                            valueElement.ValueKind == JsonValueKind.String)
                        {
                            var value = valueElement.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                                values.Add(value);
                        }
                    }
                }
            }

            // Cache the results
            lock (_cacheLock)
            {
                _cachedTables[tableName] = values;
            }

            _logger.LogDebug("Loaded {Count} values from table {TableName}", values.Count, tableName);
            return values;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading table values for {TableName}", tableName);
            return new List<string>();
        }
    }

    public async Task<string> GetRandomValueAsync(string tableName, Random random)
    {
        var values = await GetTableValuesAsync(tableName);
        return values.Count > 0 ? values[random.Next(values.Count)] : "UNKNOWN";
    }

    public async Task<(string firstName, string lastName, string gender)> GenerateRandomNameAsync(Random random)
    {
        // Randomly select gender first
        var genders = new[] { "Male", "Female" };
        var gender = genders[random.Next(genders.Length)];

        // Get names based on gender
        var firstNames = await GetFirstNamesAsync(gender);
        var lastNames = await GetLastNamesAsync();

        var firstName = firstNames.Count > 0 ? firstNames[random.Next(firstNames.Count)] : "Unknown";
        var lastName = lastNames.Count > 0 ? lastNames[random.Next(lastNames.Count)] : "Unknown";

        return (firstName, lastName, gender);
    }

    public async Task<Address> GenerateRandomAddressAsync(Random random)
    {
        var streets = await GetStreetsAsync();
        var cities = await GetCitiesAsync();
        var states = await GetStatesAsync();
        var zipCodes = await GetZipCodesAsync();

        var street = streets.Count > 0 ? streets[random.Next(streets.Count)] : "Main St";
        var houseNumber = random.Next(1, 9999);
        var city = cities.Count > 0 ? cities[random.Next(cities.Count)] : "Anytown";
        var state = states.Count > 0 ? states[random.Next(states.Count)] : "CA";
        var zipCode = zipCodes.Count > 0 ? zipCodes[random.Next(zipCodes.Count)] : "90210";

        return new Address
        {
            Street1 = $"{houseNumber} {street}",
            City = city,
            State = state,
            PostalCode = zipCode,
            Country = "USA"
        };
    }

    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _cachedTables.Clear();
        }

        _logger.LogInformation("Demographics data cache cleared");
    }

    /// <summary>
    /// Checks if embedded resources exist for the specified resource prefix.
    /// </summary>
    private static bool HasEmbeddedResources(Assembly assembly, string resourcePrefix)
    {
        var resourceNames = assembly.GetManifestResourceNames();
        return resourceNames.Any(name => name.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Loads content from embedded resource.
    /// </summary>
    private static async Task<string> LoadEmbeddedResourceAsync(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Embedded resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}