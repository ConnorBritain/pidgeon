// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;
using Pidgeon.Core.Domain.Messaging.HL7v2.Segments;

namespace Pidgeon.Core.Infrastructure.Standards.Common.HL7;

/// <summary>
/// HL7 v2.3 message parser.
/// Inspired by CIPS' proven depth-based delimiter parsing approach.
/// </summary>
public class HL7Parser
{
    /// <summary>
    /// HL7 delimiter characters.
    /// </summary>
    public class Delimiters
    {
        public char FieldSeparator { get; set; } = '|';
        public char ComponentSeparator { get; set; } = '^';
        public char RepetitionSeparator { get; set; } = '~';
        public char EscapeCharacter { get; set; } = '\\';
        public char SubcomponentSeparator { get; set; } = '&';
        public string SegmentSeparator { get; set; } = "\r";
        
        /// <summary>
        /// Gets the encoding characters string (^~\&).
        /// </summary>
        public string EncodingCharacters => 
            $"{ComponentSeparator}{RepetitionSeparator}{EscapeCharacter}{SubcomponentSeparator}";
            
        /// <summary>
        /// Extracts delimiters from MSH segment.
        /// </summary>
        public static Delimiters FromMSH(string mshSegment)
        {
            if (string.IsNullOrEmpty(mshSegment) || !mshSegment.StartsWith("MSH"))
                throw new ArgumentException("Invalid MSH segment");
                
            var delims = new Delimiters();
            
            // MSH|^~\&|...
            if (mshSegment.Length >= 9)
            {
                delims.FieldSeparator = mshSegment[3];
                delims.ComponentSeparator = mshSegment[4];
                delims.RepetitionSeparator = mshSegment[5];
                delims.EscapeCharacter = mshSegment[6];
                delims.SubcomponentSeparator = mshSegment[7];
            }
            
            return delims;
        }
    }
    
    private readonly Delimiters _delimiters;
    
    public HL7Parser(Delimiters? delimiters = null)
    {
        _delimiters = delimiters ?? new Delimiters();
    }
    
