// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Application.DTOs;
using Pidgeon.Core.Domain.Messaging.HL7v2.Common;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.DataTypes;

/// <summary>
/// Represents a person name field (XPN data type) in HL7 v2.3.
/// Format: Family^Given^Middle^Suffix^Prefix^Degree^Name Type Code
/// </summary>
public class PersonNameField : HL7Field<PersonNameDto?>
{
    /// <inheritdoc />
    public override string DataType => "XPN";

    public PersonNameField()
    {
    }

    public PersonNameField(PersonNameDto? value)
    {
        Value = value;
        RawValue = FormatValue(value);
    }

    public PersonNameField(string? stringValue)
    {
        var parseResult = ParseFromHL7String(stringValue ?? "");
        if (parseResult.IsSuccess)
        {
            Value = parseResult.Value;
            RawValue = stringValue ?? "";
        }
    }

    protected override Result<PersonNameDto?> ParseFromHL7String(string hl7Value)
    {
        if (string.IsNullOrWhiteSpace(hl7Value))
            return Result<PersonNameDto?>.Success(null);

        try
        {
            // Split on component separator (^)
            var components = hl7Value.Split('^');
            
            // XPN fields: Family^Given^Middle^Suffix^Prefix^Degree^NameTypeCode
            var family = components.Length > 0 ? components[0] : null;
            var given = components.Length > 1 ? components[1] : null;
            var middle = components.Length > 2 ? components[2] : null;
            var suffix = components.Length > 3 ? components[3] : null;
            var prefix = components.Length > 4 ? components[4] : null;
            
            var personName = new PersonNameDto
            {
                LastName = family ?? "",
                FirstName = given ?? "",
                MiddleName = middle,
                Suffix = suffix,
                Prefix = prefix
            };

            return Result<PersonNameDto?>.Success(personName);
        }
        catch (Exception ex)
        {
            return Result<PersonNameDto?>.Failure($"Invalid person name format: {hl7Value} - {ex.Message}");
        }
    }

    protected override string FormatValue(PersonNameDto? value)
    {
        if (value == null)
            return "";

        var components = new[]
        {
            value.LastName ?? "",
            value.FirstName ?? "",
            value.MiddleName ?? "",
            value.Suffix ?? "",
            value.Prefix ?? "",
            "", // Degree - not in DTO
            ""  // Name Type Code - typically empty
        };

        // Trim trailing empty components
        var lastNonEmptyIndex = -1;
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(components[i]))
            {
                lastNonEmptyIndex = i;
                break;
            }
        }

        if (lastNonEmptyIndex == -1)
            return "";

        return string.Join("^", components.Take(lastNonEmptyIndex + 1));
    }

    /// <summary>
    /// Gets a component from the split array, returning null if index is out of bounds or empty.
    /// </summary>
    private static string? GetComponent(string[] components, int index)
    {
        if (index >= components.Length)
            return null;
            
        var component = components[index]?.Trim();
        return string.IsNullOrEmpty(component) ? null : component;
    }

    /// <summary>
    /// Creates a PersonNameField from separate name components.
    /// </summary>
    public static PersonNameField Create(string? family, string? given, string? middle = null, 
        string? prefix = null, string? suffix = null)
    {
        var personName = new PersonNameDto
        {
            LastName = family ?? "",
            FirstName = given ?? "",
            MiddleName = middle,
            Prefix = prefix,
            Suffix = suffix
        };
        
        return new PersonNameField(personName);
    }
}