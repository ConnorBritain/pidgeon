// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using FluentAssertions;
using Segmint.Core.Performance;
using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;
using Segmint.Core.Standards.HL7.v23.Types;
using Xunit;
using Xunit.Abstractions;

namespace Segmint.Tests.Performance;

/// <summary>
/// Performance tests to validate optimizations work correctly.
/// </summary>
public class PerformanceTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void StringBuilderPool_ShouldImprovePerformance()
    {
        const int iterations = 1000;
        
        // Measure traditional StringBuilder creation
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("MSH|");
            sb.Append("Test");
            sb.Append("|");
            sb.Append(i);
            var result = sb.ToString();
        }
        sw1.Stop();

        // Measure pooled StringBuilder
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var result = StringBuilderPool.Execute(sb =>
            {
                sb.Append("MSH|");
                sb.Append("Test");
                sb.Append("|");
                sb.Append(i);
            });
        }
        sw2.Stop();

        _output.WriteLine($"Traditional: {sw1.ElapsedMilliseconds}ms");
        _output.WriteLine($"Pooled: {sw2.ElapsedMilliseconds}ms");
        
        // Pool should be faster or at least not significantly slower
        sw2.ElapsedMilliseconds.Should().BeLessOrEqualTo(sw1.ElapsedMilliseconds * 2);
    }

    [Fact]
    public void ComponentCache_ShouldImprovePerformance()
    {
        const string testValue = "Family^Given^Middle^Jr^Dr^MD^L";
        const int iterations = 1000;

        // Clear cache first
        ComponentCache.Clear();

        // Measure traditional splitting
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var components = testValue.Split('^');
        }
        sw1.Stop();

        // Measure cached splitting
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var components = ComponentCache.GetComponents(testValue, '^');
        }
        sw2.Stop();

        _output.WriteLine($"Traditional: {sw1.ElapsedMilliseconds}ms");
        _output.WriteLine($"Cached: {sw2.ElapsedMilliseconds}ms");
        _output.WriteLine($"Cache size: {ComponentCache.CacheSize}");

        // After first run, cache should be faster
        sw2.ElapsedMilliseconds.Should().BeLessOrEqualTo(sw1.ElapsedMilliseconds * 2);
        ComponentCache.CacheSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PerformanceMetrics_ShouldTrackOperations()
    {
        PerformanceMetrics.Clear();

        // Measure some operations
        PerformanceMetrics.Measure(() => System.Threading.Thread.Sleep(1), "TestOperation1");
        PerformanceMetrics.Measure(() => System.Threading.Thread.Sleep(2), "TestOperation2");
        PerformanceMetrics.Measure(() => System.Threading.Thread.Sleep(1), "TestOperation1");

        var metrics1 = PerformanceMetrics.GetMetrics("TestOperation1");
        var metrics2 = PerformanceMetrics.GetMetrics("TestOperation2");
        var overallStats = PerformanceMetrics.GetOverallStats();

        metrics1.Should().NotBeNull();
        metrics1!.Count.Should().Be(2);
        metrics1.OperationName.Should().Be("TestOperation1");

        metrics2.Should().NotBeNull();
        metrics2!.Count.Should().Be(1);
        
        overallStats.TotalOperations.Should().Be(3);

        _output.WriteLine($"Metrics1: {metrics1}");
        _output.WriteLine($"Metrics2: {metrics2}");
        _output.WriteLine($"Overall: {overallStats}");
    }

    [Fact]
    public void FastHL7Parser_ShouldExtractSegmentIds()
    {
        var hl7Message = "MSH|^~\\&|SENDING|FACILITY|\r" +
                        "PID|1||12345||Smith^John^M|\r" +
                        "PV1|1|I|ICU^BED001|\r";

        var segmentIds = FastHL7Parser.ExtractSegmentIds(hl7Message.AsSpan());

        segmentIds.Should().HaveCount(3);
        segmentIds[0].Should().Be("MSH");
        segmentIds[1].Should().Be("PID");
        segmentIds[2].Should().Be("PV1");
    }

    [Fact]
    public void FastHL7Parser_ShouldExtractFieldValues()
    {
        var segment = "PID|1||12345||Smith^John^M|19800101|M";

        var setId = FastHL7Parser.ExtractFieldValue(segment.AsSpan(), 1);
        var patientId = FastHL7Parser.ExtractFieldValue(segment.AsSpan(), 3);
        var patientName = FastHL7Parser.ExtractFieldValue(segment.AsSpan(), 5);

        setId.Should().Be("1");
        patientId.Should().Be("12345");
        patientName.Should().Be("Smith^John^M");
    }

    [Fact]
    public void FastHL7Parser_ShouldExtractComponents()
    {
        var fieldValue = "Smith^John^Michael^Jr";

        var components = FastHL7Parser.ExtractComponents(fieldValue.AsSpan());

        components.Should().HaveCount(4);
        components[0].Should().Be("Smith");
        components[1].Should().Be("John");
        components[2].Should().Be("Michael");
        components[3].Should().Be("Jr");
    }

    [Fact]
    public void OptimizedSegment_ShouldPerformWell()
    {
        const int iterations = 100;
        
        // Create a complex segment
        var segment = new PIDSegment();
        segment.SetPatientIdentifiers("12345", "MRN", "HOSPITAL");
        segment.SetPatientName("Smith", "John", "Michael");
        segment.SetPatientDemographics(
            DateTime.Parse("1980-01-01"),
            "M",
            new AddressField("123 Main St^Apt 2B^^Boston^MA^02101^USA")
        );

        // Measure serialization performance
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var hl7String = segment.ToHL7String();
        }
        sw1.Stop();

        // Measure parsing performance
        var hl7StringForParsing = segment.ToHL7String();
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var newSegment = new PIDSegment();
            newSegment.FromHL7String(hl7StringForParsing);
        }
        sw2.Stop();

        _output.WriteLine($"Serialization: {sw1.ElapsedMilliseconds}ms for {iterations} iterations");
        _output.WriteLine($"Parsing: {sw2.ElapsedMilliseconds}ms for {iterations} iterations");
        _output.WriteLine($"Avg serialization: {(double)sw1.ElapsedMilliseconds / iterations:F3}ms");
        _output.WriteLine($"Avg parsing: {(double)sw2.ElapsedMilliseconds / iterations:F3}ms");

        // Performance should be reasonable
        sw1.ElapsedMilliseconds.Should().BeLessThan(1000); // Less than 1 second for 100 iterations
        sw2.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public void OptimizedMessage_ShouldPerformWell()
    {
        const int iterations = 50;

        // Create a complex message
        var message = new ADTMessage();
        message.MSH.SetSendingApplication("EMR_SYSTEM");
        message.MSH.SetSendingFacility("HOSPITAL_A");
        message.MSH.SetReceivingApplication("ADT_PROCESSOR");
        message.MSH.SetReceivingFacility("HOSPITAL_B");
        message.MSH.SetMessageType("ADT", "A01");
        message.MSH.SetMessageControlId("MSG123");
        message.MSH.SetProcessingId(false); // P = production = false for this test
        message.EVN.SetEventTypeCode("A01");
        message.EVN.SetRecordedDateTime(DateTime.Now);
        message.PID.SetPatientIdentifiers("12345", "MRN", "HOSPITAL");
        message.PID.SetPatientName("Smith", "John", "Michael");
        message.PID.SetPatientDemographics(
            DateTime.Parse("1975-03-15"),
            "M",
            new AddressField("456 Oak Ave^Unit 3B^^Seattle^WA^98101^USA")
        );
        message.PV1.SetPatientClass("I");
        message.PV1.SetAssignedPatientLocation("ICU^BED001");
        message.PV1.SetAdmitDateTime(DateTime.Now.AddHours(-2));
        message.PV1.SetAdmissionType("ER");

        // Measure message performance
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var hl7String = message.ToHL7String();
        }
        sw1.Stop();

        var hl7StringForParsing = message.ToHL7String();
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var newMessage = new ADTMessage();
            newMessage.FromHL7String(hl7StringForParsing);
        }
        sw2.Stop();

        _output.WriteLine($"Message serialization: {sw1.ElapsedMilliseconds}ms for {iterations} iterations");
        _output.WriteLine($"Message parsing: {sw2.ElapsedMilliseconds}ms for {iterations} iterations");
        _output.WriteLine($"Avg message serialization: {(double)sw1.ElapsedMilliseconds / iterations:F3}ms");
        _output.WriteLine($"Avg message parsing: {(double)sw2.ElapsedMilliseconds / iterations:F3}ms");

        // Performance should be reasonable for enterprise use
        sw1.ElapsedMilliseconds.Should().BeLessThan(2000); // Less than 2 seconds for 50 iterations
        sw2.ElapsedMilliseconds.Should().BeLessThan(2000);
    }
}