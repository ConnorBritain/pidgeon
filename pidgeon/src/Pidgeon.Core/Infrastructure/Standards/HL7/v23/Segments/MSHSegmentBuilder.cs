// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text;
using System.Text.RegularExpressions;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;

/// <summary>
/// Input data required to build MSH (Message Header) segments.
/// </summary>
public record MSHInput
{
    public required string MessageType { get; init; }     // e.g., "ADT^A01^ADT_A01"
    public required string TriggerEvent { get; init; }    // e.g., "A01"
    public string SendingApplication { get; init; } = "PIDGEON";
    public string SendingFacility { get; init; } = "FACILITY";
    public string ReceivingApplication { get; init; } = "RECEIVER";
    public string ReceivingFacility { get; init; } = "FACILITY";
    public DateTime? MessageDateTime { get; init; }
}

/// <summary>
/// Builds HL7 v2.3 MSH (Message Header) segments with strict standards compliance.
/// MSH is the first segment in every HL7 message and contains routing and control information.
/// </summary>
public class MSHSegmentBuilder : IHL7SegmentBuilder<MSHInput>
{
    private const string FieldSeparator = "|";
    private const string EncodingCharacters = "^~\\&";
    
    public string Build(MSHInput input, int setId, GenerationOptions options)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var sb = new StringBuilder("MSH");
        
        // MSH-1: Field separator (always |)
        sb.Append(FieldSeparator);
        
        // MSH-2: Encoding characters (^~\&)
        sb.Append(EncodingCharacters).Append(FieldSeparator);
        
        // MSH-3: Sending application (REQUIRED)
        sb.Append(input.SendingApplication).Append(FieldSeparator);
        
        // MSH-4: Sending facility (REQUIRED)
        sb.Append(input.SendingFacility).Append(FieldSeparator);
        
        // MSH-5: Receiving application (optional)
        sb.Append(input.ReceivingApplication).Append(FieldSeparator);
        
        // MSH-6: Receiving facility (optional)
        sb.Append(input.ReceivingFacility).Append(FieldSeparator);
        
        // MSH-7: Date/Time of message (REQUIRED)
        var timestamp = input.MessageDateTime ?? DateTime.Now;
        sb.Append(FormatHL7Timestamp(timestamp)).Append(FieldSeparator);
        
        // MSH-8: Security (optional)
        sb.Append(FieldSeparator);
        
        // MSH-9: Message Type (REQUIRED) - format: MSG^EVENT^MSG_CTRL_ID
        sb.Append(input.MessageType).Append(FieldSeparator);
        
        // MSH-10: Message Control ID (REQUIRED)
        sb.Append(GenerateMessageControlId(options)).Append(FieldSeparator);
        
        // MSH-11: Processing ID (REQUIRED) - P=Production, T=Test, D=Debug
        sb.Append("P").Append(FieldSeparator);
        
        // MSH-12: Version ID (REQUIRED) - should be 2.3 for HL7 v2.3
        sb.Append("2.3");
        
