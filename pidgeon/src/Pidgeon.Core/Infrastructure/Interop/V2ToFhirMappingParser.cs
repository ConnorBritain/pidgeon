using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Pidgeon.Core.Domain.Interop;

namespace Pidgeon.Core.Infrastructure.Interop;

public class V2ToFhirMappingParser
{
    public async Task<IReadOnlyList<SegmentMappingRow>> ParseSegmentMappingAsync(string csvFilePath)
    {
        var fileContent = await File.ReadAllTextAsync(csvFilePath);
        var lines = fileContent.Split('\n');

        // Skip the first 3 lines (section headers and multi-line column headers)
        // and start reading from line 4 which contains the actual data
        var dataLines = lines.Skip(3).Where(line => !string.IsNullOrWhiteSpace(line));
        var csvContent = string.Join('\n', dataLines);

        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false, // We'll parse without headers since structure is complex
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null
        });

        var records = new List<SegmentMappingRow>();

        while (await csv.ReadAsync())
        {
            try
            {
                // Parse fields by position since the header structure is complex
                var record = new SegmentMappingRow
                {
                    SortOrder = csv.GetField(0),           // Sort Order
                    HL7Identifier = csv.GetField(1),        // Identifier
                    HL7Name = csv.GetField(2),              // Name
                    HL7DataType = csv.GetField(3),          // Data Type
                    HL7CardinalityMin = csv.GetField(4),    // Cardinality - Min
                    HL7CardinalityMax = csv.GetField(5),    // Cardinality - Max
                    Condition = csv.GetField(6),            // Condition (IF True)
                    FHIRAttribute = csv.GetField(9),        // FHIR Attribute
                    FHIRDataType = csv.GetField(11),        // FHIR Data Type
                    FHIRCardinalityMin = csv.GetField(12),  // FHIR Cardinality - Min
                    FHIRCardinalityMax = csv.GetField(13),  // FHIR Cardinality - Max
                    DataTypeMapping = csv.GetField(14),     // Data Type Mapping
                    VocabularyMapping = csv.GetField(15),   // Vocabulary Mapping
                    Assignment = csv.GetField(16),          // Assignment
                    Comments = csv.GetField(17)             // Comments
                };

                // Only add non-empty rows with valid identifiers
                if (!string.IsNullOrWhiteSpace(record.HL7Identifier))
                {
                    records.Add(record);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing row {csv.Parser.Row}: {ex.Message}");
            }
        }

        return records;
    }

    public async Task<IReadOnlyList<DataTypeMappingRow>> ParseDataTypeMappingAsync(string csvFilePath)
    {
        var fileContent = await File.ReadAllTextAsync(csvFilePath);
        var lines = fileContent.Split('\n');

        // Skip the first 3 lines (section headers and multi-line column headers)
        var dataLines = lines.Skip(3).Where(line => !string.IsNullOrWhiteSpace(line));
        var csvContent = string.Join('\n', dataLines);

        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null
        });

        var records = new List<DataTypeMappingRow>();

        while (await csv.ReadAsync())
        {
            try
            {
                var record = new DataTypeMappingRow
                {
                    SortOrder = csv.GetField(0),           // Sort Order
                    HL7Identifier = csv.GetField(1),        // Identifier
                    HL7Name = csv.GetField(2),              // Name
                    HL7DataType = csv.GetField(3),          // Data Type
                    HL7CardinalityMin = csv.GetField(4),    // Cardinality - Min
                    HL7CardinalityMax = csv.GetField(5),    // Cardinality - Max
                    Condition = csv.GetField(6),            // Condition (IF True)
                    FHIRAttribute = csv.GetField(9),        // FHIR Attribute
                    FHIRDataType = csv.GetField(11),        // FHIR Data Type
                    FHIRCardinalityMin = csv.GetField(12),  // FHIR Cardinality - Min
                    FHIRCardinalityMax = csv.GetField(13),  // FHIR Cardinality - Max
                    DataTypeMapping = csv.GetField(14),     // Data Type Mapping
                    VocabularyMapping = csv.GetField(15),   // Vocabulary Mapping
                    Assignment = csv.GetField(16),          // Assignment
                    Comments = csv.GetField(17)             // Comments
                };

                if (!string.IsNullOrWhiteSpace(record.HL7Identifier))
                {
                    records.Add(record);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing row {csv.Parser.Row}: {ex.Message}");
            }
        }

        return records;
    }

    public async Task<IReadOnlyList<CodeSystemMappingRow>> ParseCodeSystemMappingAsync(string csvFilePath)
    {
        using var reader = new StringReader(await File.ReadAllTextAsync(csvFilePath));
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null
        });

        var records = new List<CodeSystemMappingRow>();
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            try
            {
                var record = new CodeSystemMappingRow
                {
                    HL7Code = csv.GetField(0),
                    HL7Display = csv.GetField(1),
                    FHIRCode = csv.GetField(2),
                    FHIRDisplay = csv.GetField(3),
                    FHIRSystem = csv.GetField(4),
                    Comments = csv.GetField(5)
                };

                if (!string.IsNullOrWhiteSpace(record.HL7Code))
                {
                    records.Add(record);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing row {csv.Parser.Row}: {ex.Message}");
            }
        }

        return records;
    }

    public string ExtractSegmentTypeFromFileName(string fileName)
    {
        // Extract segment name from "HL7 Segment - FHIR R4_ PID[Patient] - PID.csv"
        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"HL7 Segment.*?([A-Z0-9]{2,3})\[([^\]]+)\]");
        if (match.Success)
        {
            return $"{match.Groups[1].Value}[{match.Groups[2].Value}]";
        }
        return Path.GetFileNameWithoutExtension(fileName);
    }

    public string ExtractDataTypeFromFileName(string fileName)
    {
        // Extract datatype from "HL7 Data Type - FHIR R4_ CX[Identifier] - Sheet1.csv"
        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"HL7 Data Type.*?([A-Z0-9_]+)\[([^\]]+)\]");
        if (match.Success)
        {
            return $"{match.Groups[1].Value}[{match.Groups[2].Value}]";
        }
        return Path.GetFileNameWithoutExtension(fileName);
    }

    public string ExtractCodeSystemFromFileName(string fileName)
    {
        // Extract code system from "HL7 Concept Map_ AdministrativeSex - Sheet1.csv"
        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"HL7 Concept Map_ (.+?) - ");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return Path.GetFileNameWithoutExtension(fileName);
    }
}