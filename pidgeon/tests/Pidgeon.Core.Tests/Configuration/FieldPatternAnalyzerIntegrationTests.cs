// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;
using Pidgeon.Core.Application.Services.Configuration;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Extensions;
using Xunit;
using FluentAssertions;
using Pidgeon.Core;

namespace Pidgeon.Core.Tests.Configuration;

/// <summary>
/// Integration tests for FieldPatternAnalyzer service validating plugin→adapter delegation flow.
/// Tests real healthcare scenarios to ensure vendor pattern detection works correctly.
/// </summary>
public class FieldPatternAnalyzerIntegrationTests : IDisposable
{
    private readonly IFieldPatternAnalysisService _analyzer;
    private readonly ServiceProvider _serviceProvider;

    public FieldPatternAnalyzerIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPidgeonCore();
        
        _serviceProvider = services.BuildServiceProvider();
        _analyzer = _serviceProvider.GetRequiredService<IFieldPatternAnalysisService>();
    }

    [Fact(DisplayName = "Should analyze field patterns from real HL7 ADT messages")]
    public async Task Should_Analyze_Field_Patterns_From_Real_HL7_Messages()
    {
        // Arrange - Real healthcare scenario: Hospital admission messages
        var hl7Messages = new[]
        {
            "MSH|^~\\&|Epic|Hospital|||20250826153540||ADT^A01|MSG123456|P|2.3|||||\r" +
            "PID|1||PAT1234567||Doe^John^M||19801015|M|||123 Main St^^Cleveland^OH^44101^US||(216) 555-0123||||||123-45-6789|||\r" +
            "PV1||I|ICU^101^A||||1234567890^Smith^Jane^^^^^^|||||||||||||||||||||||||||||||||||||20250826000000|"
        };

        // Act - Analyze patterns using plugin→adapter flow
        var result = await _analyzer.AnalyzeAsync(hl7Messages, "HL7v23", "ADT^A01");

        // Assert - Verify meaningful pattern detection
        if (result.IsFailure)
        {
            Assert.True(false, $"Pattern analysis failed: {result.Error.Message}");
        }
        
        result.Value.Should().NotBeNull();
        result.Value.Standard.Should().NotBeNullOrEmpty();
        result.Value.MessageType.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "Should analyze individual segment patterns for vendor detection")]
    public async Task Should_Analyze_Individual_Segment_Patterns()
    {
        // Arrange - Individual PID segments from different vendors
        var pidSegments = new[]
        {
            "PID|1||PAT1234567||Doe^John^M||19801015|M|||123 Main St^^Cleveland^OH^44101^US||(216) 555-0123||||||123-45-6789|||"
        };

        // Act - Analyze segment patterns
        var result = await _analyzer.AnalyzeSegmentAsync(pidSegments, "PID", "HL7v23");

        // Assert - Should extract PID-specific patterns
        if (result.IsFailure)
        {
            Assert.True(false, $"Segment analysis failed: {result.Error.Message}");
        }
        
        result.Value.Should().NotBeNull();
        result.Value.SegmentId.Should().Be("PID");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}