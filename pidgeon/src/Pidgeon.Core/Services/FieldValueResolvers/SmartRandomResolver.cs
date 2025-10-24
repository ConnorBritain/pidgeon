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
        var description = context.Field.Description?.ToLowerInvariant() ?? "";
        var combined = $"{fieldName} {description}";

        // Provider/clinician/physician/observer names
        if (combined.Contains("clinician") || combined.Contains("physician") ||
            combined.Contains("provider") || combined.Contains("observer") ||
            combined.Contains("practitioner") || combined.Contains("doctor"))
            return "Dr. Smith";

        // Organization/clinic names
        if (combined.Contains("organization") || combined.Contains("clinic") ||
            combined.Contains("facility name") || combined.Contains("institution"))
            return "General Healthcare Center";

        // Location names (including pending locations, birth place, etc.)
        if (combined.Contains("location") || combined.Contains("place"))
        {
            if (combined.Contains("birth"))
                return "General Hospital";
            if (combined.Contains("pending") || combined.Contains("prior"))
                return "Ward 3B";
            return "Main Building";
        }

        // Patient demographic defaults
        if (combined.Contains("name") && !combined.Contains("file"))
            return "DefaultName";
        if (combined.Contains("maiden"))
            return "Jones";
        if (combined.Contains("alias"))
            return "Smith";
        if (combined.Contains("address") || combined.Contains("street"))
            return "123 Main St";
        if (combined.Contains("city"))
            return "Anytown";
        if (combined.Contains("state") && !combined.Contains("patient"))
            return "CA";
        if (combined.Contains("zip") || combined.Contains("postal"))
            return "12345";
        if (combined.Contains("country"))
            return "USA";

        // Room and bed fields
        if (combined.Contains("room"))
            return GenerateRandomRoom();
        if (combined.Contains("bed"))
            return GenerateRandomBed();

        // Facility/department fields
        if (combined.Contains("facility") || combined.Contains("hospital"))
            return "General Hospital";
        if (combined.Contains("department") || combined.Contains("unit"))
            return "General Medicine";
        if (combined.Contains("ward"))
            return $"Ward {_random.Next(1, 10)}";

        // Clinical fields
        if (combined.Contains("medication") || combined.Contains("drug"))
            return "Unknown Medication";
        if (combined.Contains("result") || combined.Contains("value"))
            return "Normal";
        if (combined.Contains("diagnosis"))
            return "General Diagnosis";

        // Description fields
        if (combined.Contains("description"))
            return "Standard description";
        if (combined.Contains("comment") || combined.Contains("notes"))
            return "No additional comments";
        if (combined.Contains("reason"))
            return "Routine care";

        return "";
    }

    /// <summary>
    /// Generate numeric values using field semantics.
    /// </summary>
    private string GenerateNumericValue(FieldResolutionContext context)
    {
        var fieldName = context.Field.Name?.ToLowerInvariant() ?? "";
        var description = context.Field.Description?.ToLowerInvariant() ?? "";
        var combined = $"{fieldName} {description}";

        // Sequence numbers and IDs
        if (combined.Contains("sequence") || combined.Contains("set id"))
            return "1";
        if (combined.Contains("patient") && combined.Contains("id"))
            return _random.Next(100000, 999999).ToString();
        if (combined.Contains("account"))
            return _random.Next(1000000, 9999999).ToString();

        // Age fields
        if (combined.Contains("age"))
            return _random.Next(18, 80).ToString();

        // Financial/cost fields
        if (combined.Contains("cost") || combined.Contains("charge") ||
            combined.Contains("amount") || combined.Contains("reimbursement") ||
            combined.Contains("payment"))
            return $"{_random.Next(100, 10000)}.00";

        // Days/length of stay
        if (combined.Contains("days") || combined.Contains("length"))
            return _random.Next(1, 14).ToString();

        // Counts/quantities
        if (combined.Contains("count") || combined.Contains("quantity"))
            return _random.Next(1, 100).ToString();

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
        return "";
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