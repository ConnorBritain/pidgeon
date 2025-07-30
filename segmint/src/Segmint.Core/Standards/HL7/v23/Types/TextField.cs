// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using System.Linq;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 Text (TX) field - variable length text data.
/// Maximum length of 65,536 characters.
/// </summary>
public class TextField : HL7Field
{
    /// <inheritdoc />
    public override string DataType => "TX";

    /// <inheritdoc />
    public override int? MaxLength => 65536;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextField"/> class.
    /// </summary>
    /// <param name="value">The initial text value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public TextField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
    }

    /// <summary>
    /// Gets the text value of this field.
    /// </summary>
    public string Value => RawValue;

    /// <inheritdoc />
    protected override void ValidateValue(string value)
    {
        // TX fields have minimal validation - just check for HL7 control characters
        if (value.Contains('|') || value.Contains('^') || value.Contains('&') || value.Contains('~') || value.Contains('\\'))
        {
            throw new ArgumentException("Text field cannot contain HL7 control characters (|^&~\\).");
        }
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new TextField(RawValue, IsRequired);
    }

    /// <summary>
    /// Implicitly converts a string to a TextField.
    /// </summary>
    public static implicit operator TextField(string value)
    {
        return new TextField(value);
    }

    /// <summary>
    /// Implicitly converts a TextField to a string.
    /// </summary>
    public static implicit operator string(TextField field)
    {
        return field.Value;
    }
}
