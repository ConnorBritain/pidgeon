// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Common;
using System.Reflection;
using System.Text.Json;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23;

/// <summary>
/// Provides access to HL7 v2.3 segment definitions from embedded JSON resources.
/// Loads segment schemas that define field structures and constraints.
/// </summary>
public class HL7SegmentProvider : IHL7SegmentProvider
{
    private readonly ILogger<HL7SegmentProvider> _logger;
    private readonly Assembly _assembly;
    private readonly Dictionary<string, SegmentSchema> _cache = new();

    public HL7SegmentProvider(ILogger<HL7SegmentProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // Load resources from Pidgeon.Data assembly
        _assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Pidgeon.Data")
            ?? throw new InvalidOperationException("Pidgeon.Data assembly not found");
    }

    /// <summary>
    /// Gets a segment definition by code (e.g., "MSH", "PID", "EVN").
    /// Loads from embedded JSON resources in Pidgeon.Data.
    /// </summary>
    public async Task<Result<SegmentSchema>> GetSegmentAsync(string segmentCode)
    {
        try
        {
            // Normalize the segment code
            var normalizedCode = segmentCode.ToUpperInvariant();

            // Check cache first
            if (_cache.TryGetValue(normalizedCode, out var cachedSegment))
            {
                return Result<SegmentSchema>.Success(cachedSegment);
            }

            // Build resource name for embedded JSON file
            var resourceName = $"data.standards.hl7v23.segments.{normalizedCode.ToLowerInvariant()}.json";

            _logger.LogDebug("Loading segment {SegmentCode} from resource {ResourceName}",
                segmentCode, resourceName);

            // Load from embedded resource
            using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogDebug("Segment resource not found: {ResourceName}", resourceName);
                return Result<SegmentSchema>.Failure($"Segment {segmentCode} not found");
            }

            // Parse JSON to segment schema
            var json = await new StreamReader(stream).ReadToEndAsync();
            var jsonDoc = JsonDocument.Parse(json);
            var segmentSchema = ParseSegmentSchemaFromJson(jsonDoc.RootElement);

            // Cache for future requests
            _cache[normalizedCode] = segmentSchema;

            _logger.LogDebug("Successfully loaded segment {SegmentCode} with {FieldCount} fields",
                segmentCode, segmentSchema.Fields.Count);

            return Result<SegmentSchema>.Success(segmentSchema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading segment {SegmentCode}", segmentCode);
            return Result<SegmentSchema>.Failure($"Failed to load segment: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses segment JSON structure into strongly-typed schema object.
    /// </summary>
    private SegmentSchema ParseSegmentSchemaFromJson(JsonElement root)
    {
        var code = root.GetProperty("code").GetString() ?? "";
        var name = root.GetProperty("name").GetString() ?? "";
        var description = root.TryGetProperty("description", out var descElement)
            ? descElement.GetString() ?? ""
            : "";

        var fields = new List<SegmentField>();
        if (root.TryGetProperty("fields", out var fieldsArray))
        {
            foreach (var fieldElement in fieldsArray.EnumerateArray())
            {
                var field = ParseSegmentFieldFromJson(fieldElement);
                fields.Add(field);
            }
        }

        return new SegmentSchema(
            Code: code,
            Name: name,
            Description: description,
            Fields: fields.AsReadOnly()
        );
    }

    /// <summary>
    /// Parses segment field JSON structure into strongly-typed field object.
    /// </summary>
    private SegmentField ParseSegmentFieldFromJson(JsonElement fieldElement)
    {
        var position = fieldElement.GetProperty("position").GetInt32();
        var name = fieldElement.TryGetProperty("field_description", out var nameElement)
            ? nameElement.GetString() ?? ""
            : fieldElement.TryGetProperty("field_name", out var altNameElement)
                ? altNameElement.GetString() ?? ""
                : "";
        var dataType = fieldElement.GetProperty("data_type").GetString() ?? "";
        var optionality = fieldElement.GetProperty("optionality").GetString() ?? "";
        var repeatability = fieldElement.GetProperty("repeatability").GetString() ?? "";

        // Parse length, handling both string and numeric formats
        var lengthValue = 0;
        if (fieldElement.TryGetProperty("length", out var lengthElement))
        {
            if (lengthElement.ValueKind == JsonValueKind.String)
            {
                int.TryParse(lengthElement.GetString(), out lengthValue);
            }
            else if (lengthElement.ValueKind == JsonValueKind.Number)
            {
                lengthValue = lengthElement.GetInt32();
            }
        }

        // Parse table ID if present
        int? tableId = null;
        if (fieldElement.TryGetProperty("table", out var tableElement))
        {
            var tableString = tableElement.GetString();
            if (!string.IsNullOrEmpty(tableString) && int.TryParse(tableString, out var tableValue))
            {
                tableId = tableValue;
            }
        }

        var description = fieldElement.TryGetProperty("description", out var descElement)
            ? descElement.GetString()
            : null;

        return new SegmentField(
            Position: position,
            Name: name,
            DataType: dataType,
            Optionality: optionality,
            Repeatability: repeatability,
            Length: lengthValue,
            TableId: tableId,
            Description: description
        );
    }
}

/// <summary>
/// Interface for accessing HL7 segment schema definitions.
/// </summary>
public interface IHL7SegmentProvider
{
    /// <summary>
    /// Gets a segment schema definition by code.
    /// </summary>
    Task<Result<SegmentSchema>> GetSegmentAsync(string segmentCode);
}

/// <summary>
/// Represents a complete HL7 segment schema with all field definitions.
/// </summary>
public record SegmentSchema(
    string Code,
    string Name,
    string Description,
    IReadOnlyList<SegmentField> Fields);

/// <summary>
/// Represents a field definition within a segment schema.
/// </summary>
public record SegmentField(
    int Position,
    string Name,
    string DataType,
    string Optionality,
    string Repeatability,
    int Length,
    int? TableId,
    string? Description);