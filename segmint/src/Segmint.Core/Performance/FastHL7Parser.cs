// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Buffers;
using System.Text;

namespace Segmint.Core.Performance;

/// <summary>
/// High-performance HL7 message parsing utilities using spans and memory pooling.
/// </summary>
public static class FastHL7Parser
{
    private const char SegmentSeparator = '\r';
    private const char FieldSeparator = '|';
    private const char ComponentSeparator = '^';

    /// <summary>
    /// Quickly extracts segment IDs from an HL7 message without full parsing.
    /// </summary>
    /// <param name="hl7Message">The HL7 message.</param>
    /// <returns>Array of segment IDs found in the message.</returns>
    public static string[] ExtractSegmentIds(ReadOnlySpan<char> hl7Message)
    {
        if (hl7Message.IsEmpty) return Array.Empty<string>();

        // Count segments first to avoid resizing
        var segmentCount = CountSegments(hl7Message);
        if (segmentCount == 0) return Array.Empty<string>();

        var segmentIds = new string[segmentCount];
        var segmentIndex = 0;
        var start = 0;

        for (int i = 0; i <= hl7Message.Length; i++)
        {
            if (i == hl7Message.Length || hl7Message[i] == SegmentSeparator || hl7Message[i] == '\n')
            {
                if (i > start)
                {
                    var segmentSpan = hl7Message.Slice(start, i - start);
                    var segmentId = ExtractSegmentId(segmentSpan);
                    if (!string.IsNullOrEmpty(segmentId) && segmentIndex < segmentIds.Length)
                    {
                        segmentIds[segmentIndex++] = segmentId;
                    }
                }
                start = i + 1;
            }
        }

        // Resize if we found fewer segments than expected
        if (segmentIndex < segmentIds.Length)
        {
            Array.Resize(ref segmentIds, segmentIndex);
        }

        return segmentIds;
    }

    /// <summary>
    /// Quickly extracts a specific field value from a segment without full parsing.
    /// </summary>
    /// <param name="segment">The segment text.</param>
    /// <param name="fieldIndex">The 1-based field index.</param>
    /// <returns>The field value, or empty string if not found.</returns>
    public static string ExtractFieldValue(ReadOnlySpan<char> segment, int fieldIndex)
    {
        if (segment.IsEmpty || fieldIndex < 1) return string.Empty;

        var currentField = 0;
        var start = 0;

        for (int i = 0; i <= segment.Length; i++)
        {
            if (i == segment.Length || segment[i] == FieldSeparator)
            {
                currentField++;
                if (currentField == fieldIndex)
                {
                    return i > start ? segment.Slice(start, i - start).ToString() : string.Empty;
                }
                start = i + 1;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Quickly counts fields in a segment.
    /// </summary>
    /// <param name="segment">The segment text.</param>
    /// <returns>The number of fields found.</returns>
    public static int CountFields(ReadOnlySpan<char> segment)
    {
        if (segment.IsEmpty) return 0;

        var count = 1; // Start with 1 (segment ID)
        foreach (var c in segment)
        {
            if (c == FieldSeparator) count++;
        }
        return count;
    }

    /// <summary>
    /// Quickly extracts components from a field value.
    /// </summary>
    /// <param name="fieldValue">The field value.</param>
    /// <returns>Array of components.</returns>
    public static string[] ExtractComponents(ReadOnlySpan<char> fieldValue)
    {
        if (fieldValue.IsEmpty) return Array.Empty<string>();

        var componentCount = CountComponents(fieldValue);
        if (componentCount <= 1) return new[] { fieldValue.ToString() };

        var components = new string[componentCount];
        var componentIndex = 0;
        var start = 0;

        for (int i = 0; i <= fieldValue.Length; i++)
        {
            if (i == fieldValue.Length || fieldValue[i] == ComponentSeparator)
            {
                components[componentIndex++] = i > start 
                    ? fieldValue.Slice(start, i - start).ToString() 
                    : string.Empty;
                start = i + 1;
            }
        }

        return components;
    }

    /// <summary>
    /// Validates HL7 message structure quickly without full parsing.
    /// </summary>
    /// <param name="hl7Message">The HL7 message to validate.</param>
    /// <returns>True if the message has valid basic structure.</returns>
    public static bool IsValidStructure(ReadOnlySpan<char> hl7Message)
    {
        if (hl7Message.Length < 4) return false;

        // Check for MSH header
        if (!hl7Message.StartsWith("MSH|".AsSpan())) return false;

        // Check for reasonable segment count
        var segmentCount = CountSegments(hl7Message);
        return segmentCount >= 1 && segmentCount <= 1000; // Reasonable bounds
    }

    /// <summary>
    /// Efficiently joins components with the HL7 component separator.
    /// </summary>
    /// <param name="components">The components to join.</param>
    /// <returns>The joined component string.</returns>
    public static string JoinComponents(ReadOnlySpan<string> components)
    {
        if (components.IsEmpty) return string.Empty;
        if (components.Length == 1) return components[0] ?? string.Empty;

        // Convert span to array to avoid ref-like type in lambda
        var componentArray = components.ToArray();
        
        return StringBuilderPool.Execute(sb =>
        {
            for (int i = 0; i < componentArray.Length; i++)
            {
                if (i > 0) sb.Append(ComponentSeparator);
                sb.Append(componentArray[i] ?? string.Empty);
            }
        });
    }

    private static string ExtractSegmentId(ReadOnlySpan<char> segment)
    {
        if (segment.Length < 3) return string.Empty;

        var firstPipeIndex = segment.IndexOf(FieldSeparator);
        if (firstPipeIndex == -1 || firstPipeIndex != 3) return string.Empty;

        return segment.Slice(0, 3).ToString();
    }

    private static int CountSegments(ReadOnlySpan<char> hl7Message)
    {
        if (hl7Message.IsEmpty) return 0;

        var count = 1; // Start with 1
        foreach (var c in hl7Message)
        {
            if (c == SegmentSeparator || c == '\n') count++;
        }
        return count;
    }

    private static int CountComponents(ReadOnlySpan<char> fieldValue)
    {
        if (fieldValue.IsEmpty) return 0;

        var count = 1;
        foreach (var c in fieldValue)
        {
            if (c == ComponentSeparator) count++;
        }
        return count;
    }
}