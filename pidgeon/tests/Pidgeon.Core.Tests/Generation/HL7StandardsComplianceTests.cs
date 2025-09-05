// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Xunit;
using Pidgeon.Core.Application.Services.Generation.Plugins;
using Pidgeon.Core.Generation.Types;
using Pidgeon.Core.Generation;
using System.Text.RegularExpressions;

namespace Pidgeon.Core.Tests.Generation;

/// <summary>
/// Standards compliance tests that validate generated HL7 messages against official HL7.org specifications.
/// These tests define the exact requirements for standards-compliant message generation.
/// </summary>
public class HL7StandardsComplianceTests
{
    private readonly HL7MessageGenerationPlugin _plugin;

    public HL7StandardsComplianceTests()
    {
        _plugin = TestHelpers.CreateHL7Plugin();
    }

    [Fact]
    public async Task ADT_A01_Generated_Message_Must_Comply_With_HL7_v23_Standard()
    {
        // Arrange
        var options = new GenerationOptions { Seed = 12345 };

        // Act  
        var result = await _plugin.GenerateMessagesAsync("ADT^A01", 1, options);

        // Assert
        Assert.True(result.IsSuccess, result.IsFailure ? $"Message generation failed: {result.Error}" : "Message generation succeeded");
        var message = result.Value.First();
        
        // Validate HL7 v2.3 ADT^A01 message structure per HL7.org specification
        ValidateHL7v23_ADT_A01_Structure(message);
        ValidateHL7v23_MSH_Segment(message);
        ValidateHL7v23_EVN_Segment(message);
        ValidateHL7v23_PID_Segment(message);
        ValidateHL7v23_PV1_Segment(message);
    }

    [Fact]
    public async Task ORU_R01_Generated_Message_Must_Comply_With_HL7_v23_Standard()
    {
        // Arrange
        var options = new GenerationOptions { Seed = 54321 };

        // Act
        var result = await _plugin.GenerateMessagesAsync("ORU^R01", 1, options);

        // Assert
        Assert.True(result.IsSuccess, result.IsFailure ? $"Message generation failed: {result.Error}" : "Message generation succeeded");
        var message = result.Value.First();
        
        // Validate HL7 v2.3 ORU^R01 message structure per HL7.org specification
        ValidateHL7v23_ORU_R01_Structure(message);
        ValidateHL7v23_MSH_Segment(message);
        ValidateHL7v23_PID_Segment(message);  
        ValidateHL7v23_OBR_Segment(message);
        ValidateHL7v23_OBX_Segment(message);
    }

    [Fact]
    public async Task RDE_O11_Generated_Message_Must_Comply_With_HL7_v23_Standard()
    {
        // Arrange
        var options = new GenerationOptions { Seed = 98765 };

        // Act
        var result = await _plugin.GenerateMessagesAsync("RDE^O11", 1, options);

        // Assert
        Assert.True(result.IsSuccess, result.IsFailure ? $"Message generation failed: {result.Error}" : "Message generation succeeded");
        var message = result.Value.First();
        
        // Validate HL7 v2.3 RDE^O11 message structure per HL7.org specification
        ValidateHL7v23_RDE_O11_Structure(message);
        ValidateHL7v23_MSH_Segment(message);
        ValidateHL7v23_PID_Segment(message);
        ValidateHL7v23_RXE_Segment(message);
    }

    [Theory]
    [InlineData("ADT^A01")]
    [InlineData("ADT^A08")] 
    [InlineData("ADT^A03")]
    [InlineData("ORU^R01")]
    [InlineData("RDE^O11")]
    public async Task All_Core_Message_Types_Must_Have_Valid_HL7_Line_Endings(string messageType)
    {
        // Arrange
        var options = new GenerationOptions();

        // Act
        var result = await _plugin.GenerateMessagesAsync(messageType, 1, options);

        // Assert
        Assert.True(result.IsSuccess);
        var message = result.Value.First();
        
        // HL7 v2.3 Standard: Segments must be separated by \r\n (CRLF)
        Assert.Contains("\r\n", message);
        Assert.DoesNotContain("\n\r", message); // Wrong order
        Assert.DoesNotContain("\r\r", message); // Double carriage return
        
        // Should not end with line separator
        Assert.False(message.EndsWith("\r\n"), "Message should not end with CRLF");
    }

