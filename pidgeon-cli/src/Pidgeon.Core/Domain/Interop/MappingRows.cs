namespace Pidgeon.Core.Domain.Interop;

/// <summary>
/// Represents a row from an HL7 segment to FHIR resource mapping CSV file.
/// Maps to the structure of files like "HL7 Segment - FHIR R4_ PID[Patient] - PID.csv"
/// </summary>
public record SegmentMappingRow
{
    public string? SortOrder { get; init; }
    public string? HL7Identifier { get; init; }        // e.g., "PID-3"
    public string? HL7Name { get; init; }              // e.g., "Patient Identifier List"
    public string? HL7DataType { get; init; }          // e.g., "CX"
    public string? HL7CardinalityMin { get; init; }    // e.g., "1"
    public string? HL7CardinalityMax { get; init; }    // e.g., "-1"
    public string? Condition { get; init; }            // IF/THEN logic
    public string? FHIRAttribute { get; init; }        // e.g., "identifier[2]"
    public string? FHIRDataType { get; init; }         // e.g., "Identifier"
    public string? FHIRCardinalityMin { get; init; }
    public string? FHIRCardinalityMax { get; init; }
    public string? DataTypeMapping { get; init; }      // e.g., "CX[Identifier]"
    public string? VocabularyMapping { get; init; }
    public string? Assignment { get; init; }
    public string? Comments { get; init; }
}

/// <summary>
/// Represents a row from an HL7 data type to FHIR data type mapping CSV file.
/// Maps to the structure of files like "HL7 Data Type - FHIR R4_ CX[Identifier] - Sheet1.csv"
/// </summary>
public record DataTypeMappingRow
{
    public string? SortOrder { get; init; }
    public string? HL7Identifier { get; init; }        // e.g., "CX.1"
    public string? HL7Name { get; init; }              // e.g., "ID Number"
    public string? HL7DataType { get; init; }          // e.g., "ST"
    public string? HL7CardinalityMin { get; init; }
    public string? HL7CardinalityMax { get; init; }
    public string? Condition { get; init; }
    public string? FHIRAttribute { get; init; }        // e.g., "value"
    public string? FHIRDataType { get; init; }         // e.g., "string"
    public string? FHIRCardinalityMin { get; init; }
    public string? FHIRCardinalityMax { get; init; }
    public string? DataTypeMapping { get; init; }
    public string? VocabularyMapping { get; init; }
    public string? Assignment { get; init; }
    public string? Comments { get; init; }
}

/// <summary>
/// Represents a row from an HL7 code system to FHIR vocabulary mapping CSV file.
/// Maps to the structure of files like "HL7 Concept Map_ AdministrativeSex - Sheet1.csv"
/// </summary>
public record CodeSystemMappingRow
{
    public string? HL7Code { get; init; }              // e.g., "M"
    public string? HL7Display { get; init; }           // e.g., "Male"
    public string? FHIRCode { get; init; }             // e.g., "male"
    public string? FHIRDisplay { get; init; }          // e.g., "Male"
    public string? FHIRSystem { get; init; }           // e.g., "http://hl7.org/fhir/administrative-gender"
    public string? Comments { get; init; }
}

/// <summary>
/// Aggregated mapping file information extracted from CSV parsing
/// </summary>
public record MappingFileInfo
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public MappingFileType FileType { get; init; }
    public string SourceType { get; init; } = string.Empty;      // e.g., "PID", "CX", "AdministrativeSex"
    public string TargetType { get; init; } = string.Empty;      // e.g., "Patient", "Identifier"
    public int RowCount { get; init; }
    public DateTime LastModified { get; init; }
}

public enum MappingFileType
{
    Segment,
    DataType,
    CodeSystem,
    Message
}