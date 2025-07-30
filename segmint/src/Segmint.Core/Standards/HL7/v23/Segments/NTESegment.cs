// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Types;
using System.Collections.Generic;
namespace Segmint.Core.Standards.HL7.v23.Segments;

/// <summary>
/// Represents the HL7 NTE (Notes and Comments) segment.
/// Contains free-text notes and comments that can be associated with various other segments.
/// </summary>
public class NTESegment : HL7Segment
{
    /// <inheritdoc />
    public override string SegmentId => "NTE";

    /// <summary>
    /// Initializes a new instance of the <see cref="NTESegment"/> class.
    /// </summary>
    public NTESegment()
    {
        InitializeFields();
    }

    /// <summary>
    /// Initializes the segment fields according to HL7 v2.3 specification.
    /// </summary>
    protected override void InitializeFields()
    {
        // NTE.1 - Set ID - NTE
        AddField(new SequenceIdField(isRequired: false));
        
        // NTE.2 - Source of Comment
        AddField(new CodedValueField(isRequired: false));
        
        // NTE.3 - Comment
        AddField(new TextField(isRequired: false));
    }

    /// <summary>
    /// Gets or sets the set ID.
    /// </summary>
    public SequenceIdField SetId
    {
        get => (SequenceIdField)this[1];
        set => this[1] = value;
    }

    /// <summary>
    /// Gets or sets the source of comment.
    /// </summary>
    public CodedValueField SourceOfComment
    {
        get => (CodedValueField)this[2];
        set => this[2] = value;
    }

    /// <summary>
    /// Gets or sets the comment text.
    /// </summary>
    public TextField Comment
    {
        get => (TextField)this[3];
        set => this[3] = value;
    }

    /// <summary>
    /// Creates a new NTE segment for a patient note.
    /// </summary>
    /// <param name="comment">The comment text.</param>
    /// <param name="setId">The set ID (optional).</param>
    /// <returns>A new <see cref="NTESegment"/> instance.</returns>
    public static NTESegment CreatePatientNote(string comment, int? setId = null)
    {
        var segment = new NTESegment();
        
        if (setId.HasValue)
        {
            segment.SetId.SetValue(setId.Value.ToString());
        }
        
        segment.SourceOfComment.SetValue("P"); // Patient
        segment.Comment.SetValue(comment);
        
        return segment;
    }

    /// <summary>
    /// Creates a new NTE segment for a physician note.
    /// </summary>
    /// <param name="comment">The comment text.</param>
    /// <param name="setId">The set ID (optional).</param>
    /// <returns>A new <see cref="NTESegment"/> instance.</returns>
    public static NTESegment CreatePhysicianNote(string comment, int? setId = null)
    {
        var segment = new NTESegment();
        
        if (setId.HasValue)
        {
            segment.SetId.SetValue(setId.Value.ToString());
        }
        
        segment.SourceOfComment.SetValue("L"); // Physician
        segment.Comment.SetValue(comment);
        
        return segment;
    }

    /// <summary>
    /// Creates a new NTE segment for a pharmacy note.
    /// </summary>
    /// <param name="comment">The comment text.</param>
    /// <param name="setId">The set ID (optional).</param>
    /// <returns>A new <see cref="NTESegment"/> instance.</returns>
    public static NTESegment CreatePharmacyNote(string comment, int? setId = null)
    {
        var segment = new NTESegment();
        
        if (setId.HasValue)
        {
            segment.SetId.SetValue(setId.Value.ToString());
        }
        
        segment.SourceOfComment.SetValue("R"); // Pharmacy
        segment.Comment.SetValue(comment);
        
        return segment;
    }

    /// <summary>
    /// Creates a new NTE segment for a nursing note.
    /// </summary>
    /// <param name="comment">The comment text.</param>
    /// <param name="setId">The set ID (optional).</param>
    /// <returns>A new <see cref="NTESegment"/> instance.</returns>
    public static NTESegment CreateNursingNote(string comment, int? setId = null)
    {
        var segment = new NTESegment();
        
        if (setId.HasValue)
        {
            segment.SetId.SetValue(setId.Value.ToString());
        }
        
        segment.SourceOfComment.SetValue("N"); // Nursing
        segment.Comment.SetValue(comment);
        
        return segment;
    }

    /// <summary>
    /// Creates a new NTE segment for a general note.
    /// </summary>
    /// <param name="comment">The comment text.</param>
    /// <param name="source">The source of the comment (optional).</param>
    /// <param name="setId">The set ID (optional).</param>
    /// <returns>A new <see cref="NTESegment"/> instance.</returns>
    public static NTESegment CreateGeneralNote(string comment, string? source = null, int? setId = null)
    {
        var segment = new NTESegment();
        
        if (setId.HasValue)
        {
            segment.SetId.SetValue(setId.Value.ToString());
        }
        
        if (!string.IsNullOrEmpty(source))
        {
            segment.SourceOfComment.SetValue(source);
        }
        
        segment.Comment.SetValue(comment);
        
        return segment;
    }

    /// <summary>
    /// Sets the comment text.
    /// </summary>
    /// <param name="comment">The comment text.</param>
    public void SetComment(string comment)
    {
        Comment.SetValue(comment);
    }

    /// <summary>
    /// Sets the source of comment.
    /// </summary>
    /// <param name="source">The source code (P=Patient, L=Physician, R=Pharmacy, N=Nursing, etc.).</param>
    public void SetSourceOfComment(string source)
    {
        SourceOfComment.SetValue(source);
    }

    /// <summary>
    /// Sets the set ID for this note.
    /// </summary>
    /// <param name="setId">The set ID.</param>
    public void SetSetId(int setId)
    {
        SetId.SetValue(setId.ToString());
    }

    /// <summary>
    /// Gets the display text for the comment source.
    /// </summary>
    /// <returns>A human-readable description of the comment source.</returns>
    public string GetSourceDisplayText()
    {
        return SourceOfComment.RawValue switch
        {
            "P" => "Patient",
            "L" => "Physician",
            "R" => "Pharmacy",
            "N" => "Nursing",
            "A" => "Ancillary",
            "O" => "Other",
            _ => SourceOfComment.RawValue
        };
    }

    /// <summary>
    /// Gets a formatted display string for this note.
    /// </summary>
    /// <returns>A human-readable representation of the note.</returns>
    public string ToDisplayString()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(SourceOfComment.RawValue))
        {
            parts.Add($"[{GetSourceDisplayText()}]");
        }
        
        if (!string.IsNullOrEmpty(Comment.RawValue))
        {
            parts.Add(Comment.RawValue);
        }
        
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Creates a copy of this segment.
    /// </summary>
    /// <returns>A new instance with the same field values.</returns>
    public override HL7Segment Clone()
    {
        var cloned = new NTESegment();
        for (var i = 1; i <= FieldCount; i++)
        {
            cloned[i] = this[i].Clone();
        }
        return cloned;
    }
}
