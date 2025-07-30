// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Performance;
using System.Globalization;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 Composite Quantity with Units (CQ) field.
/// Format: quantity^units
/// </summary>
public class CompositeQuantityField : HL7Field
{
    /// <inheritdoc />
    public override string DataType => "CQ";

    /// <inheritdoc />
    public override int? MaxLength => 60;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeQuantityField"/> class.
    /// </summary>
    /// <param name="value">The initial composite quantity value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public CompositeQuantityField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeQuantityField"/> class with quantity and units.
    /// </summary>
    /// <param name="quantity">The quantity value.</param>
    /// <param name="units">The units of measure.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public CompositeQuantityField(decimal quantity, string units, bool isRequired = false)
        : base(null, isRequired)
    {
        SetComponents(quantity, units);
    }

    /// <summary>
    /// Gets or sets the quantity component.
    /// </summary>
    public string Quantity
    {
        get => GetComponent(1);
        set => SetComponent(1, value);
    }

    /// <summary>
    /// Gets or sets the units component.
    /// </summary>
    public string Units
    {
        get => GetComponent(2);
        set => SetComponent(2, value);
    }

    /// <summary>
    /// Gets the quantity as a decimal value.
    /// </summary>
    /// <returns>The parsed decimal quantity, or null if invalid.</returns>
    public decimal? GetQuantityAsDecimal()
    {
        var quantityStr = Quantity;
        if (string.IsNullOrEmpty(quantityStr))
            return null;

        return decimal.TryParse(quantityStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) 
            ? result 
            : null;
    }

    /// <summary>
    /// Sets the quantity and units components.
    /// </summary>
    /// <param name="quantity">The quantity value.</param>
    /// <param name="units">The units of measure.</param>
    public void SetComponents(decimal quantity, string units)
    {
        SetComponents(quantity.ToString(CultureInfo.InvariantCulture), units);
    }

    /// <summary>
    /// Sets the quantity and units components.
    /// </summary>
    /// <param name="quantity">The quantity value as string.</param>
    /// <param name="units">The units of measure.</param>
    public void SetComponents(string? quantity, string? units)
    {
        var components = new[]
        {
            quantity ?? string.Empty,
            units ?? string.Empty
        };

        // Remove trailing empty components
        var lastNonEmpty = -1;
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(components[i]))
            {
                lastNonEmpty = i;
                break;
            }
        }

        if (lastNonEmpty == -1)
        {
            RawValue = string.Empty;
        }
        else
        {
            var result = new string[lastNonEmpty + 1];
            Array.Copy(components, result, lastNonEmpty + 1);
            RawValue = string.Join("^", result);
        }
    }

    /// <summary>
    /// Gets a component by its 1-based index.
    /// </summary>
    /// <param name="index">The 1-based component index (1-2).</param>
    /// <returns>The component value, or empty string if not present.</returns>
    public string GetComponent(int index)
    {
        if (index < 1 || index > 2)
            throw new ArgumentOutOfRangeException(nameof(index), "Component index must be between 1 and 2.");

        if (IsEmpty)
            return string.Empty;

        var components = ComponentCache.GetComponents(RawValue, '^');
        return index <= components.Length ? components[index - 1] : string.Empty;
    }

    /// <summary>
    /// Sets a component by its 1-based index.
    /// </summary>
    /// <param name="index">The 1-based component index (1-2).</param>
    /// <param name="value">The component value to set.</param>
    public void SetComponent(int index, string? value)
    {
        if (index < 1 || index > 2)
            throw new ArgumentOutOfRangeException(nameof(index), "Component index must be between 1 and 2.");

        var components = IsEmpty ? new string[2] : ComponentCache.GetComponents(RawValue, '^');
        
        // Expand array if necessary
        if (components.Length < 2)
        {
            var newComponents = new string[2];
            Array.Copy(components, newComponents, components.Length);
            for (int i = components.Length; i < 2; i++)
            {
                newComponents[i] = string.Empty;
            }
            components = newComponents;
        }

        components[index - 1] = value ?? string.Empty;

        // Remove trailing empty components
        var lastNonEmpty = -1;
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(components[i]))
            {
                lastNonEmpty = i;
                break;
            }
        }

        if (lastNonEmpty == -1)
        {
            RawValue = string.Empty;
        }
        else
        {
            var result = new string[lastNonEmpty + 1];
            Array.Copy(components, result, lastNonEmpty + 1);
            RawValue = string.Join("^", result);
        }
    }

    /// <inheritdoc />
    protected override void ValidateValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        // Check that we don't have more than 2 components
        var components = ComponentCache.GetComponents(value, '^');
        if (components.Length > 2)
        {
            throw new ArgumentException($"Composite quantity cannot have more than 2 components. Found {components.Length} components.");
        }

        // Validate that quantity is numeric if present
        if (components.Length >= 1 && !string.IsNullOrEmpty(components[0]))
        {
            if (!decimal.TryParse(components[0], NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            {
                throw new ArgumentException($"Quantity component must be numeric: '{components[0]}'");
            }
        }

        // Validate that components don't contain HL7 control characters
        foreach (var component in components)
        {
            if (component.Contains('|') || component.Contains('&') || component.Contains('~') || component.Contains('\\'))
            {
                throw new ArgumentException("Composite quantity components cannot contain HL7 control characters (|&~\\).");
            }
        }
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new CompositeQuantityField(RawValue, IsRequired);
    }

    /// <summary>
    /// Creates a composite quantity with the specified values.
    /// </summary>
    /// <param name="quantity">The quantity value.</param>
    /// <param name="units">The units of measure.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new CompositeQuantityField instance.</returns>
    public static CompositeQuantityField Create(decimal quantity, string units, bool isRequired = false)
    {
        return new CompositeQuantityField(quantity, units, isRequired);
    }

    /// <summary>
    /// Implicitly converts a string to a CompositeQuantityField.
    /// </summary>
    public static implicit operator CompositeQuantityField(string value)
    {
        return new CompositeQuantityField(value);
    }

    /// <summary>
    /// Implicitly converts a CompositeQuantityField to a string.
    /// </summary>
    public static implicit operator string(CompositeQuantityField field)
    {
        return field.RawValue;
    }
}
