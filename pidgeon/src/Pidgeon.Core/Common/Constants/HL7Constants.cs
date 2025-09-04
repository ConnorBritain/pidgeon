// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Common.Constants;

/// <summary>
/// Centralized HL7 constants following sacred architectural principles.
/// Static constants are allowed per INIT.md for pure values without state.
/// </summary>
public static class HL7Constants
{
    /// <summary>
    /// HL7 field separator character (|).
    /// </summary>
    public const char FieldSeparator = '|';
    
    /// <summary>
    /// HL7 component separator character (^).
    /// </summary>
    public const char ComponentSeparator = '^';
    
    /// <summary>
    /// HL7 repetition separator character (~).
    /// </summary>
    public const char RepetitionSeparator = '~';
    
    /// <summary>
    /// HL7 escape character (\).
    /// </summary>
    public const char EscapeCharacter = '\\';
    
    /// <summary>
    /// HL7 subcomponent separator character (&).
    /// </summary>
    public const char SubcomponentSeparator = '&';
    
    /// <summary>
    /// Standard HL7 encoding characters string (^~\&).
    /// </summary>
    public const string EncodingCharacters = "^~\\&";
    
    /// <summary>
    /// Full delimiter set for MSH segment (includes field separator).
    /// </summary>
    public static string GetFullDelimiters() => 
        $"{FieldSeparator}{ComponentSeparator}{RepetitionSeparator}{EscapeCharacter}{SubcomponentSeparator}";
    
    /// <summary>
    /// HL7 escape sequences for special characters.
    /// </summary>
    public static class EscapeSequences
    {
        public const string Field = "\\F\\";
        public const string Component = "\\S\\";
        public const string Subcomponent = "\\T\\";
        public const string Repetition = "\\R\\";
        public const string Escape = "\\E\\";
    }
    
    /// <summary>
    /// Common HL7 segment identifiers.
    /// </summary>
    public static class Segments
    {
        public const string MSH = "MSH";
        public const string PID = "PID";
        public const string PV1 = "PV1";
        public const string ORC = "ORC";
        public const string RXE = "RXE";
        public const string RXR = "RXR";
        public const string EVN = "EVN";
        public const string IN1 = "IN1";
        public const string GT1 = "GT1";
        public const string NK1 = "NK1";
    }
}