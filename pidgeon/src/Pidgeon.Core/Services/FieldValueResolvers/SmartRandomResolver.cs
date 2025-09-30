// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Fallback resolver that generates smart random values based on field semantics.
/// Uses field names, data types, and healthcare context to generate appropriate values.
/// Priority: 10 (lowest - only used when no other resolver can provide value)
/// </summary>
public class SmartRandomResolver : IFieldValueResolver
{
    private readonly ILogger<SmartRandomResolver> _logger;
    private readonly Random _random;

    public int Priority => 10;

    public SmartRandomResolver(ILogger<SmartRandomResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();
    }

    public async Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        await Task.Yield();

        // Generate value based on data type and field semantics
        return context.Field.DataType switch
        {
            "ST" or "TX" or "FT" => GenerateStringValue(context),
            "NM" or "SI" => GenerateNumericValue(context),
            "DT" => DateTime.Now.ToString("yyyyMMdd"),
            "TM" => DateTime.Now.ToString("HHmmss"),
            "TS" or "DTM" => DateTime.Now.ToString("yyyyMMddHHmmss"),
            "ID" or "IS" => GenerateCodedValue(context),
            _ => GenerateGenericValue(context)
        };
    }

    /// <summary>
    /// Generate string values using field semantics and healthcare context.
    /// </summary>
    private string GenerateStringValue(FieldResolutionContext context)
    {
        var fieldName = context.Field.Name?.ToLowerInvariant() ?? "";

        // Patient demographic defaults
        if (fieldName.Contains("name"))
            return "DefaultName";
        if (fieldName.Contains("address") || fieldName.Contains("street"))
            return "123 Main St";
        if (fieldName.Contains("city"))
            return "Anytown";
        if (fieldName.Contains("state"))
            return "ST";
        if (fieldName.Contains("zip") || fieldName.Contains("postal"))
            return "12345";
        if (fieldName.Contains("phone") || fieldName.Contains("telephone"))
            return "555-123-4567";

        // Location fields
        if (fieldName.Contains("room"))
            return GenerateRandomRoom();
        if (fieldName.Contains("bed"))
            return GenerateRandomBed();
        if (fieldName.Contains("facility") || fieldName.Contains("hospital"))
            return "General Hospital";
        if (fieldName.Contains("department") || fieldName.Contains("unit"))
            return "General Medicine";

        // Clinical fields
        if (fieldName.Contains("medication") || fieldName.Contains("drug"))
            return "Unknown Medication";
        if (fieldName.Contains("result") || fieldName.Contains("value"))
            return "Normal";
        if (fieldName.Contains("diagnosis"))
            return "General Diagnosis";

        // Generic fallback
        return $"Generated_{context.Field.Name?.Replace(" ", "_") ?? "Value"}";
    }

    /// <summary>
    /// Generate numeric values using field semantics.
    /// </summary>
    private string GenerateNumericValue(FieldResolutionContext context)
    {
        var fieldName = context.Field.Name?.ToLowerInvariant() ?? "";

        // Sequence numbers and IDs
        if (fieldName.Contains("sequence") || fieldName.Contains("set id"))
            return "1";
        if (fieldName.Contains("patient") && fieldName.Contains("id"))
            return _random.Next(100000, 999999).ToString();
        if (fieldName.Contains("account"))
            return _random.Next(1000000, 9999999).ToString();

        // Age fields
        if (fieldName.Contains("age"))
            return _random.Next(18, 80).ToString();

        // Generic numeric
        return _random.Next(1, 100).ToString();
    }

    /// <summary>
    /// Generate coded values by examining field semantics.
    /// </summary>
    private string GenerateCodedValue(FieldResolutionContext context)
    {
        var fieldName = context.Field.Name?.ToLowerInvariant() ?? "";

        // Common healthcare coded values
        if (fieldName.Contains("sex") || fieldName.Contains("gender"))
            return _random.Next(2) == 0 ? "M" : "F";
        if (fieldName.Contains("marital"))
            return new[] { "S", "M", "D", "W" }[_random.Next(4)];
        if (fieldName.Contains("race"))
            return new[] { "1002-5", "2028-9", "2054-5", "2076-8" }[_random.Next(4)];
        if (fieldName.Contains("admission"))
            return new[] { "E", "I", "O", "P" }[_random.Next(4)];
        if (fieldName.Contains("class"))
            return new[] { "E", "I", "O", "P", "R" }[_random.Next(5)];
        if (fieldName.Contains("status"))
            return new[] { "A", "I", "P" }[_random.Next(3)];

        // Default coded value
        return "U"; // Unknown/Other
    }

    /// <summary>
    /// Generate generic value when data type is unknown or unhandled.
    /// </summary>
    private string GenerateGenericValue(FieldResolutionContext context)
    {
        return $"Generated_{context.Field.Name?.Replace(" ", "_") ?? "Field"}";
    }

    /// <summary>
    /// Generate random room number.
    /// </summary>
    private string GenerateRandomRoom() => $"{_random.Next(100, 999)}";

    /// <summary>
    /// Generate random bed identifier.
    /// </summary>
    private string GenerateRandomBed() => $"{_random.Next(100, 999)}{(char)('A' + _random.Next(0, 4))}";
}