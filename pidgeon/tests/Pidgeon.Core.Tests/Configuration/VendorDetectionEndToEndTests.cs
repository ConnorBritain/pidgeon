// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;
using Pidgeon.Core.Application.Services.Configuration;
using Pidgeon.Core.Extensions;
using Xunit;
using FluentAssertions;
using Pidgeon.Core;

namespace Pidgeon.Core.Tests.Configuration;

/// <summary>
/// End-to-end tests for vendor pattern detection workflow.
/// Validates the complete plugin→adapter→service chain for P0 Feature #3.
/// </summary>
public class VendorDetectionEndToEndTests : IDisposable
{
    private readonly IFieldPatternAnalysisService _patternAnalyzer;
    private readonly IConfidenceCalculationService _confidenceCalculator;
    private readonly ServiceProvider _serviceProvider;

    public VendorDetectionEndToEndTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPidgeonCore();
        
        _serviceProvider = services.BuildServiceProvider();
        _patternAnalyzer = _serviceProvider.GetRequiredService<IFieldPatternAnalysisService>();
        _confidenceCalculator = _serviceProvider.GetRequiredService<IConfidenceCalculationService>();
    }

    [Fact(DisplayName = "P0 Feature #3: Should detect vendor patterns from sample HL7 messages")]
    public async Task Should_Detect_Vendor_Patterns_From_Sample_Messages_End_To_End()
    {
        // Arrange - Healthcare scenario: Analyze sample messages to infer vendor configuration
        var sampleMessages = new[]
        {
            "MSH|^~\\&|Epic|MyHospital|||20250826153540||ADT^A01|EPIC123456|P|2.3|||||\r" +
            "PID|1||1234567||Doe^John^Michael||19801015|M|||123 Main St^^Cleveland^OH^44101^US||(216) 555-0123||||||123-45-6789|||"
        };

        // Act - Complete vendor detection workflow
        
        // Step 1: Analyze field patterns
        var patternsResult = await _patternAnalyzer.AnalyzeAsync(sampleMessages, "HL7v23", "ADT^A01");
        patternsResult.IsSuccess.Should().BeTrue("Pattern analysis should succeed");

        // Step 2: Calculate confidence in patterns
        var confidenceResult = await _confidenceCalculator.CalculateFieldPatternConfidenceAsync(patternsResult.Value, 1);
        confidenceResult.IsSuccess.Should().BeTrue("Confidence calculation should succeed");

        // Assert - End-to-end plugin→adapter workflow should complete
        patternsResult.Value.Should().NotBeNull("Should extract patterns");
        confidenceResult.Value.Should().BeInRange(0.0, 1.0, "Should calculate valid confidence");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}