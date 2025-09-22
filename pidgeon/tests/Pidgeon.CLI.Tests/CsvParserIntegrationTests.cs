using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pidgeon.Core.Infrastructure.Interop;
using Xunit;
using Xunit.Abstractions;

namespace Pidgeon.CLI.Tests;

/// <summary>
/// Integration tests for CSV parser and semantic path generation.
/// Tests with actual HL7 v2-to-FHIR mapping files.
/// </summary>
public class CsvParserIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;

    public CsvParserIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        // Set up DI container
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddSingleton<V2ToFhirMappingParser>();
        services.AddSingleton<MappingImportService>();
        services.AddSingleton<SemanticPathGenerator>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ParsePIDSegmentMapping_ShouldExtractCorrectRows()
    {
        // Arrange
        var parser = _serviceProvider.GetRequiredService<V2ToFhirMappingParser>();
        var pidFile = "data/interop/hl7-fhir-mappings/segments/HL7 Segment - FHIR R4_ PID[Patient] - PID.csv";

        // Act
        var rows = await parser.ParseSegmentMappingAsync(pidFile);

        // Assert
        Assert.NotNull(rows);
        Assert.NotEmpty(rows);

        // Check for expected PID fields
        var pid3 = rows.FirstOrDefault(r => r.HL7Identifier == "PID-3");
        Assert.NotNull(pid3);
        Assert.Equal("Patient Identifier List", pid3.HL7Name);
        Assert.Equal("CX", pid3.HL7DataType);
        Assert.Contains("identifier", pid3.FHIRAttribute?.ToLower() ?? "");

        var pid5 = rows.FirstOrDefault(r => r.HL7Identifier == "PID-5");
        Assert.NotNull(pid5);
        Assert.Equal("Patient Name", pid5.HL7Name);
        Assert.Equal("XPN", pid5.HL7DataType);

        _output.WriteLine($"Successfully parsed {rows.Count} rows from PID mapping");
        _output.WriteLine($"Sample: {pid3.HL7Identifier} ({pid3.HL7Name}) -> {pid3.FHIRAttribute}");
    }

    [Fact]
    public async Task ParseCXDataTypeMapping_ShouldExtractComponents()
    {
        // Arrange
        var parser = _serviceProvider.GetRequiredService<V2ToFhirMappingParser>();
        var cxFile = "data/interop/hl7-fhir-mappings/datatypes/HL7 Data Type - FHIR R4_ CX[Identifier] - Sheet1.csv";

        if (!File.Exists(cxFile))
        {
            _output.WriteLine($"Skipping test - file not found: {cxFile}");
            return;
        }

        // Act
        var rows = await parser.ParseDataTypeMappingAsync(cxFile);

        // Assert
        Assert.NotNull(rows);
        Assert.NotEmpty(rows);

        var cx1 = rows.FirstOrDefault(r => r.HL7Identifier == "CX.1");
        Assert.NotNull(cx1);
        Assert.Equal("ID Number", cx1.HL7Name);
        Assert.Equal("value", cx1.FHIRAttribute);

        _output.WriteLine($"Successfully parsed {rows.Count} rows from CX datatype mapping");
    }

    [Fact]
    public async Task ImportAllMappings_ShouldDiscoverAllFiles()
    {
        // Arrange
        var importService = _serviceProvider.GetRequiredService<MappingImportService>();
        var mappingsDir = "data/interop/hl7-fhir-mappings";

        if (!Directory.Exists(mappingsDir))
        {
            _output.WriteLine($"Skipping test - directory not found: {mappingsDir}");
            return;
        }

        // Act
        var files = await importService.DiscoverMappingFilesAsync(mappingsDir);

        // Assert
        Assert.NotNull(files);
        Assert.NotEmpty(files);

        var segmentFiles = files.Where(f => f.FileType == Core.Domain.Interop.MappingFileType.Segment).ToList();
        var datatypeFiles = files.Where(f => f.FileType == Core.Domain.Interop.MappingFileType.DataType).ToList();
        var codesystemFiles = files.Where(f => f.FileType == Core.Domain.Interop.MappingFileType.CodeSystem).ToList();

        _output.WriteLine($"Discovered {files.Count} total mapping files:");
        _output.WriteLine($"  - Segments: {segmentFiles.Count}");
        _output.WriteLine($"  - DataTypes: {datatypeFiles.Count}");
        _output.WriteLine($"  - CodeSystems: {codesystemFiles.Count}");

        Assert.True(segmentFiles.Count > 0, "Should find segment mapping files");
        Assert.True(datatypeFiles.Count > 0, "Should find datatype mapping files");
        Assert.True(codesystemFiles.Count > 0, "Should find codesystem mapping files");
    }

    [Fact]
    public async Task GenerateSemanticPaths_ShouldCreateTieredPaths()
    {
        // Arrange
        var importService = _serviceProvider.GetRequiredService<MappingImportService>();
        var generator = _serviceProvider.GetRequiredService<SemanticPathGenerator>();
        var mappingsDir = "data/interop/hl7-fhir-mappings";

        if (!Directory.Exists(mappingsDir))
        {
            _output.WriteLine($"Skipping test - directory not found: {mappingsDir}");
            return;
        }

        // Act
        var mappingData = await importService.ImportAllMappingsAsync(mappingsDir);
        var semanticPaths = await generator.GenerateSemanticPathsAsync(mappingData);

        // Assert
        Assert.NotNull(semanticPaths);
        Assert.NotEmpty(semanticPaths);

        var essentialPaths = semanticPaths.Where(p => p.Tier == SemanticPathTier.Essential).ToList();
        var advancedPaths = semanticPaths.Where(p => p.Tier == SemanticPathTier.Advanced).ToList();

        _output.WriteLine($"Generated {semanticPaths.Count} total semantic paths:");
        _output.WriteLine($"  - Essential (Tier 1): {essentialPaths.Count}");
        _output.WriteLine($"  - Advanced (Tier 2): {advancedPaths.Count}");

        // Check for expected essential paths
        Assert.Contains(semanticPaths, p => p.SemanticPath == "patient.mrn");
        Assert.Contains(semanticPaths, p => p.SemanticPath == "patient.name");
        Assert.Contains(semanticPaths, p => p.SemanticPath == "patient.dateOfBirth");

        // Show sample paths
        _output.WriteLine("\nSample Essential Paths:");
        foreach (var path in essentialPaths.Take(5))
        {
            _output.WriteLine($"  - {path.SemanticPath}: {path.Description} (from {path.HL7Field})");
        }
    }

    [Fact]
    public async Task TestSinglePIDFile_DetailedOutput()
    {
        // This test provides detailed output for debugging
        var parser = _serviceProvider.GetRequiredService<V2ToFhirMappingParser>();
        var pidFile = "data/interop/hl7-fhir-mappings/segments/HL7 Segment - FHIR R4_ PID[Patient] - PID.csv";

        if (!File.Exists(pidFile))
        {
            _output.WriteLine($"File not found: {pidFile}");
            return;
        }

        try
        {
            var rows = await parser.ParseSegmentMappingAsync(pidFile);

            _output.WriteLine($"=== PID Segment Parsing Results ===");
            _output.WriteLine($"Total rows parsed: {rows.Count}");
            _output.WriteLine("\nFirst 10 mappings:");

            foreach (var row in rows.Take(10))
            {
                _output.WriteLine($"\n{row.HL7Identifier}: {row.HL7Name}");
                _output.WriteLine($"  HL7 Type: {row.HL7DataType} [{row.HL7CardinalityMin}..{row.HL7CardinalityMax}]");
                _output.WriteLine($"  FHIR Path: {row.FHIRAttribute}");
                _output.WriteLine($"  FHIR Type: {row.FHIRDataType} [{row.FHIRCardinalityMin}..{row.FHIRCardinalityMax}]");
                if (!string.IsNullOrWhiteSpace(row.Condition))
                    _output.WriteLine($"  Condition: {row.Condition}");
                if (!string.IsNullOrWhiteSpace(row.Comments))
                    _output.WriteLine($"  Comments: {row.Comments}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error parsing file: {ex.Message}");
            _output.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}