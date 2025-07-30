// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Segmint.Core.Standards.HL7.v23.Types;

namespace Segmint.Benchmarks;

/// <summary>
/// Benchmarks for HL7 field operations to identify performance bottlenecks.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MarkdownExporter]
public class FieldOperationsBenchmarks
{
    private StringField _stringField = null!;
    private PersonNameField _personNameField = null!;
    private CompositeQuantityField _compositeQuantityField = null!;
    private TimestampField _timestampField = null!;
    private const int IterationCount = 1000;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _stringField = new StringField();
        _personNameField = new PersonNameField();
        _compositeQuantityField = new CompositeQuantityField();
        _timestampField = new TimestampField();
    }

    [Benchmark(Baseline = true)]
    public void StringField_SetValue()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            _stringField.SetValue($"TestValue{i}");
        }
    }

    [Benchmark]
    public void StringField_Clone()
    {
        _stringField.SetValue("TestValue");
        for (int i = 0; i < IterationCount; i++)
        {
            var clone = _stringField.Clone();
        }
    }

    [Benchmark]
    public void PersonNameField_SetComponents()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            _personNameField.SetComponents($"Family{i}", $"Given{i}", $"Middle{i}");
        }
    }

    [Benchmark]
    public void PersonNameField_FromFormattedString()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            _personNameField.RawValue = $"Family{i}^Given{i}^Middle{i}";
        }
    }

    [Benchmark]
    public void CompositeQuantityField_SetComponents_Decimal()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            _compositeQuantityField.SetComponents(100.25m + i, "MG");
        }
    }

    [Benchmark]
    public void CompositeQuantityField_SetComponents_String()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            _compositeQuantityField.SetComponents($"{100 + i}", "MG");
        }
    }

    [Benchmark]
    public void TimestampField_SetValue_DateTime()
    {
        var dateTime = DateTime.Now;
        for (int i = 0; i < IterationCount; i++)
        {
            _timestampField.SetValue(dateTime.AddDays(i));
        }
    }

    [Benchmark]
    public void TimestampField_SetValue_String()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            _timestampField.SetValue($"202507{16 + (i % 14):D2}120000");
        }
    }

    [Benchmark]
    public void Field_ToHL7String_Simple()
    {
        _stringField.SetValue("TestValue");
        var results = new string[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            results[i] = _stringField.ToHL7String();
        }
    }

    [Benchmark]
    public void Field_ToHL7String_Complex()
    {
        _personNameField.SetComponents("Family", "Given", "Middle");
        var results = new string[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            results[i] = _personNameField.ToHL7String();
        }
    }
}