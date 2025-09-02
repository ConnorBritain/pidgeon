// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Configuration.Entities;

/// <summary>
/// Hierarchical addressing system for vendor configurations.
/// Follows the pattern: VENDOR -> STANDARD -> MESSAGE_TYPE
/// </summary>
/// <param name="Vendor">Vendor name (e.g., "Epic", "Cerner", "CorEMR")</param>
/// <param name="Standard">Standard name (e.g., "HL7v23", "FHIRv4", "NCPDP")</param>
/// <param name="MessageType">Message type (e.g., "ADT^A01", "Patient", "NewRx")</param>
public record ConfigurationAddress(
    [property: JsonPropertyName("vendor")] string Vendor,
    [property: JsonPropertyName("standard")] string Standard,
    [property: JsonPropertyName("messageType")] string MessageType
)
{
    /// <summary>
    /// Returns a string representation in the format "Vendor-Standard-MessageType".
    /// </summary>
    /// <returns>Formatted address string</returns>
    public override string ToString() => $"{Vendor}-{Standard}-{MessageType}";

    /// <summary>
    /// Creates a ConfigurationAddress from a formatted string.
    /// </summary>
    /// <param name="addressString">String in format "Vendor-Standard-MessageType"</param>
    /// <returns>A result containing parsed ConfigurationAddress or validation error</returns>
    public static Result<ConfigurationAddress> Parse(string addressString)
    {
        if (string.IsNullOrEmpty(addressString))
            return Result<ConfigurationAddress>.Failure(Error.Validation("Address string cannot be null or empty", "AddressString"));

        var parts = addressString.Split('-');
        if (parts.Length != 3)
            return Result<ConfigurationAddress>.Failure(Error.Validation(
                "Address string must be in format 'Vendor-Standard-MessageType'", 
                "AddressString"));

        return Result<ConfigurationAddress>.Success(new ConfigurationAddress(parts[0], parts[1], parts[2]));
    }

    /// <summary>
    /// Attempts to parse a ConfigurationAddress from a formatted string.
    /// </summary>
    /// <param name="addressString">String to parse</param>
    /// <param name="address">Parsed address if successful</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParse(string? addressString, out ConfigurationAddress? address)
    {
        address = null;
        
        if (string.IsNullOrEmpty(addressString))
            return false;

        var parseResult = Parse(addressString);
        if (parseResult.IsSuccess)
        {
            address = parseResult.Value;
            return true;
        }
        
        address = null;
        return false;
    }

    /// <summary>
    /// Creates a partial address for vendor-level queries.
    /// </summary>
    /// <param name="vendor">Vendor name</param>
    /// <returns>Address with wildcards for standard and message type</returns>
    public static ConfigurationAddress ForVendor(string vendor) => 
        new(vendor, "*", "*");

    /// <summary>
    /// Creates a partial address for standard-level queries.
    /// </summary>
    /// <param name="standard">Standard name</param>
    /// <returns>Address with wildcards for vendor and message type</returns>
    public static ConfigurationAddress ForStandard(string standard) => 
        new("*", standard, "*");

    /// <summary>
    /// Gets whether this address represents a wildcard query.
    /// </summary>
    [JsonIgnore]
    public bool IsWildcard => Vendor == "*" || Standard == "*" || MessageType == "*";

    /// <summary>
    /// Checks if this address matches another address, supporting wildcards.
    /// </summary>
    /// <param name="other">Address to match against</param>
    /// <returns>True if addresses match (wildcards match any value)</returns>
    public bool Matches(ConfigurationAddress other)
    {
        return (Vendor == "*" || other.Vendor == "*" || Vendor == other.Vendor) &&
               (Standard == "*" || other.Standard == "*" || Standard == other.Standard) &&
               (MessageType == "*" || other.MessageType == "*" || MessageType == other.MessageType);
    }
}