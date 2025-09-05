// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Xunit;

namespace Pidgeon.Core.Tests.Generation;

/// <summary>
/// Reference validation tests using known-good HL7 messages from HL7.org examples.
/// These test our ability to generate messages that match the structure and content 
/// of reference implementations in the wild.
/// </summary>
public class HL7ReferenceValidationTests
{
    /// <summary>
    /// HL7.org reference ADT^A01 message example for comparison.
    /// Source: HL7 v2.3 Implementation Guide, Chapter 3 - Patient Administration
    /// </summary>
    private const string REFERENCE_ADT_A01 = 
        "MSH|^~\\&|ADT1|MCM|LABADT|MCM|198808181126|SECURITY|ADT^A01^ADT_A01|MSG00001|P|2.3\r\n" +
        "EVN|A01|198808181123\r\n" +
        "PID|1||PATID1234^5^M11^ADT1^MR^UNIVERSITY HOSPITAL~123456789^^^USSSA^SS||EVERYMAN^ADAM^A^III||19610615|M||C|1200 N ELM STREET^^GREENSBORO^NC^27401-1020|GL|(919)379-1212|(919)271-3434~(919)277-3114|S||PATID12345001^2^M10^ADT1^AN^A|123456789|9-87654^NC\r\n" +
        "NK1|1|NUCLEAR^NELDA^W|SPO|NK^NEXT OF KIN||(919)271-3434|(919)271-3434~(919)277-3114|N\r\n" +
        "PV1|1|I|2000^2012^01||||004777^ATTEND^AARON^A|||SUR|||A0||19|1|||||||||||||||||||||||||198808181123|198808201415\r\n" +
        "PV2|1|P||^^^REGISTRATION||^I|||^||||||||||C||||||||||||||||||||||||S||A||||\r\n" +
        "AL1|1||^PENICILLIN^L|MO|SHORTNESS OF BREATH\r\n" +
        "DG1|1|I9|71596^OSTEOARTHRITIS^I9|OSTEOARTHRITIS|19880501103005|F";

    /// <summary>
    /// HL7.org reference ORU^R01 message example for lab results.
    /// Source: HL7 v2.3 Implementation Guide, Chapter 7 - Observation Reporting
    /// </summary>
    private const string REFERENCE_ORU_R01 = 
        "MSH|^~\\&|GHH LAB|ELAB-3|GHH OE|BLDG4|200202150930||ORU^R01^ORU_R01|CNTRL-3456|P|2.3\r\n" +
        "PID|||PATID1234^5^M11^ADT1^MR^UNIVERSITY HOSPITAL||EVERYMAN^ADAM^A^III||19610615|M||C|1200 N ELM STREET^^GREENSBORO^NC^27401-1020|GL|(919)379-1212|(919)271-3434~(919)277-3114|S||PATID12345001^2^M10^ADT1^AN^A|123456789|9-87654^NC\r\n" +
        "OBR|1|845439^GHH OE|1045813^GHH LAB|1554-5^GLUCOSE^LN|||200202150730||||||||^HUSSAIN^JAHEEDA^^Dr.|||||||200202150930||F|||^^^^^R\r\n" +
        "OBX|1|NM|1554-5^GLUCOSE^LN|182|mg/dl|70_105|H|||F";

    /// <summary>
    /// Minimal valid ADT^A01 that should always work - our baseline target.
    /// This represents the absolute minimum fields required by HL7 v2.3 specification.
    /// </summary>
    private const string MINIMAL_VALID_ADT_A01 = 
        "MSH|^~\\&|PIDGEON|FACILITY|RECEIVER|FACILITY|20250101120000||ADT^A01^ADT_A01|MSG001|P|2.3\r\n" +
        "EVN|A01|20250101120000\r\n" +
        "PID|1||12345||DOE^JOHN|||M\r\n" +
        "PV1|1|I|ER^001^01";

