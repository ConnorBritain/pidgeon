// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text;
using System.Text.RegularExpressions;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;

/// <summary>
/// Builds HL7 v2.3 ORC (Common Order) segments with strict standards compliance.
/// Handles order control information including order status and ordering provider.
/// </summary>
public class ORCSegmentBuilder : IHL7SegmentBuilder<OrderControl>
{
    private const string FieldSeparator = "|";
    
    public string Build(OrderControl input, int setId, GenerationOptions options)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var sb = new StringBuilder("ORC");
        sb.Append(FieldSeparator);
        
        // ORC-1: Order Control (REQUIRED)
        sb.Append(input.OrderControlCode ?? "NW").Append(FieldSeparator); // Default to "NW" (New order)
        
        // ORC-2: Placer Order Number (optional)
        if (!string.IsNullOrEmpty(input.PlacerOrderNumber))
            sb.Append(EscapeHL7Field(input.PlacerOrderNumber));
        sb.Append(FieldSeparator);
        
        // ORC-3: Filler Order Number (optional)
        if (!string.IsNullOrEmpty(input.FillerOrderNumber))
            sb.Append(EscapeHL7Field(input.FillerOrderNumber));
        sb.Append(FieldSeparator);
        
        // ORC-4: Placer Group Number (optional)
        sb.Append(FieldSeparator);
        
        // ORC-5: Order Status (optional but commonly used)
        sb.Append(input.OrderStatus ?? "A").Append(FieldSeparator); // Default to "A" (Active)
        
        // ORC-6: Response Flag (optional)
        sb.Append(FieldSeparator);
        
        // ORC-7: Quantity/Timing (optional)
        sb.Append(FieldSeparator);
        
        // ORC-8: Parent (optional)
        sb.Append(FieldSeparator);
        
        // ORC-9: Date/Time of Transaction (optional but commonly used)
        if (input.TransactionDateTime.HasValue)
            sb.Append(FormatHL7Timestamp(input.TransactionDateTime.Value));
        sb.Append(FieldSeparator);
        
        // ORC-10: Entered By (optional)
        sb.Append(FieldSeparator);
        
        // ORC-11: Verified By (optional)
        sb.Append(FieldSeparator);
        
        // ORC-12: Ordering Provider (optional but commonly used)
        if (input.OrderingProvider != null)
        {
            sb.Append(input.OrderingProvider.Id ?? "")
              .Append("^")
              .Append(EscapeHL7Field(input.OrderingProvider.Name?.Family ?? ""))
              .Append("^")
              .Append(EscapeHL7Field(input.OrderingProvider.Name?.Given ?? ""));
        }
        sb.Append(FieldSeparator);
        
        // ORC-13: Enterer's Location (optional)
        sb.Append(FieldSeparator);
        
        // ORC-14: Call Back Phone Number (optional)
        sb.Append(FieldSeparator);
        
        // ORC-15: Order Effective Date/Time (optional)
        if (input.OrderEffectiveDateTime.HasValue)
            sb.Append(FormatHL7Timestamp(input.OrderEffectiveDateTime.Value));
        
        return sb.ToString();
    }

    public SegmentValidationResult Validate(string segment)
    {
        var result = new SegmentValidationResult
        {
            SegmentType = "ORC",
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
        if (!segment.StartsWith("ORC"))
        {
            result = result with { IsValid = false };
            result.Errors.Add("Segment must start with 'ORC'");
        }

        if (fields.Length < 2)
        {
            result = result with { IsValid = false };
            result.Errors.Add("ORC segment must have minimum 2 fields");
        }

        // ORC-1: Order Control validation (required)
        if (fields.Length > 1)
        {
            if (string.IsNullOrWhiteSpace(fields[1]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("ORC-1 (Order Control) is required");
            }
            else if (!Regex.IsMatch(fields[1], @"^(NW|OK|UA|CA|OC|CR|UC|DC|CN|RE|RU|AF|DF|FU|DE|XO|XR|XX|SC|SN|SS)$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("ORC-1 (Order Control) must be a valid order control code");
            }
        }

        // ORC-5: Order Status validation (if present)
        if (fields.Length > 5 && !string.IsNullOrWhiteSpace(fields[5]))
        {
            if (!Regex.IsMatch(fields[5], @"^[AEIPSCRXDTHF]$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("ORC-5 (Order Status) must be a valid status code");
            }
        }

        // ORC-9: Date/Time of Transaction validation (if present)
        if (fields.Length > 9 && !string.IsNullOrWhiteSpace(fields[9]))
        {
            if (!Regex.IsMatch(fields[9], @"^\d{8,14}$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("ORC-9 (Date/Time of Transaction) must be valid timestamp format");
            }
        }

        // ORC-15: Order Effective Date/Time validation (if present)
        if (fields.Length > 15 && !string.IsNullOrWhiteSpace(fields[15]))
        {
            if (!Regex.IsMatch(fields[15], @"^\d{8,14}$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("ORC-15 (Order Effective Date/Time) must be valid timestamp format");
            }
        }

        return result;
    }

    public SegmentMetadata GetMetadata()
    {
        return new SegmentMetadata
        {
            SegmentType = "ORC",
            Description = "Common Order - Contains order control information common to all order types",
            MinimumFields = 2,
            RequiredFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 1, Name = "Order Control", DataType = "ID", IsRequired = true, MaxLength = 2, ValidationPattern = @"^(NW|OK|UA|CA|OC|CR|UC|DC|CN|RE|RU|AF|DF|FU|DE|XO|XR|XX|SC|SN|SS)$", Description = "Action requested/taken on order" }
            },
            OptionalFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 2, Name = "Placer Order Number", DataType = "EI", IsRequired = false, MaxLength = 75, Description = "Unique order number from ordering system" },
                new() { FieldNumber = 3, Name = "Filler Order Number", DataType = "EI", IsRequired = false, MaxLength = 75, Description = "Unique order number from fulfilling system" },
                new() { FieldNumber = 5, Name = "Order Status", DataType = "ID", IsRequired = false, MaxLength = 2, ValidationPattern = @"^[AEIPSCRXDTHF]$", Description = "Status of the order" },
                new() { FieldNumber = 9, Name = "Date/Time of Transaction", DataType = "TS", IsRequired = false, MaxLength = 26, ValidationPattern = @"^\d{8,14}$", Description = "When order transaction occurred" },
                new() { FieldNumber = 12, Name = "Ordering Provider", DataType = "XCN", IsRequired = false, MaxLength = 250, Description = "Provider who entered the order" },
                new() { FieldNumber = 15, Name = "Order Effective Date/Time", DataType = "TS", IsRequired = false, MaxLength = 26, ValidationPattern = @"^\d{8,14}$", Description = "When order becomes effective" }
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

    private static string FormatHL7Timestamp(DateTime dateTime)
    {
        // HL7 v2.3 timestamp format: YYYYMMDDHHMMSS
        return dateTime.ToString("yyyyMMddHHmmss");
    }
}

/// <summary>
/// Input model for ORC segment builder representing order control information.
/// </summary>
public record OrderControl
{
    public string? OrderControlCode { get; init; } = "NW"; // New order
    public string? PlacerOrderNumber { get; init; }
    public string? FillerOrderNumber { get; init; }
    public string? OrderStatus { get; init; } = "A"; // Active
    public DateTime? TransactionDateTime { get; init; }
    public Provider? OrderingProvider { get; init; }
    public DateTime? OrderEffectiveDateTime { get; init; }
}