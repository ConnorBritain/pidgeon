// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace Segmint.Core.DataGeneration;

/// <summary>
/// Interface for synthetic data generators.
/// </summary>
/// <typeparam name="T">The type of data to generate.</typeparam>
public interface IDataGenerator<T>
{
    /// <summary>
    /// Generates a single instance of synthetic data.
    /// </summary>
    /// <returns>A single instance of synthetic data.</returns>
    T Generate();

    /// <summary>
    /// Generates multiple instances of synthetic data.
    /// </summary>
    /// <param name="count">The number of instances to generate.</param>
    /// <returns>A collection of synthetic data instances.</returns>
    IEnumerable<T> Generate(int count);

    /// <summary>
    /// Generates synthetic data with specific constraints.
    /// </summary>
    /// <param name="constraints">Constraints to apply during generation.</param>
    /// <returns>A single instance of synthetic data that meets the constraints.</returns>
    T Generate(DataGenerationConstraints constraints);

    /// <summary>
    /// Generates multiple instances of synthetic data with constraints.
    /// </summary>
    /// <param name="count">The number of instances to generate.</param>
    /// <param name="constraints">Constraints to apply during generation.</param>
    /// <returns>A collection of synthetic data instances that meet the constraints.</returns>
    IEnumerable<T> Generate(int count, DataGenerationConstraints constraints);
}

/// <summary>
/// Constraints for data generation.
/// </summary>
public class DataGenerationConstraints
{
    /// <summary>
    /// Random seed for reproducible generation.
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// Locale for localized data generation.
    /// </summary>
    public string Locale { get; set; } = "en-US";

    /// <summary>
    /// Date range constraints.
    /// </summary>
    public DateRange? DateRange { get; set; }

    /// <summary>
    /// Numeric range constraints.
    /// </summary>
    public NumericRange? NumericRange { get; set; }

    /// <summary>
    /// String length constraints.
    /// </summary>
    public StringLengthRange? StringLengthRange { get; set; }

    /// <summary>
    /// Custom constraints as key-value pairs.
    /// </summary>
    public Dictionary<string, object> CustomConstraints { get; set; } = new();

    /// <summary>
    /// Whether to include sensitive data in generation.
    /// </summary>
    public bool IncludeSensitiveData { get; set; } = true;

    /// <summary>
    /// Quality level for generated data (e.g., "realistic", "basic", "test").
    /// </summary>
    public string QualityLevel { get; set; } = "realistic";
}

/// <summary>
/// Date range constraint.
/// </summary>
public class DateRange
{
    /// <summary>
    /// Start date (inclusive).
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// End date (inclusive).
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// Creates a date range.
    /// </summary>
    /// <param name="start">Start date.</param>
    /// <param name="end">End date.</param>
    public DateRange(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates a date range for the past N years.
    /// </summary>
    /// <param name="years">Number of years in the past.</param>
    /// <returns>Date range from N years ago to now.</returns>
    public static DateRange PastYears(int years)
    {
        var now = DateTime.Now;
        return new DateRange(now.AddYears(-years), now);
    }

    /// <summary>
    /// Creates a date range for adults (18-80 years old).
    /// </summary>
    /// <returns>Date range for adult birth dates.</returns>
    public static DateRange AdultBirthDates()
    {
        var now = DateTime.Now;
        return new DateRange(now.AddYears(-80), now.AddYears(-18));
    }
}

/// <summary>
/// Numeric range constraint.
/// </summary>
public class NumericRange
{
    /// <summary>
    /// Minimum value (inclusive).
    /// </summary>
    public decimal Min { get; set; }

    /// <summary>
    /// Maximum value (inclusive).
    /// </summary>
    public decimal Max { get; set; }

    /// <summary>
    /// Number of decimal places.
    /// </summary>
    public int DecimalPlaces { get; set; } = 0;

    /// <summary>
    /// Creates a numeric range.
    /// </summary>
    /// <param name="min">Minimum value.</param>
    /// <param name="max">Maximum value.</param>
    /// <param name="decimalPlaces">Number of decimal places.</param>
    public NumericRange(decimal min, decimal max, int decimalPlaces = 0)
    {
        Min = min;
        Max = max;
        DecimalPlaces = decimalPlaces;
    }
}

/// <summary>
/// String length range constraint.
/// </summary>
public class StringLengthRange
{
    /// <summary>
    /// Minimum length (inclusive).
    /// </summary>
    public int Min { get; set; }

    /// <summary>
    /// Maximum length (inclusive).
    /// </summary>
    public int Max { get; set; }

    /// <summary>
    /// Creates a string length range.
    /// </summary>
    /// <param name="min">Minimum length.</param>
    /// <param name="max">Maximum length.</param>
    public StringLengthRange(int min, int max)
    {
        Min = min;
        Max = max;
    }
}