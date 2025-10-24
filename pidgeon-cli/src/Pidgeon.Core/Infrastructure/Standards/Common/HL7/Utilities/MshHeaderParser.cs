// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Infrastructure.Standards.Common.HL7.Utilities;

/// <summary>
/// Provides consolidated MSH (Message Header) segment field extraction utilities.
/// Eliminates duplication of MSH field parsing logic across HL7 processing components.
/// </summary>
internal static class MshHeaderParser
{
    // MSH Field positions (1-based indexing in HL7, but 0-based after split)
    private const int SendingApplicationIndex = 2;    // MSH.3
    private const int SendingFacilityIndex = 3;       // MSH.4
    private const int ReceivingApplicationIndex = 4;  // MSH.5
    private const int ReceivingFacilityIndex = 5;     // MSH.6
    private const int MessageControlIdIndex = 9;      // MSH.10
    private const int VersionIdIndex = 11;            // MSH.12
    
    /// <summary>
    /// Extracts a specific field value from MSH segment in the provided segments list.
    /// </summary>
    /// <param name="segments">List of HL7 message segments</param>
    /// <param name="fieldIndex">Zero-based field index after splitting by |</param>
    /// <returns>Field value or null if not found</returns>
    public static string? ExtractMshField(List<string> segments, int fieldIndex)
    {
        var mshSegment = segments.FirstOrDefault(s => s.StartsWith("MSH"));
        if (mshSegment == null) return null;

        var fields = mshSegment.Split('|');
        return fields.Length > fieldIndex ? fields[fieldIndex] : null;
    }
    
    /// <summary>
    /// Extracts the message control ID from MSH.10 field.
    /// </summary>
    public static string? ExtractMessageControlId(List<string> segments) 
        => ExtractMshField(segments, MessageControlIdIndex);
    
    /// <summary>
    /// Extracts the sending system from MSH.3 field.
    /// </summary>
    public static string? ExtractSendingSystem(List<string> segments) 
        => ExtractMshField(segments, SendingApplicationIndex);
    
    /// <summary>
    /// Extracts the receiving system from MSH.5 field.
    /// </summary>
    public static string? ExtractReceivingSystem(List<string> segments) 
        => ExtractMshField(segments, ReceivingApplicationIndex);
    
    /// <summary>
    /// Extracts the HL7 version ID from MSH.12 field.
    /// </summary>
    public static string? ExtractVersionId(List<string> segments) 
        => ExtractMshField(segments, VersionIdIndex);
    
    /// <summary>
    /// Extracts the sending facility from MSH.4 field.
    /// </summary>
    public static string? ExtractSendingFacility(List<string> segments) 
        => ExtractMshField(segments, SendingFacilityIndex);
    
    /// <summary>
    /// Extracts the receiving facility from MSH.6 field.
    /// </summary>
    public static string? ExtractReceivingFacility(List<string> segments) 
        => ExtractMshField(segments, ReceivingFacilityIndex);
    
    /// <summary>
    /// Extracts all header fields from MSH segment at once for efficiency.
    /// </summary>
    public static MshHeaderFields? ExtractAllHeaderFields(List<string> segments)
    {
        var mshSegment = segments.FirstOrDefault(s => s.StartsWith("MSH"));
        if (mshSegment == null) return null;

        var fields = mshSegment.Split('|');
        
        return new MshHeaderFields
        {
            SendingApplication = fields.Length > SendingApplicationIndex ? fields[SendingApplicationIndex] : null,
            SendingFacility = fields.Length > SendingFacilityIndex ? fields[SendingFacilityIndex] : null,
            ReceivingApplication = fields.Length > ReceivingApplicationIndex ? fields[ReceivingApplicationIndex] : null,
            ReceivingFacility = fields.Length > ReceivingFacilityIndex ? fields[ReceivingFacilityIndex] : null,
            MessageControlId = fields.Length > MessageControlIdIndex ? fields[MessageControlIdIndex] : null,
            VersionId = fields.Length > VersionIdIndex ? fields[VersionIdIndex] : null
        };
    }
}

/// <summary>
/// Container for commonly extracted MSH header fields.
/// </summary>
internal class MshHeaderFields
{
    public string? SendingApplication { get; init; }
    public string? SendingFacility { get; init; }
    public string? ReceivingApplication { get; init; }
    public string? ReceivingFacility { get; init; }
    public string? MessageControlId { get; init; }
    public string? VersionId { get; init; }
}