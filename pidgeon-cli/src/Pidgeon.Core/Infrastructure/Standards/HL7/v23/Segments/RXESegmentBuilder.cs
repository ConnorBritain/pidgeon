// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text;
using System.Text.RegularExpressions;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;

/// <summary>
/// Builds HL7 v2.3 RXE (Pharmacy/Treatment Encoded Order) segments with strict standards compliance.
/// Handles pharmacy order information including medication details and dosing instructions.
/// </summary>
public class RXESegmentBuilder : IHL7SegmentBuilder<Prescription>
{
    private const string FieldSeparator = "|";
    
    public string Build(Prescription input, int setId, GenerationOptions options)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var sb = new StringBuilder("RXE");
        sb.Append(FieldSeparator);
        
        // RXE-1: Quantity/Timing (optional)
        sb.Append(FormatQuantityTiming(input)).Append(FieldSeparator);
        
        // RXE-2: Give Code (REQUIRED) - Medication identifier
        sb.Append(FormatGiveCode(input)).Append(FieldSeparator);
        
        // RXE-3: Give Amount - Minimum (REQUIRED)
        sb.Append(FormatGiveAmount(input)).Append(FieldSeparator);
        
        // RXE-4: Give Amount - Maximum (optional)
        sb.Append(FieldSeparator);
        
        // RXE-5: Give Units (REQUIRED)
        sb.Append(FormatGiveUnits(input)).Append(FieldSeparator);
        
        // RXE-6: Give Dosage Form (optional)
        if (input.Medication?.DosageForm != null)
            sb.Append(EscapeHL7Field(input.Medication.DosageForm.ToString()));
        sb.Append(FieldSeparator);
        
        // RXE-7: Provider's Administration Instructions (optional)
        if (input.Dosage?.Instructions != null)
            sb.Append(EscapeHL7Field(input.Dosage.Instructions));
        sb.Append(FieldSeparator);
        
        // RXE-8: Deliver-To Location (optional)
        sb.Append(FieldSeparator);
        
        // RXE-9: Substitution Status (optional)
        sb.Append(FieldSeparator);
        
        // RXE-10: Dispense Amount (optional)
        if (input.Dosage?.Quantity.HasValue == true)
            sb.Append(input.Dosage.Quantity.Value.ToString());
        sb.Append(FieldSeparator);
        
        // RXE-11: Dispense Units (optional)
        if (input.Dosage?.DoseUnit != null)
            sb.Append(EscapeHL7Field(input.Dosage.DoseUnit));
        sb.Append(FieldSeparator);
        
        // RXE-12: Number of Refills (optional)
        if (input.Dosage?.Refills.HasValue == true)
            sb.Append(input.Dosage.Refills.Value.ToString());
        sb.Append(FieldSeparator);
        
        // RXE-13: Ordering Provider's DEA Number (optional)
        if (input.Prescriber?.DeaNumber != null)
            sb.Append(EscapeHL7Field(input.Prescriber.DeaNumber));
        sb.Append(FieldSeparator);
        
        // RXE-14: Pharmacist/Treatment Supplier's Verifier ID (optional)
        sb.Append(FieldSeparator);
        
        // RXE-15: Prescription Number (optional)
        if (!string.IsNullOrEmpty(input.Id))
            sb.Append(EscapeHL7Field(input.Id));
        
