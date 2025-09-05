// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text;
using System.Text.RegularExpressions;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;

/// <summary>
/// Input data required to build EVN (Event Type) segments.
/// </summary>
public record EVNInput
{
    public required string EventTypeCode { get; init; }     // e.g., "A01", "A03", "A08"
    public DateTime? RecordedDateTime { get; init; }
    public DateTime? PlannedDateTime { get; init; }
    public string? EventReasonCode { get; init; }
    public string? OperatorId { get; init; }
}

/// <summary>
/// Builds HL7 v2.3 EVN (Event Type) segments with strict standards compliance.
/// EVN contains information about the event that triggered the message.
/// </summary>
public class EVNSegmentBuilder : IHL7SegmentBuilder<EVNInput>
{
    private const string FieldSeparator = "|";
    
    public string Build(EVNInput input, int setId, GenerationOptions options)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var sb = new StringBuilder("EVN");
        sb.Append(FieldSeparator);
        
        // EVN-1: Event Type Code (REQUIRED)
        sb.Append(input.EventTypeCode).Append(FieldSeparator);
        
        // EVN-2: Recorded Date/Time (REQUIRED)
        var recordedDateTime = input.RecordedDateTime ?? DateTime.Now;
        sb.Append(FormatHL7Timestamp(recordedDateTime)).Append(FieldSeparator);
        
        // EVN-3: Date/Time Planned Event (optional)
        if (input.PlannedDateTime.HasValue)
            sb.Append(FormatHL7Timestamp(input.PlannedDateTime.Value));
        sb.Append(FieldSeparator);
        
        // EVN-4: Event Reason Code (optional)
        if (!string.IsNullOrWhiteSpace(input.EventReasonCode))
            sb.Append(input.EventReasonCode);
        sb.Append(FieldSeparator);
        
        // EVN-5: Operator ID (optional)
        if (!string.IsNullOrWhiteSpace(input.OperatorId))
            sb.Append(input.OperatorId);
        
        return sb.ToString();
    }

    public SegmentValidationResult Validate(string segment)
    {
        var result = new SegmentValidationResult
        {
            SegmentType = "EVN",
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
        if (!segment.StartsWith("EVN"))
        {
            result = result with { IsValid = false };
            result.Errors.Add("Segment must start with 'EVN'");
        }

        if (fields.Length < 3)
        {
            result = result with { IsValid = false };
            result.Errors.Add("EVN segment must have minimum 3 fields");
        }

        // EVN-1: Event Type Code validation (required)
        if (fields.Length > 1 && string.IsNullOrWhiteSpace(fields[1]))
        {
            result = result with { IsValid = false };
            result.Errors.Add("EVN-1 Event Type Code is required");
        }

        // EVN-2: Recorded Date/Time validation (required)
        if (fields.Length > 2)
        {
            if (string.IsNullOrWhiteSpace(fields[2]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("EVN-2 Recorded Date/Time is required");
            }
            else if (!Regex.IsMatch(fields[2], @"^\d{8,14}$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("EVN-2 must be valid timestamp format YYYYMMDDHHMMSS");
            }
        }

        // EVN-3: Planned Date/Time validation (optional)
        if (fields.Length > 3 && !string.IsNullOrWhiteSpace(fields[3]))
        {
            if (!Regex.IsMatch(fields[3], @"^\d{8,14}$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("EVN-3 must be valid timestamp format YYYYMMDDHHMMSS");
            }
        }

        return result;
    }

    public SegmentMetadata GetMetadata()
    {
        return new SegmentMetadata
        {
            SegmentType = "EVN",
            Description = "Event Type - Contains information about the event that triggered the message",
            MinimumFields = 3,
            RequiredFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 1, Name = "Event Type Code", DataType = "ID", IsRequired = true, MaxLength = 3, Description = "Code identifying the trigger event" },
                new() { FieldNumber = 2, Name = "Recorded Date/Time", DataType = "TS", IsRequired = true, MaxLength = 26, ValidationPattern = @"^\d{8,14}$", Description = "Date/time event was recorded" }
            },
            OptionalFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 3, Name = "Date/Time Planned Event", DataType = "TS", IsRequired = false, MaxLength = 26, ValidationPattern = @"^\d{8,14}$", Description = "Planned date/time for event" },
                new() { FieldNumber = 4, Name = "Event Reason Code", DataType = "IS", IsRequired = false, MaxLength = 3, Description = "Reason for the event" },
                new() { FieldNumber = 5, Name = "Operator ID", DataType = "XCN", IsRequired = false, MaxLength = 250, Description = "Person who entered the event" }
            }
        };
    }

    private static string FormatHL7Timestamp(DateTime dateTime)
    {
        // HL7 v2.3 timestamp format: YYYYMMDDHHMMSS
        return dateTime.ToString("yyyyMMddHHmmss");
    }
}