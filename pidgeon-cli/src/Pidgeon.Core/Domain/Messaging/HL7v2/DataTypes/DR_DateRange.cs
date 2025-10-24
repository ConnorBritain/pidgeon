// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Messaging.HL7v2.DataTypes;

/// <summary>
/// DR - Date Range
/// Used to specify a range of dates.
/// Components: RangeStartDateTime^RangeEndDateTime
/// </summary>
public record DR_DateRange
{
    /// <summary>
    /// DR.1 - Range Start Date/Time (TS)
    /// The start date/time of the range.
    /// </summary>
    public DateTime? RangeStartDateTime { get; init; }

    /// <summary>
    /// DR.2 - Range End Date/Time (TS)
    /// The end date/time of the range.
    /// </summary>
    public DateTime? RangeEndDateTime { get; init; }

    /// <summary>
    /// Creates a date range with start and end dates.
    /// </summary>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <returns>A DR instance</returns>
    public static DR_DateRange Create(DateTime startDate, DateTime endDate)
    {
        return new DR_DateRange
        {
            RangeStartDateTime = startDate,
            RangeEndDateTime = endDate
        };
    }

    /// <summary>
    /// Creates a date range starting from a specific date with no end.
    /// </summary>
    /// <param name="startDate">The start date</param>
    /// <returns>A DR instance</returns>
    public static DR_DateRange StartingFrom(DateTime startDate)
    {
        return new DR_DateRange { RangeStartDateTime = startDate };
    }

    /// <summary>
    /// Creates a date range ending at a specific date with no start.
    /// </summary>
    /// <param name="endDate">The end date</param>
    /// <returns>A DR instance</returns>
    public static DR_DateRange EndingAt(DateTime endDate)
    {
        return new DR_DateRange { RangeEndDateTime = endDate };
    }

    /// <summary>
    /// Determines if this DR is effectively empty (no dates specified).
    /// </summary>
    public bool IsEmpty => !RangeStartDateTime.HasValue && !RangeEndDateTime.HasValue;

    /// <summary>
    /// Gets the display value of the date range.
    /// </summary>
    public string DisplayValue
    {
        get
        {
            if (IsEmpty) return "Unknown";

            if (RangeStartDateTime.HasValue && RangeEndDateTime.HasValue)
                return $"{RangeStartDateTime.Value:yyyy-MM-dd} to {RangeEndDateTime.Value:yyyy-MM-dd}";
            
            if (RangeStartDateTime.HasValue)
                return $"From {RangeStartDateTime.Value:yyyy-MM-dd}";
            
            if (RangeEndDateTime.HasValue)
                return $"Until {RangeEndDateTime.Value:yyyy-MM-dd}";

            return "Unknown";
        }
    }

    /// <summary>
    /// Checks if a given date falls within this range.
    /// </summary>
    /// <param name="date">The date to check</param>
    /// <returns>True if the date is within the range</returns>
    public bool Contains(DateTime date)
    {
        if (RangeStartDateTime.HasValue && date < RangeStartDateTime.Value)
            return false;

        if (RangeEndDateTime.HasValue && date > RangeEndDateTime.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if this range overlaps with another range.
    /// </summary>
    /// <param name="other">The other date range</param>
    /// <returns>True if the ranges overlap</returns>
    public bool OverlapsWith(DR_DateRange other)
    {
        if (other == null || IsEmpty || other.IsEmpty)
            return false;

        // If either range has no end, they potentially overlap if starts are compatible
        if (!RangeEndDateTime.HasValue || !other.RangeEndDateTime.HasValue)
        {
            if (!RangeStartDateTime.HasValue || !other.RangeStartDateTime.HasValue)
                return true;
            
            // Check if one starts before the other ends
            return RangeStartDateTime.Value <= (other.RangeEndDateTime ?? DateTime.MaxValue) &&
                   other.RangeStartDateTime.Value <= (RangeEndDateTime ?? DateTime.MaxValue);
        }

        // Both ranges have ends, check for overlap
        var thisStart = RangeStartDateTime ?? DateTime.MinValue;
        var thisEnd = RangeEndDateTime.Value;
        var otherStart = other.RangeStartDateTime ?? DateTime.MinValue;
        var otherEnd = other.RangeEndDateTime.Value;

        return thisStart <= otherEnd && otherStart <= thisEnd;
    }

    /// <summary>
    /// Gets the duration of this date range.
    /// </summary>
    public TimeSpan? Duration
    {
        get
        {
            if (!RangeStartDateTime.HasValue || !RangeEndDateTime.HasValue)
                return null;

            return RangeEndDateTime.Value - RangeStartDateTime.Value;
        }
    }

    /// <summary>
    /// Validates the date range structure and business rules.
    /// </summary>
    public Result<DR_DateRange> Validate()
    {
        if (IsEmpty)
            return Error.Validation("At least start date or end date must be provided", nameof(RangeStartDateTime));

        if (RangeStartDateTime.HasValue && RangeEndDateTime.HasValue && RangeStartDateTime.Value > RangeEndDateTime.Value)
            return Error.Validation("Start date cannot be after end date", nameof(RangeStartDateTime));

        return Result<DR_DateRange>.Success(this);
    }

    /// <summary>
    /// Creates common date ranges for healthcare scenarios.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Current year date range.
        /// </summary>
        public static DR_DateRange CurrentYear => Create(
            new DateTime(DateTime.Now.Year, 1, 1),
            new DateTime(DateTime.Now.Year, 12, 31));

        /// <summary>
        /// Current month date range.
        /// </summary>
        public static DR_DateRange CurrentMonth => Create(
            new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)));

        /// <summary>
        /// Last 30 days date range.
        /// </summary>
        public static DR_DateRange Last30Days => Create(DateTime.Today.AddDays(-30), DateTime.Today);

        /// <summary>
        /// Last 7 days date range.
        /// </summary>
        public static DR_DateRange LastWeek => Create(DateTime.Today.AddDays(-7), DateTime.Today);

        /// <summary>
        /// Today only.
        /// </summary>
        public static DR_DateRange Today => Create(DateTime.Today, DateTime.Today);
    }
}