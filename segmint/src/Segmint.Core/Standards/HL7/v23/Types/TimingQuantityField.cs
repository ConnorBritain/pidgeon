// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7.Validation;
using Segmint.Core.HL7;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 Timing Quantity (TQ) field.
/// Used for medication dosing timing and frequency information.
/// </summary>
public partial class TimingQuantityField : HL7Field
{
    [GeneratedRegex(@"^[0-9]+$")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"^[0-9]{8}$")]
    private static partial Regex TimeRegex();

    /// <inheritdoc />
    public override string DataType => "TQ";

    /// <inheritdoc />
    public override int? MaxLength => 200;

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public string? Quantity { get; set; }

    /// <summary>
    /// Gets or sets the interval.
    /// </summary>
    public string? Interval { get; set; }

    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    public string? Duration { get; set; }

    /// <summary>
    /// Gets or sets the start date/time.
    /// </summary>
    public string? StartDateTime { get; set; }

    /// <summary>
    /// Gets or sets the end date/time.
    /// </summary>
    public string? EndDateTime { get; set; }

    /// <summary>
    /// Gets or sets the priority.
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Gets or sets the condition.
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the conjunction.
    /// </summary>
    public string? Conjunction { get; set; }

    /// <summary>
    /// Gets or sets the order sequencing.
    /// </summary>
    public string? OrderSequencing { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimingQuantityField"/> class.
    /// </summary>
    /// <param name="value">The initial HL7 formatted value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public TimingQuantityField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
        if (!string.IsNullOrEmpty(value))
        {
            ParseFromHL7String(value);
        }
    }

    /// <summary>
    /// Creates a timing quantity field for daily dosing.
    /// </summary>
    /// <param name="quantity">The quantity per dose.</param>
    /// <param name="timesPerDay">The number of times per day.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="TimingQuantityField"/> instance.</returns>
    public static TimingQuantityField CreateDaily(string quantity, int timesPerDay, bool isRequired = false)
    {
        var field = new TimingQuantityField(isRequired: isRequired)
        {
            Quantity = quantity,
            Interval = $"D{timesPerDay}",
            Text = $"{quantity} {timesPerDay} times daily"
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates a timing quantity field for hourly dosing.
    /// </summary>
    /// <param name="quantity">The quantity per dose.</param>
    /// <param name="hoursInterval">The interval in hours.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="TimingQuantityField"/> instance.</returns>
    public static TimingQuantityField CreateHourly(string quantity, int hoursInterval, bool isRequired = false)
    {
        var field = new TimingQuantityField(isRequired: isRequired)
        {
            Quantity = quantity,
            Interval = $"H{hoursInterval}",
            Text = $"{quantity} every {hoursInterval} hours"
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates a timing quantity field for PRN (as needed) dosing.
    /// </summary>
    /// <param name="quantity">The quantity per dose.</param>
    /// <param name="condition">The condition for administration.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="TimingQuantityField"/> instance.</returns>
    public static TimingQuantityField CreatePRN(string quantity, string condition, bool isRequired = false)
    {
        var field = new TimingQuantityField(isRequired: isRequired)
        {
            Quantity = quantity,
            Priority = "PRN",
            Condition = condition,
            Text = $"{quantity} PRN {condition}"
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates a timing quantity field for once-daily dosing.
    /// </summary>
    /// <param name="quantity">The quantity per dose.</param>
    /// <param name="time">The time of day (optional).</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="TimingQuantityField"/> instance.</returns>
    public static TimingQuantityField CreateOnceDaily(string quantity, string? time = null, bool isRequired = false)
    {
        var field = new TimingQuantityField(isRequired: isRequired)
        {
            Quantity = quantity,
            Interval = "D1",
            Text = $"{quantity} once daily"
        };

        if (!string.IsNullOrEmpty(time))
        {
            field.StartDateTime = time;
            field.Text = $"{quantity} once daily at {time}";
        }
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates a timing quantity field for BID (twice daily) dosing.
    /// </summary>
    /// <param name="quantity">The quantity per dose.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="TimingQuantityField"/> instance.</returns>
    public static TimingQuantityField CreateBID(string quantity, bool isRequired = false)
    {
        var field = new TimingQuantityField(isRequired: isRequired)
        {
            Quantity = quantity,
            Interval = "D2",
            Text = $"{quantity} twice daily"
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates a timing quantity field for TID (three times daily) dosing.
    /// </summary>
    /// <param name="quantity">The quantity per dose.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="TimingQuantityField"/> instance.</returns>
    public static TimingQuantityField CreateTID(string quantity, bool isRequired = false)
    {
        var field = new TimingQuantityField(isRequired: isRequired)
        {
            Quantity = quantity,
            Interval = "D3",
            Text = $"{quantity} three times daily"
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates a timing quantity field for QID (four times daily) dosing.
    /// </summary>
    /// <param name="quantity">The quantity per dose.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="TimingQuantityField"/> instance.</returns>
    public static TimingQuantityField CreateQID(string quantity, bool isRequired = false)
    {
        var field = new TimingQuantityField(isRequired: isRequired)
        {
            Quantity = quantity,
            Interval = "D4",
            Text = $"{quantity} four times daily"
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Parses the components from an HL7 formatted string.
    /// </summary>
    /// <param name="value">The HL7 formatted string.</param>
    private void ParseFromHL7String(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var components = value.Split('^');
        
        if (components.Length >= 1) Quantity = components[0];
        if (components.Length >= 2) Interval = components[1];
        if (components.Length >= 3) Duration = components[2];
        if (components.Length >= 4) StartDateTime = components[3];
        if (components.Length >= 5) EndDateTime = components[4];
        if (components.Length >= 6) Priority = components[5];
        if (components.Length >= 7) Condition = components[6];
        if (components.Length >= 8) Text = components[7];
        if (components.Length >= 9) Conjunction = components[8];
        if (components.Length >= 10) OrderSequencing = components[9];
    }

    /// <summary>
    /// Updates the raw HL7 value from the component values.
    /// </summary>
    private void UpdateRawValue()
    {
        var components = new[]
        {
            Quantity ?? "",
            Interval ?? "",
            Duration ?? "",
            StartDateTime ?? "",
            EndDateTime ?? "",
            Priority ?? "",
            Condition ?? "",
            Text ?? "",
            Conjunction ?? "",
            OrderSequencing ?? ""
        };

        // Find the last non-empty component
        var lastNonEmpty = -1;
        for (var i = components.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(components[i]))
            {
                lastNonEmpty = i;
                break;
            }
        }

        if (lastNonEmpty >= 0)
        {
            RawValue = string.Join("^", components.Take(lastNonEmpty + 1));
        }
        else
        {
            RawValue = string.Empty;
        }
    }

    /// <inheritdoc />
    protected override void ValidateValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var components = value.Split('^');
        
        // Validate quantity if present
        if (components.Length >= 1 && !string.IsNullOrEmpty(components[0]))
        {
            if (!NumberRegex().IsMatch(components[0]))
            {
                throw new ArgumentException($"Invalid quantity format: {components[0]}");
            }
        }

        // Validate interval if present
        if (components.Length >= 2 && !string.IsNullOrEmpty(components[1]))
        {
            if (!Regex.IsMatch(components[1], @"^[DH]\d+$"))
            {
                throw new ArgumentException($"Invalid interval format: {components[1]}");
            }
        }

        // Validate priority if present
        if (components.Length >= 6 && !string.IsNullOrEmpty(components[5]))
        {
            var validPriorities = new[] { "S", "A", "R", "P", "C", "T", "PRN" };
            if (!validPriorities.Contains(components[5]))
            {
                throw new ArgumentException($"Invalid priority: {components[5]}");
            }
        }
    }

    /// <inheritdoc />
    public override string ToHL7String()
    {
        return RawValue;
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new TimingQuantityField(RawValue, IsRequired);
    }

    /// <summary>
    /// Implicitly converts a string to a TimingQuantityField.
    /// </summary>
    /// <param name="value">The string value.</param>
    public static implicit operator TimingQuantityField(string value)
    {
        return new TimingQuantityField(value);
    }

    /// <summary>
    /// Implicitly converts a TimingQuantityField to a string.
    /// </summary>
    /// <param name="field">The TimingQuantityField.</param>
    public static implicit operator string(TimingQuantityField field)
    {
        return field.RawValue;
    }

    /// <summary>
    /// Gets a formatted display string for this timing quantity.
    /// </summary>
    /// <returns>A human-readable representation of the timing quantity.</returns>
    public string ToDisplayString()
    {
        if (!string.IsNullOrEmpty(Text))
        {
            return Text;
        }

        if (!string.IsNullOrEmpty(Quantity) && !string.IsNullOrEmpty(Interval))
        {
            return $"{Quantity} {Interval}";
        }

        return RawValue;
    }
}
