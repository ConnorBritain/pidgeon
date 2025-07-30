// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Segmint.Core.Performance;

/// <summary>
/// Performance metrics collection for monitoring HL7 operations.
/// </summary>
public static class PerformanceMetrics
{
    private static readonly ConcurrentDictionary<string, OperationMetrics> Metrics = new();
    private static readonly object StatsLock = new object();
    private static long _totalOperations = 0;
    private static long _totalTimeMs = 0;

    /// <summary>
    /// Measures the execution time of an operation.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="operation">The operation to measure.</param>
    /// <param name="operationName">The name of the operation (auto-generated if null).</param>
    /// <returns>The result of the operation.</returns>
    public static T Measure<T>(Func<T> operation, [CallerMemberName] string? operationName = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = operation();
            RecordMetric(operationName ?? "Unknown", stopwatch.ElapsedMilliseconds);
            return result;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Measures the execution time of an operation.
    /// </summary>
    /// <param name="operation">The operation to measure.</param>
    /// <param name="operationName">The name of the operation (auto-generated if null).</param>
    public static void Measure(Action operation, [CallerMemberName] string? operationName = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            operation();
        }
        finally
        {
            stopwatch.Stop();
            RecordMetric(operationName ?? "Unknown", stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Records a metric for the specified operation.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="elapsedMs">The elapsed time in milliseconds.</param>
    public static void RecordMetric(string operationName, long elapsedMs)
    {
        Metrics.AddOrUpdate(operationName,
            new OperationMetrics(operationName, elapsedMs),
            (key, existing) => existing.AddSample(elapsedMs));

        lock (StatsLock)
        {
            _totalOperations++;
            _totalTimeMs += elapsedMs;
        }
    }

    /// <summary>
    /// Gets metrics for a specific operation.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <returns>The metrics for the operation, or null if not found.</returns>
    public static OperationMetrics? GetMetrics(string operationName)
    {
        return Metrics.TryGetValue(operationName, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Gets all recorded metrics.
    /// </summary>
    /// <returns>A snapshot of all metrics.</returns>
    public static OperationMetrics[] GetAllMetrics()
    {
        var result = new OperationMetrics[Metrics.Count];
        var index = 0;
        foreach (var kvp in Metrics)
        {
            result[index++] = kvp.Value;
        }
        return result;
    }

    /// <summary>
    /// Gets overall performance statistics.
    /// </summary>
    /// <returns>Overall performance stats.</returns>
    public static OverallStats GetOverallStats()
    {
        lock (StatsLock)
        {
            return new OverallStats
            {
                TotalOperations = _totalOperations,
                TotalTimeMs = _totalTimeMs,
                AverageTimeMs = _totalOperations > 0 ? (double)_totalTimeMs / _totalOperations : 0,
                OperationsPerSecond = _totalTimeMs > 0 ? _totalOperations * 1000.0 / _totalTimeMs : 0
            };
        }
    }

    /// <summary>
    /// Clears all metrics (useful for testing).
    /// </summary>
    public static void Clear()
    {
        Metrics.Clear();
        lock (StatsLock)
        {
            _totalOperations = 0;
            _totalTimeMs = 0;
        }
    }
}

/// <summary>
/// Metrics for a specific operation.
/// </summary>
public class OperationMetrics
{
    private readonly object _lock = new object();
    private long _count = 0;
    private long _totalMs = 0;
    private long _minMs = long.MaxValue;
    private long _maxMs = 0;

    public string OperationName { get; }
    public long Count => _count;
    public long TotalMs => _totalMs;
    public long MinMs => _minMs == long.MaxValue ? 0 : _minMs;
    public long MaxMs => _maxMs;
    public double AverageMs => _count > 0 ? (double)_totalMs / _count : 0;

    internal OperationMetrics(string operationName, long initialMs)
    {
        OperationName = operationName;
        AddSample(initialMs);
    }

    internal OperationMetrics AddSample(long elapsedMs)
    {
        lock (_lock)
        {
            _count++;
            _totalMs += elapsedMs;
            _minMs = Math.Min(_minMs, elapsedMs);
            _maxMs = Math.Max(_maxMs, elapsedMs);
        }
        return this;
    }

    public override string ToString()
    {
        return $"{OperationName}: {Count} calls, {AverageMs:F2}ms avg, {MinMs}ms min, {MaxMs}ms max";
    }
}

/// <summary>
/// Overall performance statistics.
/// </summary>
public class OverallStats
{
    public long TotalOperations { get; init; }
    public long TotalTimeMs { get; init; }
    public double AverageTimeMs { get; init; }
    public double OperationsPerSecond { get; init; }

    public override string ToString()
    {
        return $"Total: {TotalOperations} ops in {TotalTimeMs}ms, {AverageTimeMs:F2}ms avg, {OperationsPerSecond:F1} ops/sec";
    }
}