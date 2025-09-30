// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Resolves field values using demographic tables (FirstName.json, LastName.json, City.json, etc.).
/// Provides realistic demographic data for patient-related fields.
/// Priority: 80 (medium-high - demographic data preferred over random)
/// </summary>
public class DemographicFieldResolver : IFieldValueResolver
{
    private readonly ILogger<DemographicFieldResolver> _logger;
    private readonly Assembly _assembly;
    private readonly Random _random;
    private readonly Dictionary<string, List<string>> _demographicTableCache = new();

    public int Priority => 80;

    public DemographicFieldResolver(ILogger<DemographicFieldResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();

        // Load resources from Pidgeon.Data assembly
        _assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Pidgeon.Data")
            ?? throw new InvalidOperationException("Pidgeon.Data assembly not found");
    }

    public async Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        var fieldName = context.Field.Name?.ToLowerInvariant() ?? "";
        var fieldDescription = context.Field.Description?.ToLowerInvariant() ?? "";

        // Map field semantics to demographic table names
        var demographicTable = GetDemographicTableForField(fieldName, fieldDescription);
        if (demographicTable == null)
            return null; // Not a demographic field

        // Load value from demographic table
        var value = await GenerateValueFromDemographicTableAsync(demographicTable);
        return value;
    }

    /// <summary>
    /// Determine which demographic table to use for a field based on its semantics.
    /// Returns null if field is not demographic-related.
    /// </summary>
    private string? GetDemographicTableForField(string fieldName, string fieldDescription)
    {
        // Patient name fields
        if (fieldName.Contains("family name") || fieldName.Contains("last name") ||
            fieldDescription.Contains("family name") || fieldDescription.Contains("last name"))
            return "LastName";

        if (fieldName.Contains("given name") || fieldName.Contains("first name") ||
            fieldDescription.Contains("given name") || fieldDescription.Contains("first name"))
            return "FirstName";

        if (fieldName.Contains("patient name") || fieldName.Contains("person name"))
        {
            // For full name fields, we'll use FirstName (could be enhanced to combine first+last)
            return "FirstName";
        }

        // Address fields
        if (fieldName.Contains("city") || fieldDescription.Contains("city"))
            return "City";

        if (fieldName.Contains("country") || fieldDescription.Contains("country"))
            return "Country";

        // No demographic mapping for this field
        return null;
    }

    /// <summary>
    /// Load random value from demographic table (FirstName.json, LastName.json, etc.).
    /// Uses caching for performance.
    /// </summary>
    private async Task<string?> GenerateValueFromDemographicTableAsync(string tableName)
    {
        try
        {
            // Check cache first
            if (_demographicTableCache.TryGetValue(tableName, out var cachedValues))
            {
                return cachedValues.Count > 0 ? cachedValues[_random.Next(cachedValues.Count)] : null;
            }

            // Build resource name for demographic table JSON file
            var resourceName = $"data.standards.hl7v23.tables.{tableName}.json";

            _logger.LogDebug("Loading demographic table {TableName} from resource {ResourceName}",
                tableName, resourceName);

            // Load from embedded resource
            using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogDebug("Demographic table resource not found: {ResourceName}", resourceName);
                _demographicTableCache[tableName] = new List<string>(); // Cache empty list to avoid repeated lookups
                return null;
            }

            // Parse JSON to extract values
            var json = await new StreamReader(stream).ReadToEndAsync();
            var jsonDoc = JsonDocument.Parse(json);

            var values = new List<string>();
            if (jsonDoc.RootElement.TryGetProperty("values", out var valuesArray))
            {
                foreach (var valueElement in valuesArray.EnumerateArray())
                {
                    if (valueElement.TryGetProperty("value", out var valueProperty))
                    {
                        var value = valueProperty.GetString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            values.Add(value);
                        }
                    }
                }
            }

            // Cache for future requests
            _demographicTableCache[tableName] = values;

            _logger.LogDebug("Successfully loaded demographic table {TableName} with {ValueCount} values",
                tableName, values.Count);

            return values.Count > 0 ? values[_random.Next(values.Count)] : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading demographic table {TableName}", tableName);
            return null;
        }
    }
}