        return sb.ToString();
    }

    public SegmentValidationResult Validate(string segment)
    {
        var result = new SegmentValidationResult
        {
            SegmentType = "MSH",
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
        if (!segment.StartsWith("MSH"))
        {
            result = result with { IsValid = false };
            result.Errors.Add("Segment must start with 'MSH'");
        }

        if (fields.Length < 12)
        {
            result = result with { IsValid = false };
            result.Errors.Add("MSH segment must have minimum 12 fields per HL7 v2.3");
        }

        // MSH-1: Field separator validation (always |)
        if (!segment.StartsWith("MSH|"))
        {
            result = result with { IsValid = false };
            result.Errors.Add("MSH-1 field separator must be '|'");
        }

        // MSH-2: Encoding characters validation (must be ^~\&)
        if (fields.Length > 1 && fields[1] != EncodingCharacters)
        {
            result = result with { IsValid = false };
            result.Errors.Add("MSH-2 encoding characters must be '^~\\&'");
        }

        // MSH-3: Sending application validation (required, non-empty)
        if (fields.Length > 2 && string.IsNullOrWhiteSpace(fields[2]))
        {
            result = result with { IsValid = false };
            result.Errors.Add("MSH-3 Sending Application is required");
        }

        // MSH-7: Date/Time validation (required, format: YYYYMMDDHHMMSS)
        if (fields.Length > 6)
        {
            if (string.IsNullOrWhiteSpace(fields[6]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("MSH-7 Date/Time of message is required");
            }
            else if (!Regex.IsMatch(fields[6], @"^\d{8,14}$"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("MSH-7 must be valid timestamp format YYYYMMDDHHMMSS");
            }
        }

        // MSH-9: Message Type validation (required, format: MSG^EVENT)
        if (fields.Length > 8)
        {
            if (string.IsNullOrWhiteSpace(fields[8]))
            {
                result = result with { IsValid = false };
                result.Errors.Add("MSH-9 Message Type is required");
            }
            else if (!fields[8].Contains("^"))
            {
                result = result with { IsValid = false };
                result.Errors.Add("MSH-9 must have format MSG^EVENT");
            }
            else
            {
                var messageType = fields[8].Split('^');
                if (messageType.Length < 2)
                {
                    result = result with { IsValid = false };
                    result.Errors.Add("MSH-9 must have at least MSG^EVENT components");
                }
            }
        }

        // MSH-10: Message Control ID validation (required, unique identifier)
        if (fields.Length > 9 && string.IsNullOrWhiteSpace(fields[9]))
        {
            result = result with { IsValid = false };
            result.Errors.Add("MSH-10 Message Control ID is required");
        }

        // MSH-11: Processing ID validation (required, usually P for production, T for test)
        if (fields.Length > 10 && !Regex.IsMatch(fields[10], @"^[PTD]$"))
        {
            result = result with { IsValid = false };
            result.Errors.Add("MSH-11 Processing ID must be P, T, or D");
        }

        // MSH-12: Version ID validation (required, should be 2.3 for HL7 v2.3)
        if (fields.Length > 11 && fields[11] != "2.3")
        {
            result.Warnings.Add("MSH-12 Version ID should be '2.3' for HL7 v2.3 compliance");
        }

        return result;
    }

    public SegmentMetadata GetMetadata()
    {
        return new SegmentMetadata
        {
            SegmentType = "MSH",
            Description = "Message Header - Contains message routing and control information",
            MinimumFields = 12,
            RequiredFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 1, Name = "Field Separator", DataType = "ST", IsRequired = true, MaxLength = 1, Description = "Always '|'" },
                new() { FieldNumber = 2, Name = "Encoding Characters", DataType = "ST", IsRequired = true, MaxLength = 4, Description = "Always '^~\\&'" },
                new() { FieldNumber = 3, Name = "Sending Application", DataType = "HD", IsRequired = true, MaxLength = 227, Description = "Application sending the message" },
                new() { FieldNumber = 4, Name = "Sending Facility", DataType = "HD", IsRequired = true, MaxLength = 227, Description = "Facility sending the message" },
                new() { FieldNumber = 7, Name = "Date/Time of Message", DataType = "TS", IsRequired = true, MaxLength = 26, ValidationPattern = @"^\d{8,14}$", Description = "YYYYMMDDHHMMSS format" },
                new() { FieldNumber = 9, Name = "Message Type", DataType = "MSG", IsRequired = true, MaxLength = 15, Description = "MSG^EVENT^MSG_CTRL_ID format" },
                new() { FieldNumber = 10, Name = "Message Control ID", DataType = "ST", IsRequired = true, MaxLength = 20, Description = "Unique message identifier" },
                new() { FieldNumber = 11, Name = "Processing ID", DataType = "PT", IsRequired = true, MaxLength = 3, ValidationPattern = @"^[PTD]$", Description = "P=Production, T=Test, D=Debug" },
                new() { FieldNumber = 12, Name = "Version ID", DataType = "VID", IsRequired = true, MaxLength = 60, Description = "HL7 version (2.3)" }
            },
            OptionalFields = new List<FieldMetadata>
            {
                new() { FieldNumber = 5, Name = "Receiving Application", DataType = "HD", IsRequired = false, MaxLength = 227, Description = "Application receiving the message" },
                new() { FieldNumber = 6, Name = "Receiving Facility", DataType = "HD", IsRequired = false, MaxLength = 227, Description = "Facility receiving the message" },
                new() { FieldNumber = 8, Name = "Security", DataType = "ST", IsRequired = false, MaxLength = 40, Description = "Security information" }
            }
        };
    }

    private static string FormatHL7Timestamp(DateTime dateTime)
    {
        // HL7 v2.3 timestamp format: YYYYMMDDHHMMSS
        return dateTime.ToString("yyyyMMddHHmmss");
    }

    private static string GenerateMessageControlId(GenerationOptions options)
    {
        // Generate deterministic message control ID based on seed for testing consistency
        var seed = options.Seed ?? Environment.TickCount;
        var random = new Random(seed);
        
        // Generate 10-character hex control ID
        var controlId = random.Next(0x10000000, 0x7FFFFFFF).ToString("X10")[..10];
        return controlId;
    }
}