        return sb.ToString();
    }

    public SegmentValidationResult Validate(string segment)
    {
        var result = new SegmentValidationResult
        {
            SegmentType = "RXE",
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
        if (!segment.StartsWith("RXE"))
        {
            result = result with { IsValid = false };
            result.Errors.Add("Segment must start with 'RXE'");
        }

        if (fields.Length < 6)
        {
            result = result with { IsValid = false };
            result.Errors.Add("RXE segment must have minimum 6 fields");
        }

        // RXE-2: Give Code validation (required)
        if (fields.Length > 2)
        {
            if (string.IsNullOrWhiteSpace(fields[2]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("RXE-2 (Give Code) is required");
            }
            else if (!fields[2].Contains("^"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("RXE-2 (Give Code) must contain component separators");
            }
        }

        // RXE-3: Give Amount validation (required)
        if (fields.Length > 3)
        {
            if (string.IsNullOrWhiteSpace(fields[3]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("RXE-3 (Give Amount) is required");
            }
            else if (!decimal.TryParse(fields[3], out _))
            {
                result = result with { IsValid = false };
                result.Errors.Add("RXE-3 (Give Amount) must be a valid numeric value");
            }
        }

        // RXE-5: Give Units validation (required)
        if (fields.Length > 5)
        {
            if (string.IsNullOrWhiteSpace(fields[5]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("RXE-5 (Give Units) is required");
            }
        }

        // RXE-10: Dispense Amount validation (if present)
        if (fields.Length > 10 && !string.IsNullOrWhiteSpace(fields[10]))
        {
            if (!decimal.TryParse(fields[10], out _))
            {
                result = result with { IsValid = false };
                result.Errors.Add("RXE-10 (Dispense Amount) must be a valid numeric value");
            }
        }

        // RXE-12: Number of Refills validation (if present)
        if (fields.Length > 12 && !string.IsNullOrWhiteSpace(fields[12]))
        {
            if (!int.TryParse(fields[12], out var refills) || refills < 0)
            {
                result = result with { IsValid = false };
                result.Errors.Add("RXE-12 (Number of Refills) must be a non-negative integer");
            }
        }

        return result;
    }

    public SegmentMetadata GetMetadata()
    {
        return new SegmentMetadata
        {
            SegmentType = "RXE",
            Description = "Pharmacy/Treatment Encoded Order - Contains detailed medication order information",
            MinimumFields = 6,
            RequiredFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 2, Name = "Give Code", DataType = "CE", IsRequired = true, MaxLength = 100, Description = "Medication identifier (NDC, drug name)" },
                new() { FieldNumber = 3, Name = "Give Amount - Minimum", DataType = "NM", IsRequired = true, MaxLength = 20, Description = "Dose amount per administration" },
                new() { FieldNumber = 5, Name = "Give Units", DataType = "CE", IsRequired = true, MaxLength = 60, Description = "Units of the give amount" }
            },
            OptionalFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 1, Name = "Quantity/Timing", DataType = "TQ", IsRequired = false, MaxLength = 200, Description = "Dosing schedule and timing" },
                new() { FieldNumber = 4, Name = "Give Amount - Maximum", DataType = "NM", IsRequired = false, MaxLength = 20, Description = "Maximum dose amount" },
                new() { FieldNumber = 6, Name = "Give Dosage Form", DataType = "CE", IsRequired = false, MaxLength = 60, Description = "Form of medication (tablet, liquid, etc.)" },
                new() { FieldNumber = 7, Name = "Provider's Administration Instructions", DataType = "CE", IsRequired = false, MaxLength = 200, Description = "How to administer the medication" },
                new() { FieldNumber = 10, Name = "Dispense Amount", DataType = "NM", IsRequired = false, MaxLength = 20, Description = "Total amount to dispense" },
                new() { FieldNumber = 11, Name = "Dispense Units", DataType = "CE", IsRequired = false, MaxLength = 60, Description = "Units for dispensed amount" },
                new() { FieldNumber = 12, Name = "Number of Refills", DataType = "NM", IsRequired = false, MaxLength = 3, Description = "Number of refills authorized" },
                new() { FieldNumber = 13, Name = "Ordering Provider's DEA Number", DataType = "ST", IsRequired = false, MaxLength = 30, Description = "DEA number of prescriber" },
                new() { FieldNumber = 15, Name = "Prescription Number", DataType = "ST", IsRequired = false, MaxLength = 20, Description = "Pharmacy prescription number" }
            }
        };
    }

    private static string FormatQuantityTiming(Prescription prescription)
    {
        // Basic frequency mapping - in real implementation this would be more sophisticated
        if (prescription.Dosage?.Frequency != null)
        {
            return prescription.Dosage.Frequency switch
            {
                "QD" or "DAILY" => "1^^D",
                "BID" => "2^^D", 
                "TID" => "3^^D",
                "QID" => "4^^D",
                _ => prescription.Dosage.Frequency
            };
        }
        return "";
    }

    private static string FormatGiveCode(Prescription prescription)
    {
        // Format: Drug Code^Drug Name^Coding System
        var drugCode = prescription.Medication?.Id ?? "UNK";
        var drugName = prescription.Medication?.Name ?? "Unknown Medication";
        var codingSystem = "NDC"; // National Drug Code
        
        return $"{drugCode}^{drugName}^{codingSystem}";
    }

    private static string FormatGiveAmount(Prescription prescription)
    {
        return prescription.Dosage?.Dose ?? "1";
    }

    private static string FormatGiveUnits(Prescription prescription)
    {
        return prescription.Dosage?.DoseUnit ?? "TAB";
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
}