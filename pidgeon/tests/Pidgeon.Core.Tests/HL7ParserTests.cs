// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Infrastructure.Standards.Common.HL7;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;
using Pidgeon.Core.Domain.Messaging.HL7v2.Segments;
using Pidgeon.Core;
using Xunit;

namespace Pidgeon.Core.Tests;

/// <summary>
/// Base class for HL7 parser tests providing common test patterns and assertion helpers.
/// </summary>
public abstract class HL7ParserTestBase
{
    protected HL7Parser CreateParser() => new();

    protected void AssertSuccessfulParse(Result<HL7Message> result, string context = "")
    {
        var errorMessage = result.IsFailure ? $"{context} failed: {result.Error.Message}" : "";
        Assert.True(result.IsSuccess, errorMessage);
        Assert.NotNull(result.Value);
    }

    protected void AssertSuccessfulSegmentParse(Result<HL7Segment> result, string expectedSegmentId, string context = "")
    {
        var errorMessage = result.IsFailure ? $"{context} failed: {result.Error.Message}" : "";
        Assert.True(result.IsSuccess, errorMessage);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedSegmentId, result.Value.SegmentId);
    }

    protected void AssertFailedParse<T>(Result<T> result, string expectedErrorFragment)
    {
        Assert.False(result.IsSuccess);
        Assert.Contains(expectedErrorFragment, result.Error.Message);
    }

    protected void AssertMessageStructure(HL7Message message, string expectedMessageCode, int expectedSegmentCount, params Type[] expectedSegmentTypes)
    {
        Assert.Equal(expectedMessageCode, message.MessageType.MessageCode);
        Assert.Equal(expectedSegmentCount, message.Segments.Count);
        
        for (int i = 0; i < expectedSegmentTypes.Length; i++)
        {
            Assert.IsType(expectedSegmentTypes[i], message.Segments[i]);
        }
    }
}

public class HL7ParserTests : HL7ParserTestBase
{
    [Fact]
    public void ParseMessage_WithValidADTMessage_ShouldSucceed()
    {
        // Arrange
        var hl7Message = "MSH|^~\\&|Pidgeon|PidgeonCore|||20250826153540||ADT^A01|MSG123456|P|2.3|||||\r" +
                        "PID|1||29010460||Lee^Lisa||20200711|F|||789 Park Dr^^Washington^GA^62778^US||(727) 268-7520||||||988-63-3391|||\r" +
                        "PV1||E|Primary Care Clinic||||1773182582^Davis^Nancy^^^^^^|||||||||||||||||||||||||||||||||||||20250817000000|";

        // Act
        var result = CreateParser().ParseMessage(hl7Message);

        // Assert
        AssertSuccessfulParse(result, "ADT message parsing");
        AssertMessageStructure(result.Value, "ADT", 3, typeof(MSHSegment), typeof(PIDSegment), typeof(PV1Segment));
    }

    [Fact]
    public void ParseMessage_WithRDEMessage_ShouldSucceed()
    {
        // Arrange
        var hl7Message = "MSH|^~\\&|Pidgeon|PidgeonPharmacy|||20250826142339||RDE^O01|MSG789012|P|2.3|||||\r" +
                        "PID|1||11443873||Sanchez^Carmen||19900520|F|||789 Main St^^Washington^CA^71738^US||(426) 445-9083||||||960-34-1125|||\r" +
                        "ORC|NW|RX9859719|||||||20250729000000|||Perez, Hiroshi^\r" +
                        "RXE|Do not crush or chew|Trazodone^Desyrel 50mg|2||dose||Do not crush or chew|||30|tablets|0|||RX9859719\r" +
                        "RXR|PO^Oral|||";

        // Act
        var result = CreateParser().ParseMessage(hl7Message);

        // Assert
        AssertSuccessfulParse(result, "RDE message parsing");
        AssertMessageStructure(result.Value, "RDE", 5);
    }

    [Fact]
    public void ParseSegment_WithValidPV1Segment_ShouldSucceed()
    {
        // Arrange
        var segmentString = "PV1||E|Primary Care Clinic||||1773182582^Davis^Nancy^^^^^^|||||||||||||||||||||||||||||||||||||20250817000000|";

        // Act
        var result = CreateParser().ParseSegment(segmentString);

        // Assert
        AssertSuccessfulSegmentParse(result, "PV1", "PV1 segment parsing");
        Assert.IsType<PV1Segment>(result.Value);
        
        var pv1 = (PV1Segment)result.Value;
        Assert.Equal("E", pv1.PatientClass.Value);
        Assert.Equal("Primary Care Clinic", pv1.AssignedPatientLocation.Value);
    }

    [Fact]
    public void ParseMessage_WithInvalidMessage_ShouldFail()
    {
        // Arrange
        var invalidMessage = "INVALID|MESSAGE|FORMAT";

        // Act
        var result = CreateParser().ParseMessage(invalidMessage);

        // Assert
        AssertFailedParse(result, "must start with MSH");
    }

    [Fact]
    public void ParseMessage_WithEmptyMessage_ShouldFail()
    {
        // Act
        var result = CreateParser().ParseMessage("");

        // Assert
        AssertFailedParse(result, "empty");
    }

    [Fact]
    public void ParseSegment_WithUnknownSegmentType_ShouldCreateGenericSegment()
    {
        // Arrange
        var unknownSegment = "ZZZ|field1|field2|field3";

        // Act
        var result = CreateParser().ParseSegment(unknownSegment);

        // Assert
        AssertSuccessfulSegmentParse(result, "ZZZ", "Unknown segment parsing");
    }

    [Fact]
    public void ADTMessage_ParseHL7String_ShouldWork()
    {
        // Arrange
        var hl7Message = "MSH|^~\\&|Pidgeon|PidgeonCore|||20250826153540||ADT^A01|MSG123456|P|2.3|||||\r" +
                        "PID|1||29010460||Lee^Lisa||20200711|F|||789 Park Dr^^Washington^GA^62778^US||(727) 268-7520||||||988-63-3391|||\r" +
                        "PV1||E|Primary Care Clinic||||1773182582^Davis^Nancy^^^^^^|||||||||||||||||||||||||||||||||||||20250817000000|";
        
        var adtMessage = new ADTMessage();

        // Act
        var result = adtMessage.ParseHL7String(hl7Message);

        // Assert
        AssertSuccessfulParse(result, "Direct ADT message parsing");
        Assert.Equal(3, adtMessage.Segments.Count);
    }
}