// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text;
using System.Text.RegularExpressions;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;

/// <summary>
/// Builds HL7 v2.3 OBR (Observation Request) segments with strict standards compliance.
/// Handles lab observation requests including ordering provider and test details.
/// </summary>
public class OBRSegmentBuilder : IHL7SegmentBuilder<ObservationRequest>
{
    private const string FieldSeparator = "|";
    
    public string Build(ObservationRequest input, int setId, GenerationOptions options)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var sb = new StringBuilder("OBR");
        sb.Append(FieldSeparator);
        
        // OBR-1: Set ID (REQUIRED)
        sb.Append(setId).Append(FieldSeparator);
        
        // OBR-2: Placer Order Number (optional)
        sb.Append(FieldSeparator);
        
        // OBR-3: Filler Order Number (optional)
        sb.Append(FieldSeparator);
        
        // OBR-4: Universal Service Identifier (REQUIRED)
        // Format: Identifier^Text^Coding System
        sb.Append(FormatUniversalServiceId(input)).Append(FieldSeparator);
        
        // OBR-5: Priority (optional)
        sb.Append(FieldSeparator);
        
        // OBR-6: Requested Date/Time (optional)
        sb.Append(FieldSeparator);
        
        // OBR-7: Observation Date/Time (optional but commonly used)
        if (input.ObservationDateTime.HasValue)
            sb.Append(FormatHL7Timestamp(input.ObservationDateTime.Value));
        sb.Append(FieldSeparator);
        
        // OBR-8: Observation End Date/Time (optional)
        sb.Append(FieldSeparator);
        
        // OBR-9: Collection Volume (optional)
        sb.Append(FieldSeparator);
        
        // OBR-10: Collector Identifier (optional)
        sb.Append(FieldSeparator);
        
        // OBR-11: Specimen Action Code (optional)
        sb.Append(FieldSeparator);
        
        // OBR-12: Danger Code (optional)
        sb.Append(FieldSeparator);
        
        // OBR-13: Relevant Clinical Information (optional)
        sb.Append(FieldSeparator);
        
        // OBR-14: Specimen Received Date/Time (optional)
        sb.Append(FieldSeparator);
        
        // OBR-15: Specimen Source (optional)
        sb.Append(FieldSeparator);
        
        // OBR-16: Ordering Provider (optional but commonly used)
        if (input.OrderingProvider != null)
        {
            sb.Append(input.OrderingProvider.Id ?? "")
              .Append("^")
              .Append(EscapeHL7Field(input.OrderingProvider.Name?.Family ?? ""))
              .Append("^")
              .Append(EscapeHL7Field(input.OrderingProvider.Name?.Given ?? ""));
        }
        
        return sb.ToString();
    }

    public SegmentValidationResult Validate(string segment)
    {
        var result = new SegmentValidationResult
        {
            SegmentType = "OBR",
            IsValid = true
        };

        if (string.IsNullOrWhiteSpace(segment))
        {
            result = result with { IsValid = false };
            result.Errors.Add("Segment cannot be null or empty");
            return result;
        }

        var fields = segment.Split('|');
        
        // Basic structure validation
        if (!segment.StartsWith("OBR"))
        {
            result = result with { IsValid = false };
            result.Errors.Add("Segment must start with 'OBR'");
        }

        if (fields.Length < 5)
        {
            result = result with { IsValid = false };
            result.Errors.Add("OBR segment must have minimum 5 fields");
        }

        // OBR-1: Set ID validation
        if (fields.Length > 1)
        {
            if (!int.TryParse(fields[1], out var setId) || setId <= 0)
            {
                result = result with { IsValid = false };
                result.Errors.Add("OBR-1 (Set ID) must be a positive integer");
            }
        }

        // OBR-4: Universal Service Identifier validation (required)
        if (fields.Length > 4)
        {
            if (string.IsNullOrWhiteSpace(fields[4]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("OBR-4 (Universal Service Identifier) is required");
            }
            else if (!fields[4].Contains("^"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("OBR-4 (Universal Service Identifier) must contain component separators");
            }
        }

        // OBR-7: Observation Date/Time validation (if present)
        if (fields.Length > 7 && !string.IsNullOrWhiteSpace(fields[7]))
        {
            if (!Regex.IsMatch(fields[7], @"^\d{8,14}$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("OBR-7 (Observation Date/Time) must be valid timestamp format");
            }
        }

        return result;
    }

    public SegmentMetadata GetMetadata()
    {
        return new SegmentMetadata
        {
            SegmentType = "OBR",
            Description = "Observation Request - Contains information about lab orders and observation requests",
            MinimumFields = 5,
            RequiredFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 1, Name = "Set ID", DataType = "SI", IsRequired = true, MaxLength = 4, Description = "Sequential number for multiple OBR segments" },
                new() { FieldNumber = 4, Name = "Universal Service Identifier", DataType = "CE", IsRequired = true, MaxLength = 200, Description = "Identifies the service being requested" }
            },
            OptionalFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 2, Name = "Placer Order Number", DataType = "EI", IsRequired = false, MaxLength = 75, Description = "Unique order number from ordering system" },
                new() { FieldNumber = 3, Name = "Filler Order Number", DataType = "EI", IsRequired = false, MaxLength = 75, Description = "Unique order number from fulfilling system" },
                new() { FieldNumber = 7, Name = "Observation Date/Time", DataType = "TS", IsRequired = false, MaxLength = 26, ValidationPattern = @"^\d{8,14}$", Description = "Date/time of observation" },
                new() { FieldNumber = 16, Name = "Ordering Provider", DataType = "XCN", IsRequired = false, MaxLength = 250, Description = "Provider who ordered the test" }
            }
        };
    }

    private static string FormatUniversalServiceId(ObservationRequest request)
    {
        // Default lab test format: Code^Description^Coding System
        // Ensure we never return empty values for required field
        var testCode = string.IsNullOrWhiteSpace(request.TestCode) ? "CBC" : request.TestCode;
        var testDescription = string.IsNullOrWhiteSpace(request.TestDescription) ? "Complete Blood Count" : request.TestDescription;
        var codingSystem = string.IsNullOrWhiteSpace(request.CodingSystem) ? "LN" : request.CodingSystem; // LOINC
        
        return $"{testCode}^{testDescription}^{codingSystem}";
    }

    private static string EscapeHL7Field(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        
        // HL7 escape sequences for special characters
        return value
            .Replace("\\", "\\E\\")  // Escape character
            .Replace("|", "\\F\\")   // Field separator
            .Replace("^", "\\S\\")   // Component separator
            .Replace("&", "\\T\\")   // Subcomponent separator
            .Replace("~", "\\R\\");  // Repetition separator
    }

    private static string FormatHL7Timestamp(DateTime dateTime)
    {
        // HL7 v2.3 timestamp format: YYYYMMDDHHMMSS
        return dateTime.ToString("yyyyMMddHHmmss");
    }
}

/// <summary>
/// Input model for OBR segment builder representing lab observation request data.
/// </summary>
public record ObservationRequest
{
    public string? TestCode { get; init; }
    public string? TestDescription { get; init; }
    public string? CodingSystem { get; init; }
    public DateTime? ObservationDateTime { get; init; }
    public Provider? OrderingProvider { get; init; }
}