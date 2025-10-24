// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Messages;

/// <summary>
/// Generic HL7 segment implementation for parsing and analysis scenarios.
/// Used when the specific segment type is not known or not important.
/// </summary>
public class GenericHL7Segment : HL7Segment
{
    private readonly string _segmentId;
    
    /// <summary>
    /// Gets the segment ID.
    /// </summary>
    public override string SegmentId => _segmentId;
    
    /// <summary>
    /// Raw field values for this segment.
    /// </summary>
    public new string[] Fields { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Number of fields in this segment.
    /// </summary>
    public int FieldCount => Fields.Length;

    /// <summary>
    /// Creates a generic segment from raw field data.
    /// </summary>
    public GenericHL7Segment(string segmentId, string[] fields)
    {
        _segmentId = segmentId;
        Fields = fields;
    }
    
    /// <summary>
    /// Default constructor for serialization.
    /// </summary>
    public GenericHL7Segment() 
    {
        _segmentId = "UNK";
    }
}