    [Theory]
    [InlineData("ADT^A01")]
    [InlineData("ORU^R01")] 
    [InlineData("RDE^O11")]
    public async Task All_Messages_Must_Have_Deterministic_Output_With_Same_Seed(string messageType)
    {
        // Arrange
        var options1 = new GenerationOptions { Seed = 42 };
        var options2 = new GenerationOptions { Seed = 42 };

        // Act
        var result1 = await _plugin.GenerateMessagesAsync(messageType, 1, options1);
        var result2 = await _plugin.GenerateMessagesAsync(messageType, 1, options2);

        // Assert
        Assert.True(result1.IsSuccess && result2.IsSuccess);
        Assert.Equal(result1.Value.First(), result2.Value.First());
    }

    [Theory]
    [InlineData("ADT^A01")]
    [InlineData("ORU^R01")]
    [InlineData("RDE^O11")]
    public async Task All_Messages_Must_Have_Different_Output_With_Different_Seeds(string messageType)
    {
        // Arrange  
        var options1 = new GenerationOptions { Seed = 111 };
        var options2 = new GenerationOptions { Seed = 222 };

        // Act
        var result1 = await _plugin.GenerateMessagesAsync(messageType, 1, options1);
        var result2 = await _plugin.GenerateMessagesAsync(messageType, 1, options2);

        // Assert
        Assert.True(result1.IsSuccess && result2.IsSuccess);
        Assert.NotEqual(result1.Value.First(), result2.Value.First());
    }

    // === HL7 v2.3 Specification Validation Methods ===

    private static void ValidateHL7v23_ADT_A01_Structure(string message)
    {
        var segments = message.Split("\r\n");
        
        // HL7 v2.3 ADT^A01 Required Segments per specification:
        // MSH (required), EVN (required), PID (required), PV1 (required)
        Assert.True(segments.Length >= 4, "ADT^A01 must have minimum 4 segments: MSH, EVN, PID, PV1");
        
        Assert.True(segments[0].StartsWith("MSH"), "First segment must be MSH");
        Assert.True(segments[1].StartsWith("EVN"), "Second segment must be EVN");  
        Assert.True(segments[2].StartsWith("PID"), "Third segment must be PID");
        Assert.True(segments[3].StartsWith("PV1"), "Fourth segment must be PV1");
    }

    private static void ValidateHL7v23_ORU_R01_Structure(string message)
    {
        var segments = message.Split("\r\n");
        
        // HL7 v2.3 ORU^R01 Required Segments per specification:
        // MSH (required), PID (required), OBR (required), OBX (required)
        Assert.True(segments.Length >= 4, "ORU^R01 must have minimum 4 segments: MSH, PID, OBR, OBX");
        
        Assert.True(segments[0].StartsWith("MSH"), "First segment must be MSH");
        Assert.Contains(segments, s => s.StartsWith("PID")); // PID required but position flexible
        Assert.Contains(segments, s => s.StartsWith("OBR")); // OBR required  
        Assert.Contains(segments, s => s.StartsWith("OBX")); // OBX required
    }

    private static void ValidateHL7v23_RDE_O11_Structure(string message)
    {
        var segments = message.Split("\r\n");
        
        // HL7 v2.3 RDE^O11 Required Segments per specification:
        // MSH (required), PID (required), RXE (required)  
        Assert.True(segments.Length >= 3, "RDE^O11 must have minimum 3 segments: MSH, PID, RXE");
        
        Assert.True(segments[0].StartsWith("MSH"), "First segment must be MSH");
        Assert.Contains(segments, s => s.StartsWith("PID")); // PID required
        Assert.Contains(segments, s => s.StartsWith("RXE")); // RXE required
    }