    /// <summary>
    /// Parses an HL7 message string into a strongly-typed message object.
    /// </summary>
    /// <param name="messageString">The raw HL7 message string</param>
    /// <returns>A result containing the parsed message or error</returns>
    public Result<HL7Message> ParseMessage(string messageString)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(messageString))
                return Error.Parsing("Message string is empty", "HL7Parser");
                
            // Clean the message (remove transport wrapper if present)
            var cleanMessage = CleanMessage(messageString);
            
            // Split into segments
            var segments = SplitSegments(cleanMessage);
            if (segments.Count == 0)
                return Error.Parsing("No segments found in message", "HL7Parser");
                
            // First segment must be MSH
            if (!segments[0].StartsWith("MSH"))
                return Error.Parsing("Message must start with MSH segment", "HL7Parser");
                
            // Extract delimiters from MSH
            _delimiters.FieldSeparator = segments[0][3];
            if (segments[0].Length >= 8)
            {
                _delimiters.ComponentSeparator = segments[0][4];
                _delimiters.RepetitionSeparator = segments[0][5];
                _delimiters.EscapeCharacter = segments[0][6];
                _delimiters.SubcomponentSeparator = segments[0][7];
            }
            
            // Parse MSH to determine message type
            var mshResult = ParseMSHSegment(segments[0]);
            if (mshResult.IsFailure)
                return Error.Parsing($"Failed to parse MSH segment: {mshResult.Error.Message}", "HL7Parser");
                
            var msh = mshResult.Value;
            var messageType = msh.GetMessageTypeComponents();
            
            if (messageType == null)
                return Error.Parsing("Could not determine message type from MSH", "HL7Parser");
                
            // Create appropriate message type based on what we have implemented
            HL7Message message = messageType.Value.MessageCode switch
            {
                "ADT" => new ADTMessage(),
                "RDE" => new RDEMessage(),
                // TODO: Implement these message types for complete HL7 MVP:
                // "ORM" => new ORMMessage(),  // Lab orders
                // "ORU" => new ORUMessage(),  // Lab results  
                // "ACK" => new ACKMessage(),  // Acknowledgments
                _ => new GenericHL7Message(messageType.Value.MessageCode)
            };
            
            // Parse the entire message string
            var parseResult = message.ParseHL7String(cleanMessage);
            if (parseResult.IsFailure)
                return parseResult;
                
            return Result<HL7Message>.Success(message);
        }
        catch (Exception ex)
        {
            return Error.Parsing($"Exception during parsing: {ex.Message}", "HL7Parser");
        }
    }
    
    /// <summary>
    /// Parses a single segment string.
    /// </summary>
    /// <param name="segmentString">The raw segment string</param>
    /// <returns>A result containing the parsed segment or error</returns>
    public Result<HL7Segment> ParseSegment(string segmentString)
    {
        if (string.IsNullOrWhiteSpace(segmentString))
            return Error.Parsing("Segment string is empty", "HL7Parser");
            
        // Get segment ID (first 3 characters)
        if (segmentString.Length < 3)
            return Error.Parsing($"Invalid segment string: {segmentString}", "HL7Parser");
            
        var segmentId = segmentString.Substring(0, 3);
        
        // Create appropriate segment based on ID
        HL7Segment segment = segmentId switch
        {
            // Currently implemented segments:
            "MSH" => new MSHSegment(),
            "PID" => new PIDSegment(),
            "PV1" => new PV1Segment(),
            "ORC" => new ORCSegment(),
            "RXE" => new RXESegment(),
            "RXR" => new RXRSegment(),
            
            // TODO: Implement these segments for complete HL7 MVP:
            // "OBR" => new OBRSegment(),  // Observation Request - needed for lab orders
            // "OBX" => new OBXSegment(),  // Observation Result - needed for lab results
            // "NTE" => new NTESegment(),  // Notes and Comments
            // "EVN" => new EVNSegment(),  // Event Type - for ADT events
            // "IN1" => new IN1Segment(),  // Insurance
            // "DG1" => new DG1Segment(),  // Diagnosis
            // "AL1" => new AL1Segment(),  // Allergy Information
            
            _ => new GenericSegment(segmentId)
        };
        
        // Parse the segment
        var parseResult = segment.ParseHL7String(segmentString);
        if (parseResult.IsFailure)
            return Error.Parsing($"Failed to parse {segmentId} segment: {parseResult.Error.Message}", "HL7Parser");
            
        return Result<HL7Segment>.Success(segment);
    }
    
    /// <summary>
    /// Parses just the MSH segment to extract message metadata.
    /// </summary>
    private Result<MSHSegment> ParseMSHSegment(string mshString)
    {
        var msh = new MSHSegment();
        var result = msh.ParseHL7String(mshString);
        
        if (result.IsFailure)
            return Error.Parsing($"Failed to parse MSH: {result.Error.Message}", "MSH");
            
        return Result<MSHSegment>.Success(msh);
    }
    
    /// <summary>
    /// Cleans a message string by removing transport wrappers.
    /// </summary>
    private string CleanMessage(string message)
    {
        // Remove MLLP wrapper if present (0x0B...0x1C 0x0D)
        const char StartBlock = '\x0B';
        const char EndBlock = '\x1C';
        
        if (message.Length > 0 && message[0] == StartBlock)
        {
            var endIndex = message.IndexOf(EndBlock);
            if (endIndex > 0)
            {
                message = message.Substring(1, endIndex - 1);
            }
        }
        
        // Normalize line endings
        message = message.Replace("\r\n", "\r").Replace("\n", "\r");
        
        // Remove trailing segment separator
        message = message.TrimEnd('\r', '\n');
        
        return message;
    }
    
    /// <summary>
    /// Splits a message into segments.
    /// </summary>
    private List<string> SplitSegments(string message)
    {
        // Handle various segment separators
        var separators = new[] { "\r\n", "\r", "\n" };
        var segments = message.Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
            
        return segments;
    }
    
    /// <summary>
    /// Unescapes HL7 escape sequences in a value.
    /// </summary>
    public string UnescapeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
            
        var result = value;
        
        // Standard HL7 escape sequences
        result = result.Replace("\\F\\", _delimiters.FieldSeparator.ToString());
        result = result.Replace("\\S\\", _delimiters.ComponentSeparator.ToString());
        result = result.Replace("\\T\\", _delimiters.SubcomponentSeparator.ToString());
        result = result.Replace("\\R\\", _delimiters.RepetitionSeparator.ToString());
        result = result.Replace("\\E\\", _delimiters.EscapeCharacter.ToString());
        
        // Hex character escapes (e.g., \X0D\ for carriage return)
        var hexPattern = @"\\X([0-9A-F]{2,4})\\";
        result = System.Text.RegularExpressions.Regex.Replace(result, hexPattern, m =>
        {
            var hex = m.Groups[1].Value;
            var charCode = Convert.ToInt32(hex, 16);
            return ((char)charCode).ToString();
        });
        
        return result;
    }
    
    /// <summary>
    /// Escapes special characters in a value for HL7 encoding.
    /// </summary>
    public string EscapeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
            
        var result = value;
        
        // Escape in specific order to avoid double-escaping
        result = result.Replace(_delimiters.EscapeCharacter.ToString(), "\\E\\");
        result = result.Replace(_delimiters.FieldSeparator.ToString(), "\\F\\");
        result = result.Replace(_delimiters.ComponentSeparator.ToString(), "\\S\\");
        result = result.Replace(_delimiters.SubcomponentSeparator.ToString(), "\\T\\");
        result = result.Replace(_delimiters.RepetitionSeparator.ToString(), "\\R\\");
        
        // Escape control characters
        result = result.Replace("\r", "\\X0D\\");
        result = result.Replace("\n", "\\X0A\\");
        
        return result;
    }
}

/// <summary>
/// Generic HL7 message for unknown message types.
/// </summary>
public class GenericHL7Message : HL7Message
{
    public GenericHL7Message(string messageType)
    {
        // Parse the message type string into HL7MessageType format
        MessageType = HL7MessageType.Parse(messageType);
    }
    
    public override required HL7MessageType MessageType { get; set; }
    
    public override void InitializeMessage()
    {
        // Generic messages start with just MSH
        AddSegment(new MSHSegment());
    }
}

/// <summary>
/// Generic segment for unknown segment types.
/// </summary>
public class GenericSegment : HL7Segment
{
    private readonly string _segmentId;
    
    public GenericSegment(string segmentId)
    {
        _segmentId = segmentId;
    }
    
    public override string SegmentId => _segmentId;
    
    public override void InitializeFields()
    {
        // Generic segments have no predefined fields
        // They will be populated during parsing
    }
}