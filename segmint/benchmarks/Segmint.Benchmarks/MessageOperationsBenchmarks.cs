// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;
using Segmint.Core.Standards.HL7.v23.Types;

namespace Segmint.Benchmarks;

/// <summary>
/// Benchmarks for complete HL7 message operations to measure end-to-end performance.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MarkdownExporter]
public class MessageOperationsBenchmarks
{
    private ADTMessage _adtMessage = null!;
    private RDEMessage _rdeMessage = null!;
    private string _sampleAdtString = null!;
    private string _sampleRdeString = null!;
    private const int IterationCount = 100;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Create comprehensive ADT message
        _adtMessage = new ADTMessage();
        
        // MSH
        _adtMessage.MSH.SetHeader("EMR_SYSTEM", "HOSPITAL_A", "ADT_PROCESSOR", "HOSPITAL_B");
        _adtMessage.MSH.SetMessageInfo("ADT", "A01", "MSG123", "P", "2.3");
        
        // EVN
        _adtMessage.EVN.SetEventInfo("A01", DateTime.Now);
        
        // PID
        _adtMessage.PID.SetPatientIdentifiers("12345", "MRN", "HOSPITAL");
        _adtMessage.PID.SetPatientName(new PersonNameField("Smith^John^Michael"));
        _adtMessage.PID.SetPatientDemographics(
            DateTime.Parse("1975-03-15"),
            "M",
            new AddressField("456 Oak Ave^Unit 3B^^Seattle^WA^98101^USA")
        );
        
        // PV1
        _adtMessage.PV1.SetVisitInfo("I", "ICU", "BED001");
        _adtMessage.PV1.SetAdmissionInfo(DateTime.Now.AddHours(-2), "ER");

        // Create comprehensive RDE message
        _rdeMessage = new RDEMessage();
        
        // MSH
        _rdeMessage.MSH.SetHeader("PHARMACY_SYS", "PHARMACY_A", "EMR_SYSTEM", "HOSPITAL_A");
        _rdeMessage.MSH.SetMessageInfo("RDE", "O01", "RX456", "P", "2.3");
        
        // PID
        _rdeMessage.PID.SetPatientIdentifiers("54321", "MRN", "HOSPITAL");
        _rdeMessage.PID.SetPatientName(new PersonNameField("Johnson^Sarah^Elizabeth"));
        
        // ORC
        _rdeMessage.ORC.SetOrderControl("NW");
        _rdeMessage.ORC.SetOrderNumbers("ORD789", "PHARM456");
        
        // RXE
        _rdeMessage.RXE.SetMedicationInfo("123456789", "Lisinopril 10mg Tablet");
        _rdeMessage.RXE.SetQuantityAndRoute("30", "TAB", "PO");

        // Generate sample strings for parsing benchmarks
        _sampleAdtString = _adtMessage.ToHL7String();
        _sampleRdeString = _rdeMessage.ToHL7String();
    }

    [Benchmark(Baseline = true)]
    public void ADTMessage_ToHL7String()
    {
        var results = new string[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            results[i] = _adtMessage.ToHL7String();
        }
    }

    [Benchmark]
    public void ADTMessage_FromHL7String()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var message = new ADTMessage();
            message.FromHL7String(_sampleAdtString);
        }
    }

    [Benchmark]
    public void RDEMessage_ToHL7String()
    {
        var results = new string[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            results[i] = _rdeMessage.ToHL7String();
        }
    }

    [Benchmark]
    public void RDEMessage_FromHL7String()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var message = new RDEMessage();
            message.FromHL7String(_sampleRdeString);
        }
    }

    [Benchmark]
    public void Message_Validation()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var errors = _adtMessage.Validate();
        }
    }

    [Benchmark]
    public void Message_Clone()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var clone = _adtMessage.Clone();
        }
    }
}