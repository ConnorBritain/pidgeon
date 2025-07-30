// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Segmint.Core.Standards.HL7.v23.Segments;
using Segmint.Core.Standards.HL7.v23.Types;

namespace Segmint.Benchmarks;

/// <summary>
/// Benchmarks for HL7 segment operations to measure serialization and parsing performance.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MarkdownExporter]
public class SegmentOperationsBenchmarks
{
    private PIDSegment _pidSegment = null!;
    private MSHSegment _mshSegment = null!;
    private IN1Segment _in1Segment = null!;
    private string _samplePidString = null!;
    private string _sampleMshString = null!;
    private string _sampleIn1String = null!;
    private const int IterationCount = 500;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Setup PID segment
        _pidSegment = new PIDSegment();
        _pidSegment.SetPatientIdentifiers("12345", "MRN", "HOSPITAL");
        _pidSegment.SetPatientName(new PersonNameField("Doe^John^M"));
        _pidSegment.SetPatientDemographics(
            DateTime.Parse("1980-01-01"),
            "M",
            new AddressField("123 Main St^Apt 2^^Boston^MA^02101^USA")
        );

        // Setup MSH segment
        _mshSegment = new MSHSegment();
        _mshSegment.SetHeader("SENDING_APP", "SENDING_FACILITY", "RECEIVING_APP", "RECEIVING_FACILITY");

        // Setup IN1 segment
        _in1Segment = new IN1Segment();
        _in1Segment.SetBasicInsurance(1, "BCBS001", "Blue Cross Blue Shield", "BCBS", "BCBS Corp", "123456789");

        // Create sample HL7 strings for parsing
        _samplePidString = _pidSegment.ToHL7String();
        _sampleMshString = _mshSegment.ToHL7String();
        _sampleIn1String = _in1Segment.ToHL7String();
    }

    [Benchmark(Baseline = true)]
    public void PIDSegment_ToHL7String()
    {
        var results = new string[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            results[i] = _pidSegment.ToHL7String();
        }
    }

    [Benchmark]
    public void PIDSegment_FromHL7String()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var pid = new PIDSegment();
            pid.FromHL7String(_samplePidString);
        }
    }

    [Benchmark]
    public void PIDSegment_Clone()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var clone = _pidSegment.Clone();
        }
    }

    [Benchmark]
    public void MSHSegment_ToHL7String()
    {
        var results = new string[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            results[i] = _mshSegment.ToHL7String();
        }
    }

    [Benchmark]
    public void MSHSegment_FromHL7String()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var msh = new MSHSegment();
            msh.FromHL7String(_sampleMshString);
        }
    }

    [Benchmark]
    public void IN1Segment_ToHL7String()
    {
        var results = new string[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            results[i] = _in1Segment.ToHL7String();
        }
    }

    [Benchmark]
    public void IN1Segment_FromHL7String()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var in1 = new IN1Segment();
            in1.FromHL7String(_sampleIn1String);
        }
    }

    [Benchmark]
    public void Segment_Validation()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var errors = _pidSegment.Validate();
        }
    }
}