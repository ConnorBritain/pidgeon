// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Performance;
using System.Collections.Generic;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 Extended Person Name (XPN) field.
/// Format: family_name^given_name^middle_initial_or_name^suffix^prefix^degree^name_type_code^name_representation_code^name_context^name_validity_range^name_assembly_order
/// </summary>
public class PersonNameField : HL7Field
{
    /// <inheritdoc />
    public override string DataType => "XPN";

    /// <inheritdoc />
    public override int? MaxLength => 250; // Standard maximum for XPN fields

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonNameField"/> class.
    /// </summary>
    /// <param name="value">The initial person name value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public PersonNameField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonNameField"/> class with name components.
    /// </summary>
    /// <param name="familyName">The family/last name.</param>
    /// <param name="givenName">The given/first name.</param>
    /// <param name="middleName">The middle name or initial.</param>
    /// <param name="suffix">The name suffix (Jr., Sr., III, etc.).</param>
    /// <param name="prefix">The name prefix (Dr., Mr., Mrs., etc.).</param>
    /// <param name="degree">The degree (MD, PhD, etc.).</param>
    /// <param name="nameTypeCode">The name type code (L=Legal, D=Display, etc.).</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public PersonNameField(
        string familyName,
        string givenName,
        string? middleName = null,
        string? suffix = null,
        string? prefix = null,
        string? degree = null,
        string? nameTypeCode = null,
        bool isRequired = false)
        : base(null, isRequired)
    {
        SetComponents(familyName, givenName, middleName, suffix, prefix, degree, nameTypeCode);
    }

    /// <summary>
    /// Gets or sets the family/last name component.
    /// </summary>
    public string FamilyName
    {
        get => GetComponent(1);
        set => SetComponent(1, value);
    }

    /// <summary>
    /// Gets or sets the given/first name component.
    /// </summary>
    public string GivenName
    {
        get => GetComponent(2);
        set => SetComponent(2, value);
    }

    /// <summary>
    /// Gets or sets the middle name or initial component.
    /// </summary>
    public string MiddleName
    {
        get => GetComponent(3);
        set => SetComponent(3, value);
    }

    /// <summary>
    /// Gets or sets the name suffix component (Jr., Sr., III, etc.).
    /// </summary>
    public string Suffix
    {
        get => GetComponent(4);
        set => SetComponent(4, value);
    }

    /// <summary>
    /// Gets or sets the name prefix component (Dr., Mr., Mrs., etc.).
    /// </summary>
    public string Prefix
    {
        get => GetComponent(5);
        set => SetComponent(5, value);
    }

    /// <summary>
    /// Gets or sets the degree component (MD, PhD, etc.).
    /// </summary>
    public string Degree
    {
        get => GetComponent(6);
        set => SetComponent(6, value);
    }

    /// <summary>
    /// Gets or sets the name type code component (L=Legal, D=Display, etc.).
    /// </summary>
    public string NameTypeCode
    {
        get => GetComponent(7);
        set => SetComponent(7, value);
    }

    /// <summary>
    /// Gets or sets the name representation code component (A=Alphabetic, I=Ideographic, P=Phonetic).
    /// </summary>
    public string NameRepresentationCode
    {
        get => GetComponent(8);
        set => SetComponent(8, value);
    }

    /// <summary>
    /// Gets or sets the last name (alias for FamilyName for test compatibility).
    /// </summary>
    public string LastName
    {
        get => FamilyName;
        set => FamilyName = value;
    }

    /// <summary>
    /// Gets or sets the first name (alias for GivenName for test compatibility).
    /// </summary>
    public string FirstName
    {
        get => GivenName;
        set => GivenName = value;
    }

    /// <summary>
    /// Gets the formatted display name (First Middle Last Suffix).
    /// </summary>
    public string DisplayName
    {
        get
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(GivenName))
                parts.Add(GivenName);
                
            if (!string.IsNullOrEmpty(MiddleName))
                parts.Add(MiddleName);
                
            if (!string.IsNullOrEmpty(FamilyName))
                parts.Add(FamilyName);
                
            if (!string.IsNullOrEmpty(Suffix))
                parts.Add(Suffix);

            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// Gets the formatted formal name (Prefix First Middle Last Suffix, Degree).
    /// </summary>
    public string FormalName
    {
        get
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(Prefix))
                parts.Add(Prefix);
                
            if (!string.IsNullOrEmpty(GivenName))
                parts.Add(GivenName);
                
            if (!string.IsNullOrEmpty(MiddleName))
                parts.Add(MiddleName);
                
            if (!string.IsNullOrEmpty(FamilyName))
                parts.Add(FamilyName);
                
            if (!string.IsNullOrEmpty(Suffix))
                parts.Add(Suffix);

            var name = string.Join(" ", parts);
            
            if (!string.IsNullOrEmpty(Degree))
                name += $", {Degree}";

            return name;
        }
    }

    /// <summary>
    /// Returns the formatted display name for test compatibility.
    /// </summary>
    /// <returns>The formatted display name.</returns>
    public string ToDisplayString()
    {
        return FormalName.Replace(", ", " "); // Remove degree comma formatting
    }

    /// <summary>
    /// Returns the formatted formal name string for test compatibility.
    /// </summary>
    /// <returns>The formatted formal name.</returns>
    public string ToFormalString()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(FamilyName))
            parts.Add(FamilyName);
            
        var givenParts = new List<string>();
        if (!string.IsNullOrEmpty(GivenName))
            givenParts.Add(GivenName);
        if (!string.IsNullOrEmpty(MiddleName))
            givenParts.Add(MiddleName);
            
        if (givenParts.Count > 0)
            parts.Add(string.Join(" ", givenParts));
            
        if (!string.IsNullOrEmpty(Suffix))
            parts.Add(Suffix);
            
        if (!string.IsNullOrEmpty(Prefix))
            parts.Add(Prefix);

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Gets the initials from the given name, middle name, and family name.
    /// </summary>
    /// <returns>The initials string.</returns>
    public string GetInitials()
    {
        var initials = new List<string>();
        
        if (!string.IsNullOrEmpty(GivenName))
            initials.Add(GivenName.Substring(0, 1).ToUpper());
            
        if (!string.IsNullOrEmpty(MiddleName))
            initials.Add(MiddleName.Substring(0, 1).ToUpper());
            
        if (!string.IsNullOrEmpty(FamilyName))
            initials.Add(FamilyName.Substring(0, 1).ToUpper());

        return string.Join("", initials);
    }

    /// <summary>
    /// Sets the name components using a simplified method (alias for SetComponents).
    /// </summary>
    /// <param name="familyName">The family/last name.</param>
    /// <param name="givenName">The given/first name.</param>
    /// <param name="middleName">The middle name or initial.</param>
    /// <param name="suffix">The name suffix.</param>
    /// <param name="prefix">The name prefix.</param>
    public void SetName(
        string familyName,
        string? givenName = null,
        string? middleName = null,
        string? suffix = null,
        string? prefix = null)
    {
        SetComponents(familyName, givenName, middleName, suffix, prefix);
    }

    /// <summary>
    /// Sets the primary name components.
    /// </summary>
    /// <param name="familyName">The family/last name.</param>
    /// <param name="givenName">The given/first name.</param>
    /// <param name="middleName">The middle name or initial.</param>
    /// <param name="suffix">The name suffix.</param>
    /// <param name="prefix">The name prefix.</param>
    /// <param name="degree">The degree.</param>
    /// <param name="nameTypeCode">The name type code.</param>
    public void SetComponents(
        string? familyName = null,
        string? givenName = null,
        string? middleName = null,
        string? suffix = null,
        string? prefix = null,
        string? degree = null,
        string? nameTypeCode = null)
    {
        var components = new[]
        {
            familyName ?? string.Empty,
            givenName ?? string.Empty,
            middleName ?? string.Empty,
            suffix ?? string.Empty,
            prefix ?? string.Empty,
            degree ?? string.Empty,
            nameTypeCode ?? string.Empty,
            string.Empty, // name_representation_code
            string.Empty, // name_context
            string.Empty, // name_validity_range
            string.Empty  // name_assembly_order
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
    /// <param name="index">The 1-based component index (1-11).</param>
    /// <returns>The component value, or empty string if not present.</returns>
    public string GetComponent(int index)
    {
        if (index < 1 || index > 11)
            throw new ArgumentOutOfRangeException(nameof(index), "Component index must be between 1 and 11.");

        if (IsEmpty)
            return string.Empty;

        var components = ComponentCache.GetComponents(RawValue, '^');
        return index <= components.Length ? components[index - 1] : string.Empty;
    }

    /// <summary>
    /// Sets a component by its 1-based index.
    /// </summary>
    /// <param name="index">The 1-based component index (1-11).</param>
    /// <param name="value">The component value to set.</param>
    public void SetComponent(int index, string? value)
    {
        if (index < 1 || index > 11)
            throw new ArgumentOutOfRangeException(nameof(index), "Component index must be between 1 and 11.");

        var components = IsEmpty ? new string[11] : ComponentCache.GetComponents(RawValue, '^');
        
        // Expand array if necessary
        if (components.Length < 11)
        {
            var newComponents = new string[11];
            Array.Copy(components, newComponents, components.Length);
            for (int i = components.Length; i < 11; i++)
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
            return; // Empty values are handled by the base class

        // Check that we don't have more than 11 components
        var components = ComponentCache.GetComponents(value, '^');
        if (components.Length > 11)
        {
            throw new ArgumentException($"Person name cannot have more than 11 components. Found {components.Length} components.");
        }

        // Validate that components don't contain HL7 control characters
        foreach (var component in components)
        {
            if (component.Contains('|') || component.Contains('&') || component.Contains('~') || component.Contains('\\'))
            {
                throw new ArgumentException("Person name components cannot contain HL7 control characters (|&~\\).");
            }
        }
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new PersonNameField(RawValue, IsRequired);
    }

    /// <summary>
    /// Creates a person name with just first and last name.
    /// </summary>
    /// <param name="firstName">The first/given name.</param>
    /// <param name="lastName">The last/family name.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new PersonNameField instance.</returns>
    public static PersonNameField Create(string firstName, string lastName, bool isRequired = false)
    {
        return new PersonNameField(lastName, firstName, isRequired: isRequired);
    }

    /// <summary>
    /// Creates a person name with first, middle, and last name.
    /// </summary>
    /// <param name="firstName">The first/given name.</param>
    /// <param name="middleName">The middle name or initial.</param>
    /// <param name="lastName">The last/family name.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new PersonNameField instance.</returns>
    public static PersonNameField Create(string firstName, string middleName, string lastName, bool isRequired = false)
    {
        return new PersonNameField(lastName, firstName, middleName, isRequired: isRequired);
    }

    /// <summary>
    /// Implicitly converts a string to a PersonNameField.
    /// </summary>
    public static implicit operator PersonNameField(string value)
    {
        return new PersonNameField(value: value);
    }

    /// <summary>
    /// Implicitly converts a PersonNameField to a string.
    /// </summary>
    public static implicit operator string(PersonNameField field)
    {
        return field.RawValue;
    }

    /// <summary>
    /// Creates a doctor name with "Dr" prefix.
    /// </summary>
    /// <param name="lastName">The last/family name.</param>
    /// <param name="firstName">The first/given name.</param>
    /// <param name="middleName">The middle name or initial.</param>
    /// <param name="suffix">The name suffix (e.g., "MD", "PhD").</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new PersonNameField instance with "Dr" prefix.</returns>
    public static PersonNameField CreateDoctor(string lastName, string firstName, string? middleName = null, string? suffix = null, bool isRequired = false)
    {
        var field = new PersonNameField(isRequired: isRequired);
        field.SetComponents(lastName, firstName, middleName, suffix, "Dr");
        return field;
    }

    /// <summary>
    /// Creates a patient name.
    /// </summary>
    /// <param name="lastName">The last/family name.</param>
    /// <param name="firstName">The first/given name.</param>
    /// <param name="middleName">The middle name or initial.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new PersonNameField instance.</returns>
    public static PersonNameField CreatePatient(string lastName, string firstName, string? middleName = null, bool isRequired = false)
    {
        var field = new PersonNameField(isRequired: isRequired);
        field.SetComponents(lastName, firstName, middleName);
        return field;
    }
}
