// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Infrastructure.Standards.Common.HL7;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;
using Pidgeon.Core.Domain.Messaging.HL7v2.Segments;
using Xunit;

namespace Pidgeon.Core.Tests;

public class HL7ParserTests
{
    [Fact]
    public void ParseMessage_WithValidADTMessage_ShouldSucceed()
    {
        // Arrange
        var hl7Message = "MSH|^~\\&|Pidgeon|PidgeonCore|||20250826153540||ADT^A01|MSG123456|P|2.3|||||\r" +
                        "PID|1||29010460||Lee^Lisa||20200711|F|||789 Park Dr^^Washington^GA^62778^US||(727) 268-7520||||||988-63-3391|||\r" +
                        "PV1||E|Primary Care Clinic||||1773182582^Davis^Nancy^^^^^^|||||||||||||||||||||||||||||||||||||20250817000000|";
        
        var parser = new HL7Parser();

        // Act
        var result = parser.ParseMessage(hl7Message);

        // Assert
        Assert.True(result.IsSuccess, result.IsFailure ? $"Parser failed: {result.Error.Message}" : "");
        Assert.NotNull(result.Value);
        Assert.Equal("ADT", result.Value.MessageType.MessageCode);
        Assert.Equal(3, result.Value.Segments.Count);
        
        var allSegments = result.Value.Segments;
        // Verify segment types
        Assert.IsType<MSHSegment>(allSegments[0]);
        Assert.IsType<PIDSegment>(allSegments[1]); 
        Assert.IsType<PV1Segment>(allSegments[2]);
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
        
        var parser = new HL7Parser();

        // Act
        var result = parser.ParseMessage(hl7Message);

        // Assert
        Assert.True(result.IsSuccess, result.IsFailure ? $"Parser failed: {result.Error.Message}" : "");
        Assert.NotNull(result.Value);
        Assert.Equal("RDE", result.Value.MessageType.MessageCode);
        Assert.Equal(5, result.Value.Segments.Count);
    }

    [Fact]
    public void ParseSegment_WithValidPV1Segment_ShouldSucceed()
    {
        // Arrange
        var segmentString = "PV1||E|Primary Care Clinic||||1773182582^Davis^Nancy^^^^^^|||||||||||||||||||||||||||||||||||||20250817000000|";
        var parser = new HL7Parser();

        // Act
        var result = parser.ParseSegment(segmentString);

        // Assert
        Assert.True(result.IsSuccess, result.IsFailure ? $"Segment parsing failed: {result.Error.Message}" : "");
        Assert.NotNull(result.Value);
        Assert.Equal("PV1", result.Value.SegmentId);
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
        var parser = new HL7Parser();

        // Act
        var result = parser.ParseMessage(invalidMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("must start with MSH", result.Error.Message);
    }

    [Fact]
    public void ParseMessage_WithEmptyMessage_ShouldFail()
    {
        // Arrange
        var parser = new HL7Parser();

        // Act
        var result = parser.ParseMessage("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("empty", result.Error.Message);
    }

    [Fact]
    public void ParseSegment_WithUnknownSegmentType_ShouldCreateGenericSegment()
    {
        // Arrange
        var unknownSegment = "ZZZ|field1|field2|field3";
        var parser = new HL7Parser();

        // Act
        var result = parser.ParseSegment(unknownSegment);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("ZZZ", result.Value.SegmentId);
        // Should create a GenericSegment for unknown types
    }

    [Fact]
    public void ADTMessage_ParseHL7String_ShouldWork()
    {
        // Arrange - Test ADTMessage parsing directly (bypassing HL7Parser)
        var hl7Message = "MSH|^~\\&|Pidgeon|PidgeonCore|||20250826153540||ADT^A01|MSG123456|P|2.3|||||\r" +
                        "PID|1||29010460||Lee^Lisa||20200711|F|||789 Park Dr^^Washington^GA^62778^US||(727) 268-7520||||||988-63-3391|||\r" +
                        "PV1||E|Primary Care Clinic||||1773182582^Davis^Nancy^^^^^^|||||||||||||||||||||||||||||||||||||20250817000000|";
        
        var adtMessage = new ADTMessage();

        // Act - Call ParseHL7String directly
        var result = adtMessage.ParseHL7String(hl7Message);

        // Assert
        Assert.True(result.IsSuccess, result.IsFailure ? $"ADT parsing failed: {result.Error.Message}" : "");
        Assert.Equal(3, adtMessage.Segments.Count);
    }
}