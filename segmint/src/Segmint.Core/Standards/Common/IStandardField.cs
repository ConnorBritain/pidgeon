// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace Segmint.Core.Standards.Common;

/// <summary>
/// Universal interface for healthcare standard data fields (HL7 fields, FHIR elements, NCPDP data elements, etc.)
/// Provides common functionality for validation, conversion, and metadata across all healthcare standards.
/// </summary>
public interface IStandardField
{
    /// <summary>
    /// Gets the healthcare standard this field belongs to.
    /// </summary>
    string StandardName { get; }

    /// <summary>
    /// Gets the field name or identifier within the standard.
    /// </summary>
    string FieldName { get; }

    /// <summary>
    /// Gets the data type of the field (e.g., "ST", "string", "CodeableConcept").
    /// </summary>
    string DataType { get; }

    /// <summary>
    /// Gets or sets whether this field is required by the standard.
    /// </summary>
    bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets whether this field can repeat (have multiple values).
    /// </summary>
    bool IsRepeating { get; set; }

    /// <summary>
    /// Gets or sets the maximum length for text fields (null if no limit).
    /// </summary>
    int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the field value as an object (can be string, DateTime, complex type, etc.).
    /// </summary>
    object? Value { get; set; }

    /// <summary>
    /// Gets whether the field currently has a value.
    /// </summary>
    bool HasValue { get; }

    /// <summary>
    /// Validates the field value according to the healthcare standard's rules.
    /// </summary>
    /// <returns>Validation result with any errors or warnings.</returns>
    ValidationResult Validate();

    /// <summary>
    /// Converts the field value to its native standard format (HL7 encoded, FHIR JSON, etc.).
    /// </summary>
    /// <returns>The field value in native format.</returns>
    string ToNativeFormat();

    /// <summary>
    /// Converts the field value to a universal string representation.
    /// </summary>
    /// <returns>String representation of the field value.</returns>
    string ToUniversalString();

    /// <summary>
    /// Parses a string value into the appropriate typed value for this field.
    /// </summary>
    /// <param name="value">String value to parse.</param>
    void ParseFromString(string value);

    /// <summary>
    /// Creates a deep copy of the field.
    /// </summary>
    /// <returns>A cloned copy of the field.</returns>
    IStandardField Clone();

    /// <summary>
    /// Gets metadata about the field definition and current value.
    /// </summary>
    /// <returns>Field metadata for documentation and analysis.</returns>
    FieldMetadata GetMetadata();
}

/// <summary>
/// Metadata information about a healthcare standard field.
/// </summary>
public class FieldMetadata
{
    /// <summary>
    /// Gets or sets the field description from the standard documentation.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field path within the message structure.
    /// </summary>
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of valid values for coded fields.
    /// </summary>
    public List<string> ValidValues { get; set; } = new();

    /// <summary>
    /// Gets or sets the table or value set reference for coded fields.
    /// </summary>
    public string? CodeSystem { get; set; }

    /// <summary>
    /// Gets or sets example values for documentation purposes.
    /// </summary>
    public List<string> ExampleValues { get; set; } = new();

    /// <summary>
    /// Gets or sets custom properties specific to the healthcare standard.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}