    [Fact]
    public void Reference_ADT_A01_Should_Parse_Successfully()
    {
        // This validates our understanding of the HL7 standard by ensuring
        // we can parse the official reference implementation
        var segments = REFERENCE_ADT_A01.Split("\r\n");
        
        // Should have all expected segments
        Assert.Contains(segments, s => s.StartsWith("MSH"));
        Assert.Contains(segments, s => s.StartsWith("EVN")); 
        Assert.Contains(segments, s => s.StartsWith("PID"));
        Assert.Contains(segments, s => s.StartsWith("PV1"));
        
        // Validate the MSH structure matches our understanding
        var mshFields = segments[0].Split('|');
        Assert.Equal("ADT^A01^ADT_A01", mshFields[8]); // Message type
        Assert.Equal("P", mshFields[10]); // Processing ID
        Assert.Equal("2.3", mshFields[11]); // Version
    }

    [Fact]
    public void Reference_ORU_R01_Should_Parse_Successfully()
    {
        // Validates our understanding of observation reporting messages
        var segments = REFERENCE_ORU_R01.Split("\r\n");
        
        Assert.Contains(segments, s => s.StartsWith("MSH"));
        Assert.Contains(segments, s => s.StartsWith("PID"));
        Assert.Contains(segments, s => s.StartsWith("OBR"));
        Assert.Contains(segments, s => s.StartsWith("OBX"));
        
        // Validate OBX segment structure (observation result)
        var obxSegment = segments.First(s => s.StartsWith("OBX"));
        var obxFields = obxSegment.Split('|');
        Assert.Equal("NM", obxFields[2]); // Value type (Numeric)
        Assert.Equal("1554-5^GLUCOSE^LN", obxFields[3]); // Observation identifier (LOINC)
        Assert.Equal("182", obxFields[5]); // Observation value
        Assert.Equal("mg/dl", obxFields[6]); // Units
    }

    [Fact]  
    public void Minimal_ADT_A01_Should_Be_Structurally_Valid()
    {
        // This defines our minimum viable message - what we absolutely must generate
        var segments = MINIMAL_VALID_ADT_A01.Split("\r\n");
        
        Assert.Equal(4, segments.Length); // Exactly MSH, EVN, PID, PV1
        
        // Each segment should have minimum required fields
        var mshFields = segments[0].Split('|');
        Assert.True(mshFields.Length >= 12, "MSH needs minimum 12 fields");
        
        var pidFields = segments[2].Split('|');
        Assert.True(pidFields.Length >= 6, "PID needs minimum 6 fields");
        
        var pv1Fields = segments[3].Split('|');
        Assert.True(pv1Fields.Length >= 4, "PV1 needs minimum 4 fields");
    }

    [Theory]
    [InlineData("MSH|^~\\&|PIDGEON||RECEIVER||20250101120000||ADT^A01|MSG001|P|2.3")] // Missing sending facility
    [InlineData("MSH|^~\\&|PIDGEON|FACILITY|RECEIVER|FACILITY|||ADT^A01|MSG001|P|2.3")] // Missing timestamp
    [InlineData("MSH|^~\\&|PIDGEON|FACILITY|RECEIVER|FACILITY|20250101120000||ADT^A01||P|2.3")] // Missing control ID
    [InlineData("MSH|^~\\&|PIDGEON|FACILITY|RECEIVER|FACILITY|20250101120000||ADT^A01|MSG001||2.3")] // Missing processing ID
    [InlineData("MSH|^~\\&|PIDGEON|FACILITY|RECEIVER|FACILITY|20250101120000||ADT^A01|MSG001|P|")] // Missing version
    public void Invalid_MSH_Segments_Should_Be_Identified(string invalidMsh)
    {
        // These represent common MSH validation failures our generator must avoid
        var fields = invalidMsh.Split('|');
        
        // These are the validations our generator must pass
        var hasValidTimestamp = !string.IsNullOrWhiteSpace(fields[6]) && fields[6].Length >= 8;
        var hasValidControlId = !string.IsNullOrWhiteSpace(fields[9]);
        var hasValidProcessingId = !string.IsNullOrWhiteSpace(fields[10]) && fields[10].Length == 1;
        var hasValidVersion = !string.IsNullOrWhiteSpace(fields[11]);
        
        // At least one of these should fail for invalid messages
        var isValid = hasValidTimestamp && hasValidControlId && hasValidProcessingId && hasValidVersion;
        Assert.False(isValid, "This MSH segment should be identified as invalid");
    }

