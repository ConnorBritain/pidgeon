// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text;
using System.Text.RegularExpressions;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;

/// <summary>
/// Builds HL7 v2.3 PV1 (Patient Visit) segments with strict standards compliance.
/// Handles patient visit information including location, class, and attending physician.
/// </summary>
public class PV1SegmentBuilder : IHL7SegmentBuilder<Encounter>
{
    private const string FieldSeparator = "|";
    
    public string Build(Encounter input, int setId, GenerationOptions options)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var sb = new StringBuilder("PV1");
        sb.Append(FieldSeparator);
        
        // PV1-1: Set ID (REQUIRED)
        sb.Append(setId).Append(FieldSeparator);
        
        // PV1-2: Patient Class (REQUIRED) - E=Emergency, I=Inpatient, O=Outpatient, etc.
        sb.Append(MapEncounterTypeToPatientClass(input.Type)).Append(FieldSeparator);
        
        // PV1-3: Assigned Patient Location (optional but commonly used)
        // Format: Building^Room^Bed^Facility^Location Status^Person Location Type
        sb.Append(FormatPatientLocation(input.Location)).Append(FieldSeparator);
        
        // PV1-4: Admission Type (optional)
        sb.Append(FieldSeparator);
        
        // PV1-5: Preadmit Number (optional)
        sb.Append(FieldSeparator);
        
        // PV1-6: Prior Patient Location (optional)
        sb.Append(FieldSeparator);
        
        // PV1-7: Attending Doctor (optional but commonly used)
        if (input.Provider != null)
        {
            sb.Append(input.Provider.Id ?? "")
              .Append("^")
              .Append(EscapeHL7Field(input.Provider.Name?.Family ?? ""))
              .Append("^")
              .Append(EscapeHL7Field(input.Provider.Name?.Given ?? ""));
        }
        sb.Append(FieldSeparator);
        
        // PV1-8: Referring Doctor (optional)
        sb.Append(FieldSeparator);
        
        // PV1-9: Consulting Doctor (optional)
        sb.Append(FieldSeparator);
        
        // PV1-10: Hospital Service (optional)
        sb.Append(FieldSeparator);
        
        // PV1-11: Temporary Location (optional)
        sb.Append(FieldSeparator);
        
        // PV1-12: Preadmit Test Indicator (optional)
        sb.Append(FieldSeparator);
        
        // PV1-13: Re-admission Indicator (optional)
        sb.Append(FieldSeparator);
        
        // PV1-14: Admit Source (optional)
        sb.Append(FieldSeparator);
        
        // PV1-15: Ambulatory Status (optional)
        sb.Append(FieldSeparator);
        
        // PV1-16: VIP Indicator (optional)
        sb.Append(FieldSeparator);
        
        // PV1-17: Admitting Doctor (optional)
        sb.Append(FieldSeparator);
        
        // PV1-18: Patient Type (optional)
        sb.Append(FieldSeparator);
        
        // PV1-19: Visit Number (optional but commonly used)
        sb.Append(input.Id ?? "").Append(FieldSeparator);
        
        // PV1-20 through PV1-43: Various optional fields (skip to PV1-44)
        for (int i = 20; i <= 43; i++)
            sb.Append(FieldSeparator);
        
        // PV1-44: Admit Date/Time (optional but commonly used)
        if (input.StartTime.HasValue)
            sb.Append(FormatHL7Timestamp(input.StartTime.Value));
        
        return sb.ToString();
    }

    public SegmentValidationResult Validate(string segment)
    {
        var result = new SegmentValidationResult
        {
            SegmentType = "PV1",
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
        if (!segment.StartsWith("PV1"))
        {
            result = result with { IsValid = false };
            result.Errors.Add("Segment must start with 'PV1'");
        }

        if (fields.Length < 3)
        {
            result = result with { IsValid = false };
            result.Errors.Add("PV1 segment must have minimum 3 fields");
        }

        // PV1-1: Set ID validation
        if (fields.Length > 1)
        {
            if (!int.TryParse(fields[1], out var setId) || setId <= 0)
            {
                result = result with { IsValid = false };
                result.Errors.Add("PV1-1 (Set ID) must be a positive integer");
            }
        }

        // PV1-2: Patient Class validation (required)
        if (fields.Length > 2)
        {
            if (!Regex.IsMatch(fields[2], @"^[EIOPRBN]$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("PV1-2 (Patient Class) must be E, I, O, P, R, B, or N");
            }
        }

        // PV1-3: Assigned Patient Location validation (optional but if present, format: Building^Room^Bed)
        if (fields.Length > 3 && !string.IsNullOrWhiteSpace(fields[3]))
        {
            if (!fields[3].Contains("^"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("PV1-3 (Patient Location) must contain '^' separators if present");
            }
        }

        // PV1-44: Admit Date/Time validation (if present, last field)
        if (fields.Length > 44 && !string.IsNullOrWhiteSpace(fields[44]))
        {
            if (!Regex.IsMatch(fields[44], @"^\d{8,14}$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("PV1-44 (Admit Date/Time) must be valid timestamp format");
            }
        }

        return result;
    }

    public SegmentMetadata GetMetadata()
    {
        return new SegmentMetadata
        {
            SegmentType = "PV1",
            Description = "Patient Visit - Contains visit-specific information for inpatient or outpatient encounters",
            MinimumFields = 3,
            RequiredFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 1, Name = "Set ID", DataType = "SI", IsRequired = true, MaxLength = 4, Description = "Sequential number for multiple PV1 segments" },
                new() { FieldNumber = 2, Name = "Patient Class", DataType = "IS", IsRequired = true, MaxLength = 1, ValidationPattern = @"^[EIOPRBN]$", Description = "E=Emergency, I=Inpatient, O=Outpatient, etc." }
            },
            OptionalFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 3, Name = "Assigned Patient Location", DataType = "PL", IsRequired = false, MaxLength = 80, Description = "Building^Room^Bed format" },
                new() { FieldNumber = 7, Name = "Attending Doctor", DataType = "XCN", IsRequired = false, MaxLength = 250, Description = "Attending physician information" },
                new() { FieldNumber = 19, Name = "Visit Number", DataType = "CX", IsRequired = false, MaxLength = 20, Description = "Unique visit identifier" },
                new() { FieldNumber = 44, Name = "Admit Date/Time", DataType = "TS", IsRequired = false, MaxLength = 26, ValidationPattern = @"^\d{8,14}$", Description = "Date/time of admission" }
            }
        };
    }

    private static string MapEncounterTypeToPatientClass(EncounterType? type)
    {
        return type switch
        {
            EncounterType.Emergency => "E",
            EncounterType.Inpatient => "I",
            EncounterType.Outpatient => "O",
            EncounterType.Observation => "O",
            _ => "I" // Default to inpatient
        };
    }

    private static string FormatPatientLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return "WARD^101^A"; // Default HL7 location format

        // If already in HL7 format, use as-is
        if (location.Contains("^"))
            return location;

        // Convert simple format like "Room 101" to HL7 format
        if (location.StartsWith("Room "))
            return $"WARD^{location.Replace("Room ", "")}^A";

        // Default conversion for other formats
        return $"WARD^{location}^A";
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