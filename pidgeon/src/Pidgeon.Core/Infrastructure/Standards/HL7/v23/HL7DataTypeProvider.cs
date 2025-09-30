// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Common;
using System.Reflection;
using System.Text.Json;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23;

/// <summary>
/// Provides access to HL7 v2.3 data type definitions from embedded JSON resources.
/// Loads data type schemas that define primitive and composite type structures.
/// </summary>
public class HL7DataTypeProvider : IHL7DataTypeProvider
{
    private readonly ILogger<HL7DataTypeProvider> _logger;
    private readonly Assembly _assembly;
    private readonly Dictionary<string, DataType> _cache = new();

    public HL7DataTypeProvider(ILogger<HL7DataTypeProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // Load resources from Pidgeon.Data assembly
        _assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Pidgeon.Data")
            ?? throw new InvalidOperationException("Pidgeon.Data assembly not found");
    }

    /// <summary>
    /// Gets a data type definition by code (e.g., "ST", "HD", "CE").
    /// Loads from embedded JSON resources in Pidgeon.Data.
    /// </summary>
    public async Task<Result<DataType>> GetDataTypeAsync(string dataTypeCode)
    {
        try
        {
            // Normalize the data type code
            var normalizedCode = dataTypeCode.ToUpperInvariant();

            // Check cache first
            if (_cache.TryGetValue(normalizedCode, out var cachedDataType))
            {
                return Result<DataType>.Success(cachedDataType);
            }

            // Build resource name for embedded JSON file
            var resourceName = $"data.standards.hl7v23.data_types.{normalizedCode.ToLowerInvariant()}.json";

            _logger.LogDebug("Loading data type {DataTypeCode} from resource {ResourceName}",
                dataTypeCode, resourceName);

            // Load from embedded resource
            using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogDebug("Data type resource not found: {ResourceName}", resourceName);
                return Result<DataType>.Failure($"Data type {dataTypeCode} not found");
            }

            // Parse JSON to data type schema
            var json = await new StreamReader(stream).ReadToEndAsync();
            var jsonDoc = JsonDocument.Parse(json);
            var dataType = ParseDataTypeFromJson(jsonDoc.RootElement);

            // Cache for future requests
            _cache[normalizedCode] = dataType;

            _logger.LogDebug("Successfully loaded data type {DataTypeCode} ({Category}) with {ComponentCount} components",
                dataTypeCode, dataType.Category, dataType.Components.Count);

            return Result<DataType>.Success(dataType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data type {DataTypeCode}", dataTypeCode);
            return Result<DataType>.Failure($"Failed to load data type: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses data type JSON structure into strongly-typed schema object.
    /// </summary>
    private DataType ParseDataTypeFromJson(JsonElement root)
    {
        var code = root.GetProperty("code").GetString() ?? "";
        var name = root.GetProperty("name").GetString() ?? "";
        var description = root.TryGetProperty("description", out var descElement)
            ? descElement.GetString() ?? ""
            : "";
        var category = root.TryGetProperty("category", out var categoryElement)
            ? categoryElement.GetString() ?? ""
            : "";

        var components = new List<DataTypeComponent>();
        if (root.TryGetProperty("fields", out var fieldsArray))
        {
            foreach (var fieldElement in fieldsArray.EnumerateArray())
            {
                var component = ParseDataTypeComponentFromJson(fieldElement);
                components.Add(component);
            }
        }

        return new DataType(
            Code: code,
            Name: name,
            Description: description,
            Category: category,
            Components: components.AsReadOnly()
        );
    }

    /// <summary>
    /// Parses data type component JSON structure into strongly-typed component object.
    /// </summary>
    private DataTypeComponent ParseDataTypeComponentFromJson(JsonElement componentElement)
    {
        var position = componentElement.GetProperty("position").GetInt32();
        var name = componentElement.TryGetProperty("field_description", out var nameElement)
            ? nameElement.GetString() ?? ""
            : componentElement.TryGetProperty("field_name", out var altNameElement)
                ? altNameElement.GetString() ?? ""
                : "";
        var dataType = componentElement.GetProperty("data_type").GetString() ?? "";
        var optionality = componentElement.GetProperty("optionality").GetString() ?? "";

        // Parse table ID if present
        int? tableId = null;
        if (componentElement.TryGetProperty("table", out var tableElement))
        {
            var tableString = tableElement.GetString();
            if (!string.IsNullOrEmpty(tableString) && int.TryParse(tableString, out var tableValue))
            {
                tableId = tableValue;
            }
        }

        return new DataTypeComponent(
            Position: position,
            Name: name,
            DataType: dataType,
            Optionality: optionality,
            TableId: tableId
        );
    }
}

/// <summary>
/// Interface for accessing HL7 data type definitions.
/// </summary>
public interface IHL7DataTypeProvider
{
    /// <summary>
    /// Gets a data type definition by code.
    /// </summary>
    Task<Result<DataType>> GetDataTypeAsync(string dataTypeCode);
}

/// <summary>
/// Represents a complete HL7 data type definition.
/// </summary>
public record DataType(
    string Code,
    string Name,
    string Description,
    string Category,
    IReadOnlyList<DataTypeComponent> Components);

/// <summary>
/// Represents a component within a composite data type.
/// </summary>
public record DataTypeComponent(
    int Position,
    string Name,
    string DataType,
    string Optionality,
    int? TableId);