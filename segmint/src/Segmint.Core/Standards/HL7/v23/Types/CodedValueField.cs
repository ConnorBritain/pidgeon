// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7.Validation;
using Segmint.Core.HL7;
using System.Collections.Generic;
using System.Linq;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 Coded Value (IS) field.
/// Used for coded values from user-defined tables.
/// </summary>
public class CodedValueField : HL7Field
{
    private readonly HashSet<string>? _allowedValues;
    private readonly int? _tableNumber;

    /// <inheritdoc />
    public override string DataType => "IS";

    /// <inheritdoc />
    public override int? MaxLength { get; }

    /// <summary>
    /// Gets the allowed values for this coded value field.
    /// </summary>
    public IReadOnlySet<string>? AllowedValues => _allowedValues;

    /// <summary>
    /// Gets the HL7 table number for this field.
    /// </summary>
    public int? TableNumber => _tableNumber;

    /// <summary>
    /// Gets or sets the value of this coded value field.
    /// </summary>
    public string Value
    {
        get => RawValue;
        set => SetValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodedValueField"/> class.
    /// </summary>
    /// <param name="value">The initial value.</param>
    /// <param name="allowedValues">The allowed values for this field.</param>
    /// <param name="tableNumber">The HL7 table number (optional).</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public CodedValueField(string? value = null, IEnumerable<string>? allowedValues = null, 
        int? tableNumber = null, int? maxLength = null, bool isRequired = false)
        : base(value, isRequired)
    {
        _allowedValues = allowedValues?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _tableNumber = tableNumber;
        MaxLength = maxLength;
    }

    /// <summary>
    /// Creates a coded value field for gender.
    /// </summary>
    /// <param name="value">The gender value (M, F, O, U).</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="CodedValueField"/> instance.</returns>
    public static CodedValueField CreateGender(string? value = null, bool isRequired = false)
    {
        return new CodedValueField(value, ["M", "F", "O", "U"], 1, 1, isRequired);
    }

    /// <summary>
    /// Creates a coded value field for marital status.
    /// </summary>
    /// <param name="value">The marital status value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="CodedValueField"/> instance.</returns>
    public static CodedValueField CreateMaritalStatus(string? value = null, bool isRequired = false)
    {
        return new CodedValueField(value, ["S", "M", "D", "W", "L", "A", "P"], 2, 1, isRequired);
    }

    /// <summary>
    /// Creates a coded value field for event type.
    /// </summary>
    /// <param name="value">The event type value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="CodedValueField"/> instance.</returns>
    public static CodedValueField CreateEventType(string? value = null, bool isRequired = false)
    {
        return new CodedValueField(value, ["A01", "A02", "A03", "A04", "A05", "A06", "A07", "A08", "A09", "A10", "A11", "A12", "A13", "A14", "A15", "A16", "A17", "A18", "A19", "A20", "A21", "A22", "A23", "A24", "A25", "A26", "A27", "A28", "A29", "A30", "A31", "A32", "A33", "A34", "A35", "A36", "A37", "A38", "A39", "A40", "A41", "A42", "A43", "A44", "A45", "A46", "A47", "A48", "A49", "A50", "A51", "A52", "A53", "A54", "A55", "A60", "A61", "A62"], 3, 3, isRequired);
    }

    /// <summary>
    /// Creates a coded value field for patient class.
    /// </summary>
    /// <param name="value">The patient class value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="CodedValueField"/> instance.</returns>
    public static CodedValueField CreatePatientClass(string? value = null, bool isRequired = false)
    {
        return new CodedValueField(value, ["E", "I", "O", "P", "R", "B", "N"], 4, 1, isRequired);
    }

    /// <summary>
    /// Creates a coded value field for admission type.
    /// </summary>
    /// <param name="value">The admission type value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="CodedValueField"/> instance.</returns>
    public static CodedValueField CreateAdmissionType(string? value = null, bool isRequired = false)
    {
        return new CodedValueField(value, ["A", "C", "E", "L", "N", "R", "T", "U"], 7, 1, isRequired);
    }

