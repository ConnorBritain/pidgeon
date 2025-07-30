// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segmint.Core.Standards.Common;

/// <summary>
/// Universal interface for all healthcare standard messages (HL7, FHIR, NCPDP, etc.)
/// Provides common functionality across all healthcare data exchange formats.
/// </summary>
public interface IStandardMessage
{
    /// <summary>
    /// Gets the healthcare standard name (e.g., "HL7", "FHIR", "NCPDP").
    /// </summary>
    string StandardName { get; }

    /// <summary>
    /// Gets the standard version (e.g., "2.3", "R4", "2024.1").
    /// </summary>
    string StandardVersion { get; }

    /// <summary>
    /// Gets the message type (e.g., "RDE", "Patient", "NewRx").
    /// </summary>
    string MessageType { get; }

    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    string MessageId { get; set; }

    /// <summary>
    /// Gets the timestamp when the message was created.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Validates the message according to the healthcare standard's rules.
    /// </summary>
    /// <returns>Validation result with any errors or warnings.</returns>
    ValidationResult Validate();

    /// <summary>
    /// Converts the message to its native format (HL7 pipe-delimited, FHIR JSON, NCPDP XML, etc.).
    /// </summary>
    /// <returns>The message in its native healthcare standard format.</returns>
    string ToNativeFormat();

    /// <summary>
    /// Converts the message to JSON representation for universal processing.
    /// </summary>
    /// <returns>JSON representation of the message.</returns>
    string ToJson();

    /// <summary>
    /// Creates a deep copy of the message.
    /// </summary>
    /// <returns>A cloned copy of the message.</returns>
    IStandardMessage Clone();

    /// <summary>
    /// Gets metadata about the message structure and content.
    /// </summary>
    /// <returns>Message metadata for analysis and documentation.</returns>
    MessageMetadata GetMetadata();
}

/// <summary>
/// Metadata information about a healthcare standard message.
/// </summary>
public class MessageMetadata
{
    /// <summary>
    /// Gets or sets the list of field paths present in the message.
    /// </summary>
    public List<string> FieldPaths { get; set; } = new();

    /// <summary>
    /// Gets or sets the message size in bytes.
    /// </summary>
    public int SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the complexity score (0-100) based on field count and nesting.
    /// </summary>
    public int ComplexityScore { get; set; }

    /// <summary>
    /// Gets or sets custom properties specific to the healthcare standard.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}