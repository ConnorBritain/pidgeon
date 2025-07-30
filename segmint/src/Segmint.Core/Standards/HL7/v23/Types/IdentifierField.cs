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
/// Represents an HL7 Identifier (ID) field - coded values from a specific table.
/// </summary>
public partial class IdentifierField : HL7Field
{
    private readonly HashSet<string>? _allowedValues;

    /// <inheritdoc />
    public override string DataType => "ID";

    /// <inheritdoc />
    public override int? MaxLength { get; }

    /// <summary>
    /// Gets the allowed values for this identifier field.
    /// </summary>
    public IReadOnlySet<string>? AllowedValues => _allowedValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentifierField"/> class.
    /// </summary>
    /// <param name="value">The initial value.</param>
    /// <param name="allowedValues">The set of allowed values for this identifier.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public IdentifierField(string? value = null, IEnumerable<string>? allowedValues = null, 
        int? maxLength = null, bool isRequired = false)
        : base(value, isRequired)
    {
        _allowedValues = allowedValues?.ToHashSet();
        MaxLength = maxLength;
        
        if (!string.IsNullOrEmpty(value))
        {
            ValidateValue(value);
        }
    }

    /// <summary>
    /// Gets the identifier value of this field.
    /// </summary>
    public string Value => RawValue;

    /// <inheritdoc />
    protected override void ValidateValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        // ID fields should only contain alphanumeric characters and limited punctuation
        if (!AlphaNumericPattern().IsMatch(value))
        {
            throw new ArgumentException("Identifier field can only contain alphanumeric characters, hyphens, and underscores.");
        }

        // Check against allowed values if specified
        if (_allowedValues != null && !_allowedValues.Contains(value))
        {
            throw new ArgumentException($"Value '{value}' is not in the allowed set: {string.Join(", ", _allowedValues)}");
        }
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new IdentifierField(RawValue, _allowedValues, MaxLength, IsRequired);
    }

    /// <summary>
    /// Creates an identifier field with common HL7 Yes/No values.
    /// </summary>
    /// <param name="value">The initial value (Y or N).</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>An identifier field configured for Yes/No values.</returns>
    public static IdentifierField YesNo(string? value = null, bool isRequired = false)
    {
        return new IdentifierField(value, new[] { "Y", "N" }, 1, isRequired);
    }

    /// <summary>
    /// Creates an identifier field with common HL7 gender values.
    /// </summary>
    /// <param name="value">The initial value (F, M, O, U).</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>An identifier field configured for gender values.</returns>
    public static IdentifierField Gender(string? value = null, bool isRequired = false)
    {
        return new IdentifierField(value, new[] { "F", "M", "O", "U" }, 1, isRequired);
    }

    /// <summary>
    /// Regex pattern for valid identifier characters.
    /// </summary>
    [GeneratedRegex(@"^[A-Za-z0-9_-]+$")]
    private static partial Regex AlphaNumericPattern();

    /// <summary>
    /// Implicitly converts a string to an IdentifierField.
    /// </summary>
    public static implicit operator IdentifierField(string value)
    {
        return new IdentifierField(value);
    }

    /// <summary>
    /// Implicitly converts an IdentifierField to a string.
    /// </summary>
    public static implicit operator string(IdentifierField field)
    {
        return field.Value;
    }
}
