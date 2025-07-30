// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Types;
using System.Collections.Generic;
namespace Segmint.Core.Standards.HL7.v23.Segments;

/// <summary>
/// Represents the HL7 RXC (Pharmacy/Treatment Component Order) segment.
/// Contains information about components of a pharmaceutical order.
/// </summary>
public class RXCSegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "RXC";

    /// <summary>
    /// Initializes a new instance of the <see cref="RXCSegment"/> class.
    /// </summary>
    public RXCSegment()
    {
        InitializeFields();
    }

    /// <summary>
    /// Initializes the segment fields according to HL7 v2.3 specification.
    /// </summary>
    protected override void InitializeFields()
    {
        // RXC.1 - RX Component Type
        AddField(new CodedValueField(isRequired: true));
        
        // RXC.2 - Component Code
        AddField(new CodedElementField(isRequired: true));
        
        // RXC.3 - Component Amount
        AddField(new NumericField(isRequired: false));
        
        // RXC.4 - Component Units
        AddField(new CodedElementField(isRequired: false));
        
        // RXC.5 - Component Strength
        AddField(new NumericField(isRequired: false));
        
        // RXC.6 - Component Strength Units
        AddField(new CodedElementField(isRequired: false));
        
        // RXC.7 - Supplementary Code
        AddField(new CodedElementField(isRequired: false));
        
        // RXC.8 - Component Drug Strength Volume
        AddField(new NumericField(isRequired: false));
        
        // RXC.9 - Component Drug Strength Volume Units
        AddField(new CodedElementField(isRequired: false));
    }

    /// <summary>
    /// Gets or sets the RX component type.
    /// </summary>
    public CodedValueField RXComponentType
    {
        get => (CodedValueField)this[1];
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the component code.
    /// </summary>
    public CodedElementField ComponentCode
    {
        get => (CodedElementField)this[2];
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the component amount.
    /// </summary>
    public NumericField ComponentAmount
    {
        get => (NumericField)this[3];
        set => this[3] = value;
    }

    /// <summary>
    /// Gets or sets the component units.
    /// </summary>
    public CodedElementField ComponentUnits
    {
        get => (CodedElementField)this[4];
        set => this[4] = value;
    }

    /// <summary>
    /// Gets or sets the component strength.
    /// </summary>
    public NumericField ComponentStrength
    {
        get => (NumericField)this[5];
        set => this[5] = value;
    }

    /// <summary>
    /// Gets or sets the component strength units.
    /// </summary>
    public CodedElementField ComponentStrengthUnits
    {
        get => (CodedElementField)this[6];
        set => this[6] = value;
    }

    /// <summary>
    /// Gets or sets the supplementary code.
    /// </summary>
    public CodedElementField SupplementaryCode
    {
        get => (CodedElementField)this[7];
        set => this[7] = value;
    }

    /// <summary>
    /// Gets or sets the component drug strength volume.
    /// </summary>
    public NumericField ComponentDrugStrengthVolume
    {
        get => (NumericField)this[8];
        set => this[8] = value;
    }

    /// <summary>
    /// Gets or sets the component drug strength volume units.
    /// </summary>
    public CodedElementField ComponentDrugStrengthVolumeUnits
    {
        get => (CodedElementField)this[9];
        set => this[9] = value;
    }

    /// <summary>
    /// Creates a new RXC segment for a base component.
    /// </summary>
    /// <param name="componentCode">The component code.</param>
    /// <param name="componentText">The component description.</param>
    /// <param name="amount">The component amount.</param>
    /// <param name="units">The component units.</param>
    /// <returns>A new <see cref="RXCSegment"/> instance.</returns>
    public static RXCSegment CreateBaseComponent(string componentCode, string componentText, 
        decimal? amount = null, string? units = null)
    {
        var segment = new RXCSegment();
        segment.RXComponentType.SetValue("B"); // Base
        segment.ComponentCode = CodedElementField.Create(componentCode, componentText);
        
        if (amount.HasValue)
        {
            segment.ComponentAmount.SetValue(amount.Value.ToString());
        }
        
        if (!string.IsNullOrEmpty(units))
        {
            segment.ComponentUnits = CodedElementField.Create(units);
        }
        
        return segment;
    }

    /// <summary>
    /// Creates a new RXC segment for an additive component.
    /// </summary>
    /// <param name="componentCode">The component code.</param>
    /// <param name="componentText">The component description.</param>
    /// <param name="amount">The component amount.</param>
    /// <param name="units">The component units.</param>
    /// <returns>A new <see cref="RXCSegment"/> instance.</returns>
    public static RXCSegment CreateAdditiveComponent(string componentCode, string componentText, 
        decimal? amount = null, string? units = null)
    {
        var segment = new RXCSegment();
        segment.RXComponentType.SetValue("A"); // Additive
        segment.ComponentCode = CodedElementField.Create(componentCode, componentText);
        
        if (amount.HasValue)
        {
            segment.ComponentAmount.SetValue(amount.Value.ToString());
        }
        
        if (!string.IsNullOrEmpty(units))
        {
            segment.ComponentUnits = CodedElementField.Create(units);
        }
        
        return segment;
    }

    /// <summary>
    /// Creates a new RXC segment for a diluent component.
    /// </summary>
    /// <param name="componentCode">The component code.</param>
    /// <param name="componentText">The component description.</param>
    /// <param name="amount">The component amount.</param>
    /// <param name="units">The component units.</param>
    /// <returns>A new <see cref="RXCSegment"/> instance.</returns>
    public static RXCSegment CreateDiluentComponent(string componentCode, string componentText, 
        decimal? amount = null, string? units = null)
    {
        var segment = new RXCSegment();
        segment.RXComponentType.SetValue("D"); // Diluent
        segment.ComponentCode = CodedElementField.Create(componentCode, componentText);
        
        if (amount.HasValue)
        {
            segment.ComponentAmount.SetValue(amount.Value.ToString());
        }
        
        if (!string.IsNullOrEmpty(units))
        {
            segment.ComponentUnits = CodedElementField.Create(units);
        }
        
        return segment;
    }

    /// <summary>
    /// Sets the component type.
    /// </summary>
    /// <param name="componentType">The component type (B=Base, A=Additive, D=Diluent).</param>
    public void SetRXComponentType(string componentType)
    {
        RXComponentType.SetValue(componentType);
    }

    /// <summary>
    /// Sets the component code and description.
    /// </summary>
    /// <param name="code">The component code.</param>
    /// <param name="text">The component description.</param>
    /// <param name="codingSystem">The coding system (optional).</param>
    public void SetComponentCode(string code, string? text = null, string? codingSystem = null)
    {
        ComponentCode = CodedElementField.Create(code, text, codingSystem);
    }

    /// <summary>
    /// Sets the component amount and units.
    /// </summary>
    /// <param name="amount">The component amount.</param>
    /// <param name="units">The component units.</param>
    public void SetComponentAmount(decimal amount, string? units = null)
    {
        ComponentAmount.SetValue(amount.ToString());
        
        if (!string.IsNullOrEmpty(units))
        {
            ComponentUnits = CodedElementField.Create(units);
        }
    }

    /// <summary>
    /// Sets the component strength and units.
    /// </summary>
    /// <param name="strength">The component strength.</param>
    /// <param name="units">The strength units.</param>
    public void SetComponentStrength(decimal strength, string? units = null)
    {
        ComponentStrength.SetValue(strength.ToString());
        
        if (!string.IsNullOrEmpty(units))
        {
            ComponentStrengthUnits = CodedElementField.Create(units);
        }
    }

    /// <summary>
    /// Gets the component type display text.
    /// </summary>
    /// <returns>A human-readable description of the component type.</returns>
    public string GetComponentTypeDisplayText()
    {
        return RXComponentType.RawValue switch
        {
            "B" => "Base",
            "A" => "Additive",
            "D" => "Diluent",
            _ => RXComponentType.RawValue
        };
    }

    /// <summary>
    /// Gets a formatted display string for this component.
    /// </summary>
    /// <returns>A human-readable representation of the component.</returns>
    public string ToDisplayString()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(RXComponentType.RawValue))
        {
            parts.Add($"[{GetComponentTypeDisplayText()}]");
        }
        
        if (!string.IsNullOrEmpty(ComponentCode.RawValue))
        {
            parts.Add(ComponentCode.ToDisplayString());
        }
        
        if (!string.IsNullOrEmpty(ComponentAmount.RawValue))
        {
            var amount = ComponentAmount.RawValue;
            if (!string.IsNullOrEmpty(ComponentUnits.RawValue))
            {
                amount += $" {ComponentUnits.ToDisplayString()}";
            }
            parts.Add(amount);
        }
        
        if (!string.IsNullOrEmpty(ComponentStrength.RawValue))
        {
            var strength = ComponentStrength.RawValue;
            if (!string.IsNullOrEmpty(ComponentStrengthUnits.RawValue))
            {
                strength += $" {ComponentStrengthUnits.ToDisplayString()}";
            }
            parts.Add($"({strength})");
        }
        
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Creates a copy of this segment.
    /// </summary>
    /// <returns>A new instance with the same field values.</returns>
    public override HL7Segment Clone()
    {
        var cloned = new RXCSegment();
        for (var i = 1; i <= FieldCount; i++)
        {
            cloned[i] = this[i].Clone();
        }
        return cloned;
    }
}
