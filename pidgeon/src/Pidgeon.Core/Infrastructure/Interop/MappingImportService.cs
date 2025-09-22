using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Interop;

namespace Pidgeon.Core.Infrastructure.Interop;

/// <summary>
/// Service for importing HL7 v2-to-FHIR mapping CSV files and generating semantic path definitions
/// </summary>
public class MappingImportService
{
    private readonly V2ToFhirMappingParser _parser;
    private readonly ILogger<MappingImportService> _logger;

    public MappingImportService(V2ToFhirMappingParser parser, ILogger<MappingImportService> logger)
    {
        _parser = parser;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MappingFileInfo>> DiscoverMappingFilesAsync(string mappingsDirectory)
    {
        var files = new List<MappingFileInfo>();

        if (!Directory.Exists(mappingsDirectory))
        {
            _logger.LogWarning("Mappings directory not found: {Directory}", mappingsDirectory);
            return files;
        }

        // Discover segment mappings
        var segmentFiles = Directory.GetFiles(Path.Combine(mappingsDirectory, "segments"), "*.csv");
        foreach (var file in segmentFiles)
        {
            var info = await CreateMappingFileInfoAsync(file, MappingFileType.Segment);
            files.Add(info);
        }

        // Discover datatype mappings
        var datatypeFiles = Directory.GetFiles(Path.Combine(mappingsDirectory, "datatypes"), "*.csv");
        foreach (var file in datatypeFiles)
        {
            var info = await CreateMappingFileInfoAsync(file, MappingFileType.DataType);
            files.Add(info);
        }

        // Discover codesystem mappings
        var codesystemFiles = Directory.GetFiles(Path.Combine(mappingsDirectory, "codesystems"), "*.csv");
        foreach (var file in codesystemFiles)
        {
            var info = await CreateMappingFileInfoAsync(file, MappingFileType.CodeSystem);
            files.Add(info);
        }

        _logger.LogInformation("Discovered {Count} mapping files", files.Count);
        return files;
    }

    public async Task<ImportedMappingData> ImportAllMappingsAsync(string mappingsDirectory)
    {
        var data = new ImportedMappingData();
        var files = await DiscoverMappingFilesAsync(mappingsDirectory);

        // Import segment mappings (highest priority for semantic paths)
        var segmentFiles = files.Where(f => f.FileType == MappingFileType.Segment).ToList();
        foreach (var file in segmentFiles)
        {
            try
            {
                var rows = await _parser.ParseSegmentMappingAsync(file.FilePath);
                data.SegmentMappings[file.FileName] = rows;
                _logger.LogInformation("Imported {Count} rows from {File}", rows.Count, file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import segment mapping: {File}", file.FileName);
            }
        }

        // Import datatype mappings
        var datatypeFiles = files.Where(f => f.FileType == MappingFileType.DataType).ToList();
        foreach (var file in datatypeFiles)
        {
            try
            {
                var rows = await _parser.ParseDataTypeMappingAsync(file.FilePath);
                data.DataTypeMappings[file.FileName] = rows;
                _logger.LogInformation("Imported {Count} datatype rows from {File}", rows.Count, file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import datatype mapping: {File}", file.FileName);
            }
        }

        // Import codesystem mappings
        var codesystemFiles = files.Where(f => f.FileType == MappingFileType.CodeSystem).ToList();
        foreach (var file in codesystemFiles)
        {
            try
            {
                var rows = await _parser.ParseCodeSystemMappingAsync(file.FilePath);
                data.CodeSystemMappings[file.FileName] = rows;
                _logger.LogInformation("Imported {Count} codesystem rows from {File}", rows.Count, file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import codesystem mapping: {File}", file.FileName);
            }
        }

        _logger.LogInformation("Import complete: {SegmentFiles} segment files, {DatatypeFiles} datatype files, {CodesystemFiles} codesystem files",
            data.SegmentMappings.Count, data.DataTypeMappings.Count, data.CodeSystemMappings.Count);

        return data;
    }

    private async Task<MappingFileInfo> CreateMappingFileInfoAsync(string filePath, MappingFileType fileType)
    {
        var fileName = Path.GetFileName(filePath);
        var fileInfo = new FileInfo(filePath);

        var (sourceType, targetType) = fileType switch
        {
            MappingFileType.Segment => (_parser.ExtractSegmentTypeFromFileName(fileName), ""),
            MappingFileType.DataType => (_parser.ExtractDataTypeFromFileName(fileName), ""),
            MappingFileType.CodeSystem => (_parser.ExtractCodeSystemFromFileName(fileName), ""),
            _ => ("", "")
        };

        // TODO: Count actual rows by parsing - for now estimate from file size
        var estimatedRows = (int)(fileInfo.Length / 200); // Rough estimate

        return new MappingFileInfo
        {
            FilePath = filePath,
            FileName = fileName,
            FileType = fileType,
            SourceType = sourceType,
            TargetType = targetType,
            RowCount = estimatedRows,
            LastModified = fileInfo.LastWriteTime
        };
    }
}

/// <summary>
/// Container for all imported mapping data from CSV files
/// </summary>
public class ImportedMappingData
{
    public Dictionary<string, IReadOnlyList<SegmentMappingRow>> SegmentMappings { get; } = new();
    public Dictionary<string, IReadOnlyList<DataTypeMappingRow>> DataTypeMappings { get; } = new();
    public Dictionary<string, IReadOnlyList<CodeSystemMappingRow>> CodeSystemMappings { get; } = new();

    public int TotalSegmentRows => SegmentMappings.Values.Sum(rows => rows.Count);
    public int TotalDataTypeRows => DataTypeMappings.Values.Sum(rows => rows.Count);
    public int TotalCodeSystemRows => CodeSystemMappings.Values.Sum(rows => rows.Count);
    public int TotalRows => TotalSegmentRows + TotalDataTypeRows + TotalCodeSystemRows;
}