    /// <summary>
    /// Creates a coded value field for order control.
    /// </summary>
    /// <param name="value">The order control value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="CodedValueField"/> instance.</returns>
    public static CodedValueField CreateOrderControl(string? value = null, bool isRequired = false)
    {
        return new CodedValueField(value, ["NW", "OK", "UA", "CA", "DC", "DE", "DF", "DR", "FU", "HD", "HR", "LI", "NA", "NM", "PA", "RE", "RL", "RO", "RP", "RQ", "RR", "RU", "SN", "SR", "SS", "UD", "UF", "UN", "UR", "UX", "XO", "XR"], 119, 2, isRequired);
    }

    /// <summary>
    /// Creates a coded value field for order status.
    /// </summary>
    /// <param name="value">The order status value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="CodedValueField"/> instance.</returns>
    public static CodedValueField CreateOrderStatus(string? value = null, bool isRequired = false)
    {
        return new CodedValueField(value, ["A", "CA", "CM", "DC", "ER", "HD", "IP", "RP", "SC"], 38, 2, isRequired);
    }

    /// <summary>
    /// Creates a coded value field for priority.
    /// </summary>
    /// <param name="value">The priority value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="CodedValueField"/> instance.</returns>
    public static CodedValueField CreatePriority(string? value = null, bool isRequired = false)
    {
        return new CodedValueField(value, ["S", "A", "R", "P", "C", "T"], 27, 1, isRequired);
    }

    /// <summary>
    /// Creates a coded value field for yes/no indicator.
    /// </summary>
    /// <param name="value">The yes/no value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="CodedValueField"/> instance.</returns>
    public static CodedValueField CreateYesNoIndicator(string? value = null, bool isRequired = false)
    {
        return new CodedValueField(value, ["Y", "N"], 136, 1, isRequired);
    }

    /// <summary>
    /// Creates a coded value field for message type.
    /// </summary>
    /// <param name="value">The message type value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="CodedValueField"/> instance.</returns>
    public static CodedValueField CreateMessageType(string? value = null, bool isRequired = false)
    {
        return new CodedValueField(value, ["ACK", "ADT", "BAR", "DFT", "DSR", "MCF", "MDM", "MFN", "ORM", "ORR", "QRY", "RAS", "RDE", "RDS", "RER", "RGV", "RRA", "RRI", "UDM"], 76, 3, isRequired);
    }

    /// <inheritdoc />
    protected override void ValidateValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        if (_allowedValues != null && !_allowedValues.Contains(value))
        {
            var tableInfo = _tableNumber.HasValue ? $" (Table {_tableNumber})" : "";
            throw new ArgumentException($"Invalid coded value '{value}' for field{tableInfo}. " +
                                      $"Allowed values: {string.Join(", ", _allowedValues)}");
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
        return new CodedValueField(RawValue, _allowedValues, _tableNumber, MaxLength, IsRequired);
    }

    /// <summary>
    /// Implicitly converts a string to a CodedValueField.
    /// </summary>
    /// <param name="value">The string value.</param>
    public static implicit operator CodedValueField(string value)
    {
        return new CodedValueField(value);
    }

    /// <summary>
    /// Implicitly converts a CodedValueField to a string.
    /// </summary>
    /// <param name="field">The CodedValueField.</param>
    public static implicit operator string(CodedValueField field)
    {
        return field.RawValue;
    }

    /// <summary>
    /// Gets a formatted display string for this coded value.
    /// </summary>
    /// <returns>A human-readable representation of the coded value.</returns>
    public string ToDisplayString()
    {
        if (string.IsNullOrEmpty(RawValue))
            return string.Empty;

        var display = RawValue;
        
        if (_tableNumber.HasValue)
        {
            display = $"{display} (Table {_tableNumber})";
        }
        
        return display;
    }
}
