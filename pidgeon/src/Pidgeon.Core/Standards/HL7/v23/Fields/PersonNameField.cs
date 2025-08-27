// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;

namespace Pidgeon.Core.Standards.HL7.v23.Fields;

/// <summary>
/// Represents a person name field (XPN data type) in HL7 v2.3.
/// Format: Family^Given^Middle^Suffix^Prefix^Degree^Name Type Code
/// </summary>
public class PersonNameField : HL7Field<PersonName?>
{
    public PersonNameField() : base()
    {
    }

    public PersonNameField(PersonName? value) : base(value)
    {
    }

    public PersonNameField(string? stringValue) : base(stringValue)
    {
    }

    protected override Result<PersonName?> ParseStringValue(string stringValue)
    {
        if (string.IsNullOrWhiteSpace(stringValue))
            return Result<PersonName?>.Success(null);

        try
        {
            // Split on component separator (^)
            var components = stringValue.Split('^');
            
            // XPN fields: Family^Given^Middle^Suffix^Prefix^Degree^NameTypeCode
            var family = GetComponent(components, 0);
            var given = GetComponent(components, 1);
            var middle = GetComponent(components, 2);
            var suffix = GetComponent(components, 3);
            var prefix = GetComponent(components, 4);
            
            var personName = new PersonName
            {
                Family = family,
                Given = given,
                Middle = middle,
                Suffix = suffix,
                Prefix = prefix
            };

            return Result<PersonName?>.Success(personName);
        }
        catch (Exception ex)
        {
            return Error.Parsing($"Invalid person name format: {stringValue} - {ex.Message}", "PersonNameField");
        }
    }

    protected override string? FormatTypedValue(PersonName? value)
    {
        if (value == null)
            return null;

        var components = new[]
        {
            value.Family ?? "",
            value.Given ?? "",
            value.Middle ?? "",
            value.Suffix ?? "",
            value.Prefix ?? "",
            "", // Degree - not in our PersonName model yet
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
        var personName = new PersonName
        {
            Family = family,
            Given = given,
            Middle = middle,
            Prefix = prefix,
            Suffix = suffix
        };
        
        return new PersonNameField(personName);
    }
}