// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text;
using System.Text.RegularExpressions;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;

/// <summary>
/// Builds HL7 v2.3 PID (Patient Identification) segments with strict standards compliance.
/// Handles all required and optional PID fields according to HL7.org specification.
/// </summary>
public class PIDSegmentBuilder : IHL7SegmentBuilder<Patient>
{
    private const string FieldSeparator = "|";
    private const string ComponentSeparator = "^";
    
    public string Build(Patient input, int setId, GenerationOptions options)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var sb = new StringBuilder("PID");
        sb.Append(FieldSeparator);
        
        // PID-1: Set ID (REQUIRED)
        sb.Append(setId).Append(FieldSeparator);
        
        // PID-2: Patient ID (external) - deprecated in v2.3, leave empty
        sb.Append(FieldSeparator);
        
        // PID-3: Patient identifier list (REQUIRED)
        var patientId = input.MedicalRecordNumber ?? input.Id ?? "UNKNOWN";
        sb.Append(patientId)
          .Append("^^^PIDGEON^MR")  // Format: ID^^^AssigningAuthority^IDType
          .Append(FieldSeparator);
        
        // PID-4: Alternate patient ID (optional)
        sb.Append(FieldSeparator);
        
        // PID-5: Patient name (REQUIRED) - format: Family^Given^Middle^Suffix^Prefix
        sb.Append(EscapeHL7Field(input.Name.Family ?? "UNKNOWN"))
          .Append(ComponentSeparator).Append(EscapeHL7Field(input.Name.Given ?? "UNKNOWN"));
        
        if (!string.IsNullOrWhiteSpace(input.Name.Middle))
            sb.Append(ComponentSeparator).Append(EscapeHL7Field(input.Name.Middle));
        
        if (!string.IsNullOrWhiteSpace(input.Name.Suffix))
            sb.Append("^^").Append(EscapeHL7Field(input.Name.Suffix));
        
        sb.Append(FieldSeparator);
        
        // PID-6: Mother's maiden name (optional)
        sb.Append(FieldSeparator);
        
        // PID-7: Date of birth (optional but commonly used)
        if (input.BirthDate.HasValue)
            sb.Append(input.BirthDate.Value.ToString("yyyyMMdd"));
        sb.Append(FieldSeparator);
        
        // PID-8: Administrative sex (optional but commonly used)
        sb.Append(MapGenderToHL7(input.Gender));
        
        return sb.ToString();
    }

    public SegmentValidationResult Validate(string segment)
    {
        var result = new SegmentValidationResult
        {
            SegmentType = "PID",
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
        if (!segment.StartsWith("PID"))
        {
            result = result with { IsValid = false };
            result.Errors.Add("Segment must start with 'PID'");
        }

        if (fields.Length < 6)
        {
            result = result with { IsValid = false };
            result.Errors.Add("PID segment must have minimum 6 fields");
        }

        // PID-1: Set ID validation
        if (fields.Length > 1)
        {
            if (!int.TryParse(fields[1], out var setId) || setId <= 0)
            {
                result = result with { IsValid = false };
                result.Errors.Add("PID-1 (Set ID) must be a positive integer");
            }
        }

        // PID-3: Patient identifier validation
        if (fields.Length > 3)
        {
            if (string.IsNullOrWhiteSpace(fields[3]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("PID-3 (Patient Identifier) is required");
            }
        }

        // PID-5: Patient name validation
        if (fields.Length > 5)
        {
            if (!fields[5].Contains(ComponentSeparator))
            {
                result = result with { IsValid = false };
                result.Errors.Add("PID-5 (Patient Name) must contain at least Family^Given");
            }
            else
            {
                var nameComponents = fields[5].Split('^');
                if (nameComponents.Length < 2)
                {
                    result = result with { IsValid = false };
                    result.Errors.Add("PID-5 must have at least Family^Given components");
                }
                if (string.IsNullOrWhiteSpace(nameComponents[0]))
                {
                    result = result with { IsValid = false };
                    result.Errors.Add("Family name (PID-5.1) is required");
                }
                if (string.IsNullOrWhiteSpace(nameComponents[1]))
                {
                    result = result with { IsValid = false };
                    result.Errors.Add("Given name (PID-5.2) is required");
                }
            }
        }

        // PID-7: Date of birth format validation
        if (fields.Length > 7 && !string.IsNullOrWhiteSpace(fields[7]))
        {
            if (!Regex.IsMatch(fields[7], @"^\d{8}$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("PID-7 (Date of Birth) must be in YYYYMMDD format");
            }
        }

        // PID-8: Administrative sex validation
        if (fields.Length > 8 && !string.IsNullOrWhiteSpace(fields[8]))
        {
            if (!Regex.IsMatch(fields[8], @"^[MFOU]$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("PID-8 (Administrative Sex) must be M, F, O, or U");
            }
        }

        return result;
    }

    public SegmentMetadata GetMetadata()
    {
        return new SegmentMetadata
        {
            SegmentType = "PID",
            Description = "Patient Identification - Contains patient demographic information",
            MinimumFields = 6,
            RequiredFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 1, Name = "Set ID", DataType = "SI", IsRequired = true, MaxLength = 4, Description = "Sequential number for multiple PID segments" },
                new() { FieldNumber = 3, Name = "Patient Identifier List", DataType = "CX", IsRequired = true, MaxLength = 250, Description = "Unique patient identifier" },
                new() { FieldNumber = 5, Name = "Patient Name", DataType = "XPN", IsRequired = true, MaxLength = 250, Description = "Patient's full name in Family^Given format" }
            },
            OptionalFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 2, Name = "Patient ID (External)", DataType = "CX", IsRequired = false, MaxLength = 20, Description = "Deprecated in v2.3" },
                new() { FieldNumber = 4, Name = "Alternate Patient ID", DataType = "CX", IsRequired = false, MaxLength = 20, Description = "Alternative patient identifier" },
                new() { FieldNumber = 6, Name = "Mother's Maiden Name", DataType = "XPN", IsRequired = false, MaxLength = 250, Description = "Mother's maiden name" },
                new() { FieldNumber = 7, Name = "Date/Time of Birth", DataType = "TS", IsRequired = false, MaxLength = 26, ValidationPattern = @"^\d{8}$", Description = "YYYYMMDD format" },
                new() { FieldNumber = 8, Name = "Administrative Sex", DataType = "IS", IsRequired = false, MaxLength = 1, ValidationPattern = @"^[MFOU]$", Description = "M=Male, F=Female, O=Other, U=Unknown" }
            }
        };
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

    private static string MapGenderToHL7(Gender? gender)
    {
        return gender switch
        {
            Gender.Male => "M",
            Gender.Female => "F",
            Gender.Other => "O",
            _ => "U" // Unknown
        };
    }
}