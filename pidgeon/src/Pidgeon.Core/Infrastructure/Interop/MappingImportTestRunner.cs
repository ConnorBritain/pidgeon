using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Interop;

namespace Pidgeon.Core.Infrastructure.Interop;

/// <summary>
/// Test runner for validating CSV parsing and semantic path generation.
/// Temporary class for development and validation.
/// </summary>
public class MappingImportTestRunner
{
    private readonly V2ToFhirMappingParser _parser;
    private readonly MappingImportService _importService;
    private readonly SemanticPathGenerator _generator;
    private readonly ILogger<MappingImportTestRunner> _logger;

    public MappingImportTestRunner(
        V2ToFhirMappingParser parser,
        MappingImportService importService,
        SemanticPathGenerator generator,
        ILogger<MappingImportTestRunner> logger)
    {
        _parser = parser;
        _importService = importService;
        _generator = generator;
        _logger = logger;
    }

    public async Task<TestResults> RunFullImportTestAsync(string mappingsDirectory)
    {
        var results = new TestResults();

        try
        {
            _logger.LogInformation("Starting full import test for directory: {Directory}", mappingsDirectory);

            // Step 1: Discover files
            var files = await _importService.DiscoverMappingFilesAsync(mappingsDirectory);
            results.FilesDiscovered = files.Count;
            _logger.LogInformation("Discovered {Count} mapping files", files.Count);

            // Step 2: Import all mappings
            var mappingData = await _importService.ImportAllMappingsAsync(mappingsDirectory);
            results.SegmentFilesImported = mappingData.SegmentMappings.Count;
            results.TotalRowsImported = mappingData.TotalRows;
            _logger.LogInformation("Imported {TotalRows} total rows from {SegmentFiles} segment files",
                mappingData.TotalRows, mappingData.SegmentMappings.Count);

            // Step 3: Generate semantic paths
            var semanticPaths = await _generator.GenerateSemanticPathsAsync(mappingData);
            results.SemanticPathsGenerated = semanticPaths.Count;
            results.EssentialPaths = semanticPaths.Count(p => p.Tier == SemanticPathTier.Essential);
            results.AdvancedPaths = semanticPaths.Count(p => p.Tier == SemanticPathTier.Advanced);

            _logger.LogInformation("Generated {Total} semantic paths: {Essential} essential, {Advanced} advanced",
                semanticPaths.Count, results.EssentialPaths, results.AdvancedPaths);

            // Step 4: Show sample essential paths
            var sampleEssential = semanticPaths
                .Where(p => p.Tier == SemanticPathTier.Essential)
                .Take(10)
                .ToList();

            _logger.LogInformation("Sample essential paths:");
            foreach (var path in sampleEssential)
            {
                _logger.LogInformation("  {Path} -> {HL7} -> {FHIR} ({Description})",
                    path.SemanticPath, path.HL7Field, path.FHIRPath, path.Description);
            }

            results.Success = true;
            results.ErrorMessage = null;
        }
        catch (Exception ex)
        {
            results.Success = false;
            results.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Import test failed");
        }

        return results;
    }

    public async Task<ParseTestResults> TestSingleFileParsingAsync(string csvFilePath)
    {
        var results = new ParseTestResults { FilePath = csvFilePath };

        try
        {
            if (csvFilePath.Contains("segments"))
            {
                var rows = await _parser.ParseSegmentMappingAsync(csvFilePath);
                results.RowsParsed = rows.Count;
                results.SampleRows = rows.Take(3).Select(r => $"{r.HL7Identifier} -> {r.FHIRAttribute}").ToList();
            }
            else if (csvFilePath.Contains("datatypes"))
            {
                var rows = await _parser.ParseDataTypeMappingAsync(csvFilePath);
                results.RowsParsed = rows.Count;
                results.SampleRows = rows.Take(3).Select(r => $"{r.HL7Identifier} -> {r.FHIRAttribute}").ToList();
            }
            else if (csvFilePath.Contains("codesystems"))
            {
                var rows = await _parser.ParseCodeSystemMappingAsync(csvFilePath);
                results.RowsParsed = rows.Count;
                results.SampleRows = rows.Take(3).Select(r => $"{r.HL7Code} -> {r.FHIRCode}").ToList();
            }

            results.Success = true;
        }
        catch (Exception ex)
        {
            results.Success = false;
            results.ErrorMessage = ex.Message;
        }

        return results;
    }
}

public class TestResults
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int FilesDiscovered { get; set; }
    public int SegmentFilesImported { get; set; }
    public int TotalRowsImported { get; set; }
    public int SemanticPathsGenerated { get; set; }
    public int EssentialPaths { get; set; }
    public int AdvancedPaths { get; set; }
}

public class ParseTestResults
{
    public string FilePath { get; set; } = "";
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int RowsParsed { get; set; }
    public List<string> SampleRows { get; set; } = new();
}