    private static void ValidateHL7v23_MSH_Segment(string message)
    {
        var mshSegment = message.Split("\r\n")[0];
        var fields = mshSegment.Split('|');
        
        // HL7 v2.3 MSH Segment Specification (19 fields total, 6 required)
        Assert.True(fields.Length >= 12, "MSH segment must have minimum 12 fields per HL7 v2.3");
        
        // MSH.1 - Field separator (always |)
        Assert.Equal("MSH", fields[0]);
        
        // MSH.2 - Encoding characters (must be ^~\&)  
        Assert.Equal("^~\\&", fields[1]);
        
        // MSH.3 - Sending application (required, non-empty)
        Assert.False(string.IsNullOrWhiteSpace(fields[2]), "MSH.3 Sending Application is required");
        
        // MSH.7 - Date/Time of message (required, format: YYYYMMDDHHMMSS)
        Assert.True(Regex.IsMatch(fields[6], @"^\d{8,14}$"), "MSH.7 must be valid timestamp format YYYYMMDDHHMMSS");
        
        // MSH.9 - Message Type (required, format: MSG^EVENT^MSG_CTRL_ID)
        Assert.Contains("^", fields[8]);
        var messageType = fields[8].Split('^');
        Assert.True(messageType.Length >= 2, "MSH.9 must have format MSG^EVENT");
        
        // MSH.10 - Message Control ID (required, unique identifier)
        Assert.False(string.IsNullOrWhiteSpace(fields[9]), "MSH.10 Message Control ID is required");
        
        // MSH.11 - Processing ID (required, usually P for production, T for test)
        Assert.Matches(@"^[PTD]$", fields[10]);
        
        // MSH.12 - Version ID (required, should be 2.3 for HL7 v2.3)
        Assert.Equal("2.3", fields[11]);
    }

    private static void ValidateHL7v23_EVN_Segment(string message)
    {
        var segments = message.Split("\r\n");
        var evnSegment = segments.FirstOrDefault(s => s.StartsWith("EVN"));
        Assert.NotNull(evnSegment);
        
        var fields = evnSegment.Split('|');
        
        // HL7 v2.3 EVN Segment Specification
        Assert.True(fields.Length >= 3, "EVN segment must have minimum 3 fields");
        
        // EVN.1 - Event Type Code (required)
        Assert.False(string.IsNullOrWhiteSpace(fields[1]), "EVN.1 Event Type Code is required");
        
        // EVN.2 - Recorded Date/Time (required, format: YYYYMMDDHHMMSS)
        Assert.True(Regex.IsMatch(fields[2], @"^\d{8,14}$"), "EVN.2 must be valid timestamp");
    }

    private static void ValidateHL7v23_PID_Segment(string message)
    {
        var segments = message.Split("\r\n");
        var pidSegment = segments.FirstOrDefault(s => s.StartsWith("PID"));
        Assert.NotNull(pidSegment);
        
        var fields = pidSegment.Split('|');
        
        // HL7 v2.3 PID Segment Specification (30 fields total)
        Assert.True(fields.Length >= 6, "PID segment must have minimum 6 fields");
        
        // PID.1 - Set ID (required, usually 1)
        Assert.True(int.TryParse(fields[1], out var setId) && setId > 0, "PID.1 must be positive integer");
        
        // PID.3 - Patient Identifier List (required)
        Assert.False(string.IsNullOrWhiteSpace(fields[3]), "PID.3 Patient ID is required");
        
        // PID.5 - Patient Name (required, format: Family^Given^Middle^Suffix^Prefix)
        Assert.Contains("^", fields[5]);
        var nameComponents = fields[5].Split('^');
        Assert.True(nameComponents.Length >= 2, "PID.5 must have at least Family^Given");
        Assert.False(string.IsNullOrWhiteSpace(nameComponents[0]), "Family name is required");
        Assert.False(string.IsNullOrWhiteSpace(nameComponents[1]), "Given name is required");
        
        // PID.7 - Date of Birth (optional, format: YYYYMMDD if present)
        if (fields.Length > 7 && !string.IsNullOrWhiteSpace(fields[7]))
        {
            Assert.True(Regex.IsMatch(fields[7], @"^\d{8}$"), "PID.7 Date of Birth must be YYYYMMDD format");
        }
        
        // PID.8 - Administrative Sex (optional, but if present must be M/F/O/U)
        if (fields.Length > 8 && !string.IsNullOrWhiteSpace(fields[8]))
        {
            Assert.Matches(@"^[MFOU]$", fields[8]);
        }
    }

