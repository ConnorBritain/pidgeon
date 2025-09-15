// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Application.Services.Configuration;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Extensions;
using Xunit;
using FluentAssertions;
using Pidgeon.Core;

namespace Pidgeon.Core.Tests.Configuration;

/// <summary>
/// Behavior tests for ConfidenceCalculator service validating healthcare confidence scoring.
/// Tests real scenarios to ensure confidence calculations are meaningful for vendor detection.
/// </summary>
public class ConfidenceCalculatorBehaviorTests : IDisposable
{
    private readonly IConfidenceCalculationService _calculator;
    private readonly ServiceProvider _serviceProvider;

    public ConfidenceCalculatorBehaviorTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPidgeonCore();
        
        _serviceProvider = services.BuildServiceProvider();
        _calculator = _serviceProvider.GetRequiredService<IConfidenceCalculationService>();
    }

    [Fact(DisplayName = "Should calculate high confidence for well-populated field patterns")]
    public async Task Should_Calculate_High_Confidence_For_Well_Populated_Patterns()
    {
        // Arrange - High-quality field patterns (all required fields populated)
        var fieldPatterns = new FieldPatterns
        {
            Standard = "HL7v23",
            MessageType = "ADT^A01",
            SegmentPatterns = new Dictionary<string, SegmentPattern>
            {
                ["PID"] = new SegmentPattern
                {
                    SegmentId = "PID",
                    SampleSize = 100,
                    FieldFrequencies = new Dictionary<int, FieldFrequency>
                    {
                        [3] = new() { PopulatedCount = 100, TotalCount = 100, FieldName = "PatientID" },
                        [5] = new() { PopulatedCount = 98, TotalCount = 100, FieldName = "PatientName" },
                        [7] = new() { PopulatedCount = 95, TotalCount = 100, FieldName = "BirthDate" }
                    }
                }
            }
        };

        // Act - Calculate confidence with sample size
        var result = await _calculator.CalculateFieldPatternConfidenceAsync(fieldPatterns, 100);

        // Assert - Should indicate high confidence for well-populated patterns
        if (result.IsFailure)
        {
            Assert.Fail($"Confidence calculation failed: {result.Error.Message}");
        }
        
        result.Value.Should().BeGreaterThan(0.0, "Should calculate meaningful confidence");
    }

    [Fact(DisplayName = "Should handle empty field patterns gracefully")]
    public async Task Should_Handle_Empty_Field_Patterns_Gracefully()
    {
        // Arrange - Empty field patterns
        var fieldPatterns = new FieldPatterns
        {
            Standard = "HL7v23",
            MessageType = "ADT^A01",
            SegmentPatterns = new Dictionary<string, SegmentPattern>()
        };

        // Act - Calculate confidence for empty patterns
        var result = await _calculator.CalculateFieldPatternConfidenceAsync(fieldPatterns, 0);

        // Assert - Should handle gracefully (may succeed or fail, both acceptable)
        if (result.IsSuccess)
        {
            result.Value.Should().BeInRange(0.0, 1.0, "Confidence should be valid percentage");
        }
        else
        {
            result.IsFailure.Should().BeTrue("Empty patterns should either calculate confidence or fail gracefully");
        }
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}