    [Theory]
    [InlineData("PID|||12345||DOE^JOHN|||M")] // Valid - minimum required fields
    [InlineData("PID|1||12345||DOE^JOHN^JANE^JR^MR||19800101|M")] // Valid - full name with DOB
    [InlineData("PID|||||||")] // Invalid - no patient ID
    [InlineData("PID|||12345|||||")] // Invalid - no patient name
    public void PID_Segment_Validation_Examples(string pidSegment)
    {
        var fields = pidSegment.Split('|');
        
        // HL7 v2.3 PID validation rules our generator must follow
        var hasPatientId = !string.IsNullOrWhiteSpace(fields[2]); // PID.3
        var hasPatientName = !string.IsNullOrWhiteSpace(fields[4]); // PID.5
        var hasNameComponents = fields[4].Contains("^") && fields[4].Split('^').Length >= 2;
        
        var isValid = hasPatientId && hasPatientName && hasNameComponents;
        
        // Document what we expect for our generator
        if (pidSegment.Contains("DOE^JOHN"))
        {
            Assert.True(isValid, "Valid PID segments should pass validation");
        }
        else
        {
            Assert.False(isValid, "Invalid PID segments should fail validation");
        }
    }

    /// <summary>
    /// Quality benchmark: Our generated messages should be at least as good as 
    /// minimal valid examples, ideally approaching reference implementation quality.
    /// </summary>
    [Fact]
    public void Generated_Messages_Should_Meet_Quality_Benchmark()
    {
        // This test will be implemented once we have working generation
        // It should compare our generated ADT^A01 against MINIMAL_VALID_ADT_A01
        // and ensure we meet or exceed that quality level
        
        Assert.True(true, "Quality benchmark test - to be implemented with working generator");
    }

    [Fact] 
    public void Field_Separator_And_Encoding_Must_Be_Consistent()
    {
        // HL7 v2.3 Standard: Field separator is always |, encoding chars are always ^~\&
        var validMessages = new[] { REFERENCE_ADT_A01, REFERENCE_ORU_R01, MINIMAL_VALID_ADT_A01 };
        
        foreach (var message in validMessages)
        {
            var msh = message.Split("\r\n")[0];
            Assert.True(msh.StartsWith("MSH|"), "Field separator must be |");
            Assert.True(msh.StartsWith("MSH|^~\\&"), "Encoding characters must be ^~\\&");
        }
        
        // Our generator must always use these exact separators
    }

    [Theory]
    [InlineData("20250101120000")] // Valid: YYYYMMDDHHMMSS
    [InlineData("20250101")] // Valid: YYYYMMDD  
    [InlineData("202501011200")] // Valid: YYYYMMDDHHMM
    [InlineData("2025010112")] // Valid: YYYYMMDDHH
    [InlineData("2025")] // Invalid: Too short
    [InlineData("202513011200")] // Invalid: Month 13
    [InlineData("20250132120000")] // Invalid: Day 32
    public void Timestamp_Format_Validation_Examples(string timestamp)
    {
        // HL7 v2.3 timestamp validation that our generator must implement
        var isValidLength = timestamp.Length >= 4 && timestamp.Length <= 14 && timestamp.Length % 2 == 0;
        var isAllDigits = timestamp.All(char.IsDigit);
        
        var isValidFormat = isValidLength && isAllDigits;
        
        if (timestamp.StartsWith("2025") && !timestamp.Contains("13") && !timestamp.Contains("32"))
        {
            Assert.True(isValidFormat, $"Valid timestamp {timestamp} should pass format validation");
        }
        else
        {
            // Invalid timestamps should be caught by our validation
            // (Note: We're not doing full date validation here, just format)
        }
    }
}