    private static void ValidateHL7v23_PV1_Segment(string message)
    {
        var segments = message.Split("\r\n");
        var pv1Segment = segments.FirstOrDefault(s => s.StartsWith("PV1"));
        Assert.NotNull(pv1Segment);
        
        var fields = pv1Segment.Split('|');
        
        // HL7 v2.3 PV1 Segment Specification (52 fields total)
        Assert.True(fields.Length >= 4, "PV1 segment must have minimum 4 fields");
        
        // PV1.1 - Set ID (required, usually 1)
        Assert.True(int.TryParse(fields[1], out var setId) && setId > 0, "PV1.1 must be positive integer");
        
        // PV1.2 - Patient Class (required: E=Emergency, I=Inpatient, O=Outpatient, etc.)
        Assert.Matches(@"^[EIOPRN]$", fields[2]);
        
        // PV1.3 - Assigned Patient Location (optional but if present, format: Building^Room^Bed)
        if (!string.IsNullOrWhiteSpace(fields[3]))
        {
            Assert.Contains("^", fields[3]);
        }
    }

    private static void ValidateHL7v23_OBR_Segment(string message)
    {
        var segments = message.Split("\r\n");
        var obrSegment = segments.FirstOrDefault(s => s.StartsWith("OBR"));
        Assert.NotNull(obrSegment);
        
        var fields = obrSegment.Split('|');
        
        // HL7 v2.3 OBR Segment Specification
        Assert.True(fields.Length >= 5, "OBR segment must have minimum 5 fields");
        
        // OBR.1 - Set ID (required)
        Assert.True(int.TryParse(fields[1], out var setId) && setId > 0, "OBR.1 must be positive integer");
        
        // OBR.4 - Universal Service Identifier (required)
        Assert.False(string.IsNullOrWhiteSpace(fields[3]), "OBR.4 Universal Service Identifier is required");
    }

    private static void ValidateHL7v23_OBX_Segment(string message)
    {
        var segments = message.Split("\r\n");
        var obxSegment = segments.FirstOrDefault(s => s.StartsWith("OBX"));
        Assert.NotNull(obxSegment);
        
        var fields = obxSegment.Split('|');
        
        // HL7 v2.3 OBX Segment Specification
        Assert.True(fields.Length >= 6, "OBX segment must have minimum 6 fields");
        
        // OBX.1 - Set ID (required)
        Assert.True(int.TryParse(fields[1], out var setId) && setId > 0, "OBX.1 must be positive integer");
        
        // OBX.2 - Value Type (required: TX, NM, CE, etc.)
        Assert.Matches(@"^(TX|NM|CE|ST|DT|TM|TS)$", fields[2]);
        
        // OBX.3 - Observation Identifier (required)
        Assert.False(string.IsNullOrWhiteSpace(fields[3]), "OBX.3 Observation Identifier is required");
    }

    private static void ValidateHL7v23_RXE_Segment(string message)
    {
        var segments = message.Split("\r\n");
        var rxeSegment = segments.FirstOrDefault(s => s.StartsWith("RXE"));
        Assert.NotNull(rxeSegment);
        
        var fields = rxeSegment.Split('|');
        
        // HL7 v2.3 RXE Segment Specification  
        Assert.True(fields.Length >= 3, "RXE segment must have minimum 3 fields");
        
        // RXE.2 - Give Code (required - medication identifier)
        Assert.False(string.IsNullOrWhiteSpace(fields[2]), "RXE.2 Give Code (medication) is required");
    }
}

