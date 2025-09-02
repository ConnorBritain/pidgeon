// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Infrastructure.Standards.Common.HL7.Utilities;
using Xunit;

namespace Pidgeon.Core.Tests.Infrastructure;

public class MshHeaderParserTests
{
    private readonly List<string> _sampleSegments;
    
    public MshHeaderParserTests()
    {
        // Sample HL7 message segments with complete MSH header
        _sampleSegments = new List<string>
        {
            "MSH|^~\\&|EPIC|EPICADT|SMS|SMSADT|20240101120000||ADT^A01|MSG00001|P|2.3|",
            "PID|||123456^^^MRN||DOE^JOHN^M||19800101|M|||123 MAIN ST^^CITY^ST^12345|",
            "PV1||I|ICU^101^A||||12345^SMITH^ROBERT^J^MD|67890^JONES^MARY^A^MD||MED||||"
        };
    }
    
    [Fact(DisplayName = "Should extract message control ID from MSH.10")]
    public void ExtractMessageControlId_WithValidMSH_ReturnsCorrectValue()
    {
        // Act
        var result = MshHeaderParser.ExtractMessageControlId(_sampleSegments);
        
        // Assert
        Assert.Equal("MSG00001", result);
    }
    
    [Fact(DisplayName = "Should extract sending application from MSH.3")]
    public void ExtractSendingSystem_WithValidMSH_ReturnsCorrectValue()
    {
        // Act
        var result = MshHeaderParser.ExtractSendingSystem(_sampleSegments);
        
        // Assert
        Assert.Equal("EPIC", result);
    }
    
    [Fact(DisplayName = "Should extract receiving application from MSH.5")]
    public void ExtractReceivingSystem_WithValidMSH_ReturnsCorrectValue()
    {
        // Act
        var result = MshHeaderParser.ExtractReceivingSystem(_sampleSegments);
        
        // Assert
        Assert.Equal("SMS", result);
    }
    
    [Fact(DisplayName = "Should extract version ID from MSH.12")]
    public void ExtractVersionId_WithValidMSH_ReturnsCorrectValue()
    {
        // Act
        var result = MshHeaderParser.ExtractVersionId(_sampleSegments);
        
        // Assert
        Assert.Equal("2.3", result);
    }
    
    [Fact(DisplayName = "Should extract sending facility from MSH.4")]
    public void ExtractSendingFacility_WithValidMSH_ReturnsCorrectValue()
    {
        // Act
        var result = MshHeaderParser.ExtractSendingFacility(_sampleSegments);
        
        // Assert
        Assert.Equal("EPICADT", result);
    }
    
    [Fact(DisplayName = "Should extract receiving facility from MSH.6")]
    public void ExtractReceivingFacility_WithValidMSH_ReturnsCorrectValue()
    {
        // Act
        var result = MshHeaderParser.ExtractReceivingFacility(_sampleSegments);
        
        // Assert
        Assert.Equal("SMSADT", result);
    }
    
    [Fact(DisplayName = "Should extract all header fields at once")]
    public void ExtractAllHeaderFields_WithValidMSH_ReturnsAllFields()
    {
        // Act
        var result = MshHeaderParser.ExtractAllHeaderFields(_sampleSegments);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("EPIC", result.SendingApplication);
        Assert.Equal("EPICADT", result.SendingFacility);
        Assert.Equal("SMS", result.ReceivingApplication);
        Assert.Equal("SMSADT", result.ReceivingFacility);
        Assert.Equal("MSG00001", result.MessageControlId);
        Assert.Equal("2.3", result.VersionId);
    }
    
    [Fact(DisplayName = "Should return null when MSH segment is missing")]
    public void ExtractMessageControlId_WithNoMSH_ReturnsNull()
    {
        // Arrange
        var segmentsWithoutMsh = new List<string>
        {
            "PID|||123456^^^MRN||DOE^JOHN^M||19800101|M|||",
            "PV1||I|ICU^101^A||||12345^SMITH^ROBERT^J^MD||"
        };
        
        // Act
        var result = MshHeaderParser.ExtractMessageControlId(segmentsWithoutMsh);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact(DisplayName = "Should handle MSH with missing fields gracefully")]
    public void ExtractVersionId_WithIncompleteMSH_ReturnsNull()
    {
        // Arrange - MSH with only 8 fields (version is field 12)
        var incompleteSegments = new List<string>
        {
            "MSH|^~\\&|EPIC|EPICADT|SMS|SMSADT|20240101120000|",
            "PID|||123456^^^MRN||DOE^JOHN^M||19800101|M|||"
        };
        
        // Act
        var result = MshHeaderParser.ExtractVersionId(incompleteSegments);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact(DisplayName = "Should extract custom field using generic method")]
    public void ExtractMshField_WithCustomIndex_ReturnsCorrectValue()
    {
        // Act - Extract message type from MSH.9 (index 8)
        var result = MshHeaderParser.ExtractMshField(_sampleSegments, 8);
        
        // Assert
        Assert.Equal("ADT^A01", result);
    }
}