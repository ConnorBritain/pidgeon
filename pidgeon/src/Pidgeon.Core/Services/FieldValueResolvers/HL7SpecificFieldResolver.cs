// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Resolves HL7-specific field values that have fixed semantic meanings.
/// Handles MSH fields, message control IDs, timestamps, and other HL7 standard values.
/// Priority: 90 (high - HL7 semantics should override random generation)
/// </summary>
public class HL7SpecificFieldResolver : IFieldValueResolver
{
    private readonly ILogger<HL7SpecificFieldResolver> _logger;
    private readonly Random _random;

    public int Priority => 90;

    public HL7SpecificFieldResolver(ILogger<HL7SpecificFieldResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();
    }

    public async Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        await Task.Yield();

        // Handle MSH segment fields with fixed HL7 meanings
        if (context.SegmentCode == "MSH")
        {
            return ResolveMSHField(context);
        }

        // Handle other HL7-specific fields across segments
        return ResolveCommonHL7Field(context);
    }

    /// <summary>
    /// Resolve MSH segment fields with proper HL7 semantics.
    /// These fields have fixed meanings per HL7 v2.3 specification.
    /// </summary>
    private string? ResolveMSHField(FieldResolutionContext context)
    {
        return context.FieldPosition switch
        {
            1 => "|",                                           // Field Separator - always "|"
            2 => "^~\\&",                                       // Encoding Characters - always "^~\&"
            3 => "PIDGEON^^L",                                  // Sending Application
            4 => "PIDGEON_FACILITY",                           // Sending Facility
            5 => "TARGET^^L",                                   // Receiving Application
            6 => "TARGET_FACILITY",                            // Receiving Facility
            7 => DateTime.Now.ToString("yyyyMMddHHmmss"),      // Date/Time of Message
            8 => "",                                           // Security (usually empty)
            9 => context.GenerationContext.MessageType,        // Message Type
            10 => GenerateRandomId(),                          // Message Control ID
            11 => "P",                                         // Processing ID (P=Production)
            12 => "2.3",                                       // Version ID (HL7 v2.3)
            _ => null                                          // Let other resolvers handle
        };
    }

    /// <summary>
    /// Resolve common HL7 fields that appear across multiple segments.
    /// Uses field name patterns to identify HL7-specific semantics.
    /// </summary>
    private string? ResolveCommonHL7Field(FieldResolutionContext context)
    {
        var fieldName = context.Field.Name?.ToLowerInvariant() ?? "";
        var fieldDescription = context.Field.Description?.ToLowerInvariant() ?? "";

        // Timestamp fields
        if (fieldName.Contains("time") || fieldName.Contains("date") ||
            fieldDescription.Contains("time") || fieldDescription.Contains("date"))
        {
            if (context.Field.DataType == "TS" || context.Field.DataType == "DTM")
                return DateTime.Now.ToString("yyyyMMddHHmmss");
            if (context.Field.DataType == "DT")
                return DateTime.Now.ToString("yyyyMMdd");
            if (context.Field.DataType == "TM")
                return DateTime.Now.ToString("HHmmss");
        }

        // Message control and sequence IDs
        if (fieldName.Contains("control") && fieldName.Contains("id"))
            return GenerateRandomId();
        if (fieldName.Contains("sequence") || fieldName.Contains("set id"))
            return "1";

        // Version fields
        if (fieldName.Contains("version"))
            return "2.3";

        // Processing ID fields
        if (fieldName.Contains("processing") && fieldName.Contains("id"))
            return "P";

        // No HL7-specific handling for this field
        return null;
    }

    /// <summary>
    /// Generate random message control ID in HL7-appropriate format.
    /// </summary>
    private string GenerateRandomId() => _random.Next(100000, 999999).ToString();
}