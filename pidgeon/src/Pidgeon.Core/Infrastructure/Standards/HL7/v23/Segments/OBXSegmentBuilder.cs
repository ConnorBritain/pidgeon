// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text;
using System.Text.RegularExpressions;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;

/// <summary>
/// Builds HL7 v2.3 OBX (Observation Result) segments with strict standards compliance.
/// Handles lab observation results with proper value types and validation.
/// </summary>
public class OBXSegmentBuilder : IHL7SegmentBuilder<ObservationResult>
{
    private const string FieldSeparator = "|";
    
    public string Build(ObservationResult input, int setId, GenerationOptions options)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var sb = new StringBuilder("OBX");
        sb.Append(FieldSeparator);
        
        // OBX-1: Set ID (REQUIRED)
        sb.Append(setId).Append(FieldSeparator);
        
        // OBX-2: Value Type (REQUIRED)
        sb.Append(DetermineValueType(input)).Append(FieldSeparator);
        
        // OBX-3: Observation Identifier (REQUIRED)
        sb.Append(FormatObservationIdentifier(input)).Append(FieldSeparator);
        
        // OBX-4: Observation Sub-ID (optional)
        sb.Append(FieldSeparator);
        
        // OBX-5: Observation Value (REQUIRED)
        sb.Append(FormatObservationValue(input)).Append(FieldSeparator);
        
        // OBX-6: Units (optional but commonly used)
        if (!string.IsNullOrEmpty(input.Units))
            sb.Append(EscapeHL7Field(input.Units));
        sb.Append(FieldSeparator);
        
        // OBX-7: References Range (optional)
        if (!string.IsNullOrEmpty(input.ReferenceRange))
            sb.Append(EscapeHL7Field(input.ReferenceRange));
        sb.Append(FieldSeparator);
        
        // OBX-8: Abnormal Flags (optional)
        if (!string.IsNullOrEmpty(input.AbnormalFlags))
            sb.Append(input.AbnormalFlags);
        sb.Append(FieldSeparator);
        
        // OBX-9: Probability (optional)
        sb.Append(FieldSeparator);
        
        // OBX-10: Nature of Abnormal Test (optional)
        sb.Append(FieldSeparator);
        
        // OBX-11: Observation Result Status (REQUIRED)
        sb.Append(input.ResultStatus ?? "F").Append(FieldSeparator); // Default to "F" (Final)
        
        // OBX-12: Effective Date of Reference Range (optional)
        sb.Append(FieldSeparator);
        
        // OBX-13: User Defined Access Checks (optional)
        sb.Append(FieldSeparator);
        
        // OBX-14: Date/Time of the Observation (optional)
        if (input.ObservationDateTime.HasValue)
            sb.Append(FormatHL7Timestamp(input.ObservationDateTime.Value));
        
        return sb.ToString();
    }

    public SegmentValidationResult Validate(string segment)
    {
        var result = new SegmentValidationResult
        {
            SegmentType = "OBX",
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
        if (!segment.StartsWith("OBX"))
        {
            result = result with { IsValid = false };
            result.Errors.Add("Segment must start with 'OBX'");
        }

        if (fields.Length < 12)
        {
            result = result with { IsValid = false };
            result.Errors.Add("OBX segment must have minimum 12 fields");
        }

        // OBX-1: Set ID validation
        if (fields.Length > 1)
        {
            if (!int.TryParse(fields[1], out var setId) || setId <= 0)
            {
                result = result with { IsValid = false };
                result.Errors.Add("OBX-1 (Set ID) must be a positive integer");
            }
        }

        // OBX-2: Value Type validation (required)
        if (fields.Length > 2)
        {
            if (string.IsNullOrWhiteSpace(fields[2]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("OBX-2 (Value Type) is required");
            }
            else if (!Regex.IsMatch(fields[2], @"^(NM|TX|CE|DT|TM|TS|ST|FT)$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("OBX-2 (Value Type) must be valid type: NM, TX, CE, DT, TM, TS, ST, or FT");
            }
        }

        // OBX-3: Observation Identifier validation (required)
        if (fields.Length > 3)
        {
            if (string.IsNullOrWhiteSpace(fields[3]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("OBX-3 (Observation Identifier) is required");
            }
            else if (!fields[3].Contains("^"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("OBX-3 (Observation Identifier) must contain component separators");
            }
        }

        // OBX-5: Observation Value validation (required)
        if (fields.Length > 5)
        {
            if (string.IsNullOrWhiteSpace(fields[5]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("OBX-5 (Observation Value) is required");
            }
        }

        // OBX-11: Result Status validation (required)
        if (fields.Length > 11)
        {
            if (string.IsNullOrWhiteSpace(fields[11]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("OBX-11 (Result Status) is required");
            }
            else if (!Regex.IsMatch(fields[11], @"^[RCPFXIDSO]$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("OBX-11 (Result Status) must be valid status code");
            }
        }

        return result;
    }

    public SegmentMetadata GetMetadata()
    {
        return new SegmentMetadata
        {
            SegmentType = "OBX",
            Description = "Observation Result - Contains actual lab results and observation values",
            MinimumFields = 12,
            RequiredFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 1, Name = "Set ID", DataType = "SI", IsRequired = true, MaxLength = 4, Description = "Sequential number for multiple OBX segments" },
                new() { FieldNumber = 2, Name = "Value Type", DataType = "ID", IsRequired = true, MaxLength = 2, ValidationPattern = @"^(NM|TX|CE|DT|TM|TS|ST|FT)$", Description = "Data type of observation value" },
                new() { FieldNumber = 3, Name = "Observation Identifier", DataType = "CE", IsRequired = true, MaxLength = 200, Description = "Identifies what was observed" },
                new() { FieldNumber = 5, Name = "Observation Value", DataType = "*", IsRequired = true, MaxLength = 65536, Description = "The actual result value" },
                new() { FieldNumber = 11, Name = "Observation Result Status", DataType = "ID", IsRequired = true, MaxLength = 1, ValidationPattern = @"^[RCPFXIDSO]$", Description = "Status of the result" }
            },
            OptionalFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 4, Name = "Observation Sub-ID", DataType = "ST", IsRequired = false, MaxLength = 20, Description = "Sub-component identifier" },
                new() { FieldNumber = 6, Name = "Units", DataType = "CE", IsRequired = false, MaxLength = 60, Description = "Units of measurement" },
                new() { FieldNumber = 7, Name = "References Range", DataType = "ST", IsRequired = false, MaxLength = 60, Description = "Normal range for values" },
                new() { FieldNumber = 8, Name = "Abnormal Flags", DataType = "ID", IsRequired = false, MaxLength = 5, Description = "Abnormality indicators" },
                new() { FieldNumber = 14, Name = "Date/Time of Observation", DataType = "TS", IsRequired = false, MaxLength = 26, Description = "When observation was made" }
            }
        };
    }

    private static string DetermineValueType(ObservationResult result)
    {
        // Determine value type based on result content
        if (result.ValueType != null)
            return result.ValueType;
            
        if (decimal.TryParse(result.Value, out _))
            return "NM"; // Numeric
            
        if (result.Value?.Contains("^") == true)
            return "CE"; // Coded Element
            
        return "TX"; // Text (default)
    }

    private static string FormatObservationIdentifier(ObservationResult result)
    {
        // Format: Code^Description^Coding System
        var code = result.ObservationCode ?? "UNKNOWN";
        var description = result.ObservationDescription ?? "Unknown Test";
        var codingSystem = result.CodingSystem ?? "LN"; // LOINC
        
        return $"{code}^{description}^{codingSystem}";
    }

    private static string FormatObservationValue(ObservationResult result)
    {
        if (string.IsNullOrEmpty(result.Value))
            return "";
            
        // Escape HL7 special characters in the value
        return EscapeHL7Field(result.Value);
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