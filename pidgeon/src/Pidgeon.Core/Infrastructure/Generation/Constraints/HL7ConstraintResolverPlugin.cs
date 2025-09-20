// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Pidgeon.Core;
using Pidgeon.Core.Application.Interfaces.Reference;
using Pidgeon.Core.Domain.Configuration;

namespace Pidgeon.Core.Infrastructure.Generation.Constraints;

/// <summary>
/// HL7 v2.3 constraint resolver plugin.
/// Uses JsonHL7ReferencePlugin to load segment definitions and table values for constraint-aware generation.
/// </summary>
public class HL7ConstraintResolverPlugin : IConstraintResolverPlugin
{
    private readonly IStandardReferenceService _referenceService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HL7ConstraintResolverPlugin> _logger;

    private static readonly Regex HL7FieldPattern = new(@"^[A-Z]{2,3}\.(\d+)(?:\.(\d+))?$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public string StandardId => "hl7v23";
    public int Priority => 100;

    private string? _dataBasePath;

    public HL7ConstraintResolverPlugin(
        IStandardReferenceService referenceService,
        IMemoryCache cache,
        ILogger<HL7ConstraintResolverPlugin> logger)
    {
        _referenceService = referenceService ?? throw new ArgumentNullException(nameof(referenceService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CanResolve(string context)
    {
        if (string.IsNullOrWhiteSpace(context))
            return false;

        // Handle HL7 field contexts: PID.8, MSH.9.1, OBR.4, etc.
        return HL7FieldPattern.IsMatch(context);
    }

    public async Task<Result<FieldConstraints>> GetConstraintsAsync(string context)
    {
        var cacheKey = $"hl7-constraints:{context}";
        if (_cache.TryGetValue<FieldConstraints>(cacheKey, out var cached))
        {
            return Result<FieldConstraints>.Success(cached!);
        }

        try
        {
            // Parse HL7 context (e.g., "PID.8")
            var match = HL7FieldPattern.Match(context);
            if (!match.Success)
            {
                return Result<FieldConstraints>.Failure($"Invalid HL7 context format: {context}");
            }

            var segmentName = context.Split('.')[0];
            var fieldPosition = int.Parse(match.Groups[1].Value);

            // Load segment definition
            var segmentResult = await _referenceService.LookupAsync(StandardId, segmentName);
            if (!segmentResult.IsSuccess)
            {
                return Result<FieldConstraints>.Failure($"Segment {segmentName} not found");
            }

            // Extract field constraints from segment definition
            var constraints = await ExtractFieldConstraintsAsync(segmentResult.Value, fieldPosition);

            // Cache for 30 minutes
            _cache.Set(cacheKey, constraints, TimeSpan.FromMinutes(30));

            return Result<FieldConstraints>.Success(constraints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting constraints for {Context}", context);
            return Result<FieldConstraints>.Failure($"Failed to get constraints: {ex.Message}");
        }
    }

    public async Task<Result<object>> GenerateValueAsync(FieldConstraints constraints, Random random)
    {
        try
        {
            // Generate based on constraint priority
            if (constraints.TableReference != null)
            {
                return await GenerateFromTableAsync(constraints.TableReference, random);
            }

            if (constraints.AllowedValues?.Any() == true)
            {
                var value = constraints.AllowedValues[random.Next(constraints.AllowedValues.Count)];
                return Result<object>.Success(value);
            }

            if (constraints.DataType != null)
            {
                return GenerateByDataType(constraints.DataType, constraints, random);
            }

            // Default generation with length constraints
            return GenerateDefaultValue(constraints, random);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating value for constraints");
            return Result<object>.Failure($"Value generation failed: {ex.Message}");
        }
    }

    public async Task<Result<bool>> ValidateValueAsync(object value, FieldConstraints constraints)
    {
        try
        {
            if (value == null)
            {
                return constraints.Required
                    ? Result<bool>.Failure("Required field cannot be null")
                    : Result<bool>.Success(true);
            }

            var stringValue = value.ToString() ?? string.Empty;

            // Validate table reference
            if (constraints.TableReference != null)
            {
                return await ValidateAgainstTableAsync(stringValue, constraints.TableReference);
            }

            // Validate allowed values
            if (constraints.AllowedValues?.Any() == true)
            {
                if (!constraints.AllowedValues.Contains(stringValue))
                {
                    return Result<bool>.Failure($"Value '{stringValue}' not in allowed values");
                }
            }

            // Validate length constraints
            if (constraints.MaxLength.HasValue && stringValue.Length > constraints.MaxLength.Value)
            {
                return Result<bool>.Failure($"Value exceeds maximum length {constraints.MaxLength.Value}");
            }

            if (constraints.MinLength.HasValue && stringValue.Length < constraints.MinLength.Value)
            {
                return Result<bool>.Failure($"Value below minimum length {constraints.MinLength.Value}");
            }

            // Validate data type
            if (constraints.DataType != null)
            {
                return ValidateDataType(stringValue, constraints.DataType);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating value");
            return Result<bool>.Failure($"Validation failed: {ex.Message}");
        }
    }

    private async Task<FieldConstraints> ExtractFieldConstraintsAsync(
        Pidgeon.Core.Domain.Reference.Entities.StandardElement segmentElement,
        int fieldPosition)
    {
        try
        {
            // Load the segment JSON file directly
            var segmentName = segmentElement.Path.ToLowerInvariant();
            var segmentFilePath = GetSegmentFilePath(segmentName);

            if (File.Exists(segmentFilePath))
            {
                var json = await File.ReadAllTextAsync(segmentFilePath);
                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("fields", out var fieldsArray) && fieldsArray.ValueKind == JsonValueKind.Array)
                {
                    // Find the field with matching position
                    foreach (var field in fieldsArray.EnumerateArray())
                    {
                        if (field.TryGetProperty("position", out var positionProp) &&
                            positionProp.ValueKind == JsonValueKind.Number &&
                            positionProp.GetInt32() == fieldPosition)
                        {
                            var constraints = new FieldConstraints
                            {
                                DataType = GetPropertyValue(field, "data_type"),
                                MaxLength = GetIntProperty(field, "length"),
                                Required = GetPropertyValue(field, "optionality") == "R",
                                Repeating = GetPropertyValue(field, "repeatability") == "∞",
                                TableReference = GetTableReference(field)
                            };

                            // Enhance with known HL7 patterns
                            constraints = await EnhanceWithHL7PatternsAsync(constraints, fieldPosition);
                            return constraints;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing segment JSON for field {Position}", fieldPosition);
        }

        // Fallback: create basic constraints
        var fallbackConstraints = new FieldConstraints
        {
            Required = false,
            MaxLength = 100
        };

        return await EnhanceWithHL7PatternsAsync(fallbackConstraints, fieldPosition);
    }

    private string? GetTableReference(JsonElement field)
    {
        var tableValue = GetPropertyValue(field, "table");
        return string.IsNullOrEmpty(tableValue) ? null : tableValue;
    }

    private string GetSegmentFilePath(string segmentName)
    {
        EnsureDataPathInitialized();
        return Path.Combine(_dataBasePath!, "segments", $"{segmentName}.json");
    }

    private string GetTableFilePath(string tableId)
    {
        EnsureDataPathInitialized();
        return Path.Combine(_dataBasePath!, "tables", $"{tableId}.json");
    }

    private void EnsureDataPathInitialized()
    {
        if (_dataBasePath != null)
            return;

        // Try multiple paths to find data directory
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "data", "standards", "hl7v23"),
            Path.Combine(Directory.GetCurrentDirectory(), "data", "standards", "hl7v23"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "data", "standards", "hl7v23")
        };

        _dataBasePath = possiblePaths.FirstOrDefault(Directory.Exists)
                        ?? possiblePaths[0]; // fallback to first path

        _logger.LogDebug("HL7ConstraintResolverPlugin using data path: {DataPath}", _dataBasePath);
    }

    private string? ExtractTableReference(Pidgeon.Core.Domain.Reference.Entities.StandardElement fieldElement)
    {
        // Look for table reference in field description or examples
        var description = fieldElement.Description?.ToLowerInvariant() ?? "";

        // Common patterns for table references
        if (description.Contains("table") && description.Contains("0001"))
            return "0001";
        if (description.Contains("table") && description.Contains("0002"))
            return "0002";
        if (description.Contains("table") && description.Contains("0004"))
            return "0004";

        // Check if any examples look like table codes
        foreach (var example in fieldElement.Examples)
        {
            if (example.Length <= 3 && example.All(char.IsLetterOrDigit))
            {
                // Could be a table value, but we need the table ID
                // For now, make educated guesses based on field patterns
                break;
            }
        }

        return null;
    }

    private Task<FieldConstraints> EnhanceWithHL7PatternsAsync(FieldConstraints constraints, int fieldPosition)
    {
        // Add known HL7 patterns and enhancements
        var enhanced = constraints.DataType?.ToUpperInvariant() switch
        {
            "IS" when constraints.TableReference != null => constraints with
            {
                MaxLength = 1 // Most IS fields are single character
            },
            "ST" => constraints with
            {
                MaxLength = constraints.MaxLength ?? 200 // Standard text field length
            },
            "NM" => constraints with
            {
                Pattern = @"^\d+(\.\d+)?$", // Numeric pattern
                Numeric = new NumericConstraints(0, 999999, null, null)
            },
            "DT" => constraints with
            {
                Pattern = @"^\d{8}$", // YYYYMMDD
                DateTime = new DateTimeConstraints(
                    DateTime.Today.AddYears(-120),
                    DateTime.Today.AddYears(10),
                    "yyyyMMdd")
            },
            "TS" => constraints with
            {
                Pattern = @"^\d{8,14}$", // YYYYMMDD[HHMMSS]
                DateTime = new DateTimeConstraints(
                    DateTime.Today.AddYears(-120),
                    DateTime.Today.AddYears(10),
                    "yyyyMMddHHmmss")
            },
            _ => constraints
        };

        return Task.FromResult(enhanced);
    }

    private async Task<Result<object>> GenerateFromTableAsync(string tableId, Random random)
    {
        var cacheKey = $"hl7-table:{tableId}";
        if (_cache.TryGetValue<List<string>>(cacheKey, out var cachedValues))
        {
            if (cachedValues!.Any())
            {
                return Result<object>.Success(cachedValues[random.Next(cachedValues.Count)]);
            }
        }

        try
        {
            // Try loading table values directly from JSON
            var values = await ExtractTableValuesAsync(tableId);

            // If no values found, try lookup from reference service
            if (!values.Any())
            {
                var tableResult = await _referenceService.LookupAsync(StandardId, tableId);
                if (tableResult.IsSuccess)
                {
                    values = ExtractTableValues(tableResult.Value);
                }
            }

            if (!values.Any())
            {
                return GenerateFallbackTableValue(tableId, random);
            }

            // Cache for 1 hour
            _cache.Set(cacheKey, values, TimeSpan.FromHours(1));

            var selectedValue = values[random.Next(values.Count)];
            return Result<object>.Success(selectedValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading table {TableId}, using fallback", tableId);
            return GenerateFallbackTableValue(tableId, random);
        }
    }

    private async Task<List<string>> ExtractTableValuesAsync(string tableId)
    {
        var values = new List<string>();

        try
        {
            var tableFilePath = GetTableFilePath(tableId);
            if (File.Exists(tableFilePath))
            {
                var json = await File.ReadAllTextAsync(tableFilePath);
                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("values", out var valuesArray) && valuesArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var valueItem in valuesArray.EnumerateArray())
                    {
                        var value = GetPropertyValue(valueItem, "value");
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            values.Add(value);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting table values for {TableId}", tableId);
        }

        return values;
    }

    private List<string> ExtractTableValues(Pidgeon.Core.Domain.Reference.Entities.StandardElement tableElement)
    {
        var values = new List<string>();

        try
        {
            // First try extracting from ValidValues collection
            foreach (var validValue in tableElement.ValidValues)
            {
                if (!string.IsNullOrWhiteSpace(validValue.Code))
                {
                    values.Add(validValue.Code);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting table values");
        }

        return values;
    }

    private Result<object> GenerateByDataType(string dataType, FieldConstraints constraints, Random random)
    {
        return dataType.ToUpperInvariant() switch
        {
            "ST" => GenerateString(constraints, random),
            "NM" => GenerateNumeric(constraints, random),
            "DT" => GenerateDate(constraints, random),
            "TM" => GenerateTime(random),
            "TS" => GenerateTimestamp(random),
            "ID" => GenerateIdentifier(constraints, random),
            "IS" => GenerateCodedValue(constraints, random),
            "CE" => Result<object>.Success("CE_VALUE^CODED_ELEMENT"),
            "XPN" => Result<object>.Success("LAST^FIRST^MIDDLE"),
            "XAD" => Result<object>.Success("123 MAIN ST^^CITY^ST^12345"),
            "XTN" => Result<object>.Success("555-1234"),
            _ => GenerateDefaultValue(constraints, random)
        };
    }

    private Result<object> GenerateString(FieldConstraints constraints, Random random)
    {
        var maxLength = constraints.MaxLength ?? 20;
        var length = random.Next(1, Math.Min(maxLength, 20));
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ ";
        var result = new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray()).Trim();
        return Result<object>.Success(result);
    }

    private Result<object> GenerateNumeric(FieldConstraints constraints, Random random)
    {
        var min = (int)(constraints.Numeric?.MinValue ?? 0);
        var max = (int)(constraints.Numeric?.MaxValue ?? 999999);
        return Result<object>.Success(random.Next(min, max));
    }

    private Result<object> GenerateDate(FieldConstraints constraints, Random random)
    {
        var minDate = constraints.DateTime?.MinDate ?? DateTime.Today.AddYears(-50);
        var maxDate = constraints.DateTime?.MaxDate ?? DateTime.Today;
        var range = (maxDate - minDate).Days;
        var randomDate = minDate.AddDays(random.Next(range));
        return Result<object>.Success(randomDate.ToString("yyyyMMdd"));
    }

    private Result<object> GenerateTime(Random random)
    {
        var time = TimeSpan.FromMinutes(random.Next(0, 24 * 60));
        return Result<object>.Success(time.ToString(@"hhmmss"));
    }

    private Result<object> GenerateTimestamp(Random random)
    {
        var date = DateTime.Today.AddDays(random.Next(-365, 365));
        var time = TimeSpan.FromMinutes(random.Next(0, 24 * 60));
        var timestamp = date.Add(time);
        return Result<object>.Success(timestamp.ToString("yyyyMMddHHmmss"));
    }

    private Result<object> GenerateIdentifier(FieldConstraints constraints, Random random)
    {
        var length = constraints.MaxLength ?? 10;
        var id = random.Next(1, (int)Math.Pow(10, Math.Min(length, 9)));
        return Result<object>.Success(id.ToString().PadLeft(length, '0'));
    }

    private Result<object> GenerateCodedValue(FieldConstraints constraints, Random random)
    {
        // For IS (coded value) fields without table reference
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var length = constraints.MaxLength ?? 3;
        var result = new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        return Result<object>.Success(result);
    }

    private Result<object> GenerateDefaultValue(FieldConstraints constraints, Random random)
    {
        var length = Math.Min(constraints.MaxLength ?? 10, 10);
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        return Result<object>.Success(result);
    }

    private Result<object> GenerateFallbackTableValue(string tableId, Random random)
    {
        // Fallback values for common HL7 tables
        return tableId switch
        {
            "0001" => Result<object>.Success(new[] { "M", "F", "O", "U" }[random.Next(4)]),
            "0002" => Result<object>.Success(new[] { "S", "M", "D", "W" }[random.Next(4)]),
            "0004" => Result<object>.Success(new[] { "E", "I", "O", "U" }[random.Next(4)]),
            _ => Result<object>.Success("UNK")
        };
    }

    private async Task<Result<bool>> ValidateAgainstTableAsync(string value, string tableId)
    {
        try
        {
            var tableResult = await _referenceService.LookupAsync(StandardId, tableId);
            if (!tableResult.IsSuccess)
            {
                return Result<bool>.Success(true); // Allow if table not found
            }

            var validValues = ExtractTableValues(tableResult.Value);
            if (!validValues.Any())
            {
                return Result<bool>.Success(true); // Allow if no values found
            }

            return validValues.Contains(value)
                ? Result<bool>.Success(true)
                : Result<bool>.Failure($"Value '{value}' not found in table {tableId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating against table {TableId}", tableId);
            return Result<bool>.Success(true); // Allow on validation error
        }
    }

    private Result<bool> ValidateDataType(string value, string dataType)
    {
        return dataType.ToUpperInvariant() switch
        {
            "NM" => decimal.TryParse(value, out _)
                ? Result<bool>.Success(true)
                : Result<bool>.Failure($"'{value}' is not a valid number"),
            "DT" => Regex.IsMatch(value, @"^\d{8}$")
                ? Result<bool>.Success(true)
                : Result<bool>.Failure($"'{value}' is not a valid date (YYYYMMDD)"),
            "TS" => Regex.IsMatch(value, @"^\d{8,14}$")
                ? Result<bool>.Success(true)
                : Result<bool>.Failure($"'{value}' is not a valid timestamp"),
            _ => Result<bool>.Success(true) // Allow other types
        };
    }

    private static string? GetPropertyValue(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static int? GetIntProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetInt32()
            : null;
    }

    private static bool? GetBoolProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            return prop.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            };
        }
        return null;
    }
}