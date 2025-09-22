using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pidgeon.Core.Infrastructure.Interop;

namespace CsvParserTestRunner;

/// <summary>
/// Simple standalone test for CSV parser functionality.
/// </summary>
public class SimpleCsvParserTest
{
    public static async Task RunTestAsync()
    {
        Console.WriteLine("=== CSV Parser Integration Test ===\n");

        // Set up minimal DI
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSingleton<V2ToFhirMappingParser>();
        services.AddSingleton<MappingImportService>();
        services.AddSingleton<SemanticPathGenerator>();

        var serviceProvider = services.BuildServiceProvider();
        var parser = serviceProvider.GetRequiredService<V2ToFhirMappingParser>();
        var importService = serviceProvider.GetRequiredService<MappingImportService>();
        var generator = serviceProvider.GetRequiredService<SemanticPathGenerator>();

        try
        {
            // Test 1: Parse a single PID file
            await TestSinglePIDFile(parser);

            // Test 2: Discover all mapping files
            await TestFileDiscovery(importService);

            // Test 3: Generate semantic paths
            await TestSemanticPathGeneration(importService, generator);

            Console.WriteLine("\n✅ All tests passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private static async Task TestSinglePIDFile(V2ToFhirMappingParser parser)
    {
        Console.WriteLine("Test 1: Parsing PID segment mapping...");

        var pidFile = "../../data/interop/hl7-fhir-mappings/segments/HL7 Segment - FHIR R4_ PID[Patient] - PID.csv";

        if (!File.Exists(pidFile))
        {
            Console.WriteLine($"⚠️  File not found: {pidFile}");
            Console.WriteLine("   Make sure you've run the interop directory cleanup.");
            return;
        }

        var rows = await parser.ParseSegmentMappingAsync(pidFile);
        Console.WriteLine($"   Parsed {rows.Count} rows from PID mapping");

        // Look for key fields
        var pid3 = rows.FirstOrDefault(r => r.HL7Identifier == "PID-3");
        var pid5 = rows.FirstOrDefault(r => r.HL7Identifier == "PID-5");
        var pid7 = rows.FirstOrDefault(r => r.HL7Identifier == "PID-7");

        if (pid3 != null)
            Console.WriteLine($"   ✓ PID-3 (Patient ID): {pid3.HL7Name} -> {pid3.FHIRAttribute}");
        else
            Console.WriteLine($"   ❌ PID-3 not found");

        if (pid5 != null)
            Console.WriteLine($"   ✓ PID-5 (Patient Name): {pid5.HL7Name} -> {pid5.FHIRAttribute}");
        else
            Console.WriteLine($"   ❌ PID-5 not found");

        if (pid7 != null)
            Console.WriteLine($"   ✓ PID-7 (DOB): {pid7.HL7Name} -> {pid7.FHIRAttribute}");
        else
            Console.WriteLine($"   ❌ PID-7 not found");

        Console.WriteLine();
    }

    private static async Task TestFileDiscovery(MappingImportService importService)
    {
        Console.WriteLine("Test 2: Discovering mapping files...");

        var mappingsDir = "../../data/interop/hl7-fhir-mappings";

        if (!Directory.Exists(mappingsDir))
        {
            Console.WriteLine($"⚠️  Directory not found: {mappingsDir}");
            return;
        }

        var files = await importService.DiscoverMappingFilesAsync(mappingsDir);

        var segmentFiles = files.Where(f => f.FileType == Pidgeon.Core.Domain.Interop.MappingFileType.Segment).Count();
        var datatypeFiles = files.Where(f => f.FileType == Pidgeon.Core.Domain.Interop.MappingFileType.DataType).Count();
        var codesystemFiles = files.Where(f => f.FileType == Pidgeon.Core.Domain.Interop.MappingFileType.CodeSystem).Count();

        Console.WriteLine($"   Discovered {files.Count} total files:");
        Console.WriteLine($"   - Segments: {segmentFiles}");
        Console.WriteLine($"   - DataTypes: {datatypeFiles}");
        Console.WriteLine($"   - CodeSystems: {codesystemFiles}");
        Console.WriteLine();
    }

    private static async Task TestSemanticPathGeneration(MappingImportService importService, SemanticPathGenerator generator)
    {
        Console.WriteLine("Test 3: Generating semantic paths...");

        var mappingsDir = "../../data/interop/hl7-fhir-mappings";

        if (!Directory.Exists(mappingsDir))
        {
            Console.WriteLine($"⚠️  Directory not found: {mappingsDir}");
            return;
        }

        // Import a subset of files to avoid overwhelming output
        var mappingData = await importService.ImportAllMappingsAsync(mappingsDir);
        Console.WriteLine($"   Imported {mappingData.TotalRows} total rows from mapping files");

        var semanticPaths = await generator.GenerateSemanticPathsAsync(mappingData);

        var essentialPaths = semanticPaths.Where(p => p.Tier == Pidgeon.Core.Infrastructure.Interop.SemanticPathTier.Essential).ToList();
        var advancedPaths = semanticPaths.Where(p => p.Tier == Pidgeon.Core.Infrastructure.Interop.SemanticPathTier.Advanced).ToList();

        Console.WriteLine($"   Generated {semanticPaths.Count} semantic paths:");
        Console.WriteLine($"   - Essential (Tier 1): {essentialPaths.Count}");
        Console.WriteLine($"   - Advanced (Tier 2): {advancedPaths.Count}");

        Console.WriteLine("\n   Sample Essential Paths:");
        foreach (var path in essentialPaths.Take(8))
        {
            Console.WriteLine($"     {path.SemanticPath}: {path.Description}");
        }

        Console.WriteLine();
    }
}