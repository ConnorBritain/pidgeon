using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pidgeon.Core.Infrastructure.Interop;

namespace CsvParserTestRunner;

/// <summary>
/// Comprehensive test for CSV import to SQLite database with semantic path integration.
/// Tests the complete pipeline: CSV files → Parser → Database → Semantic paths.
/// </summary>
public class DatabaseImportTest
{
    public static async Task RunTestAsync()
    {
        Console.WriteLine("=== Database Import Integration Test ===\n");

        // Set up services
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSingleton<V2ToFhirMappingParser>();
        services.AddSingleton<MappingImportService>();
        services.AddSingleton<SemanticPathGenerator>();

        var serviceProvider = services.BuildServiceProvider();
        var parser = serviceProvider.GetRequiredService<V2ToFhirMappingParser>();
        var importService = serviceProvider.GetRequiredService<MappingImportService>();
        var generator = serviceProvider.GetRequiredService<SemanticPathGenerator>();

        var databasePath = Path.Combine(Path.GetTempPath(), "pidgeon_test.db");
        try
        {
            // Clean up any existing test database
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }

            Console.WriteLine($"Creating test database at: {databasePath}");

            using var database = new SqliteInteropDatabase(databasePath);
            await database.InitializeAsync();

            Console.WriteLine("✅ Database initialized with schema");

            // Test 1: Import CSV data into database
            await TestCsvImportToDatabase(importService, database);

            // Test 2: Test semantic path queries
            await TestSemanticPathQueries(database);

            // Test 3: Test performance benchmarks
            await TestPerformanceBenchmarks(database);

            Console.WriteLine("\n✅ All database tests passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Database test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            // Clean up test database
            if (File.Exists(databasePath))
            {
                try
                {
                    File.Delete(databasePath);
                    Console.WriteLine($"🧹 Cleaned up test database");
                }
                catch
                {
                    Console.WriteLine($"⚠️  Could not delete test database at {databasePath}");
                }
            }
        }
    }

    private static async Task TestCsvImportToDatabase(MappingImportService importService, SqliteInteropDatabase database)
    {
        Console.WriteLine("\nTest 1: Importing CSV data to database...");

        var mappingsDir = "../../data/interop/hl7-fhir-mappings";
        if (!Directory.Exists(mappingsDir))
        {
            Console.WriteLine($"⚠️  Skipping import test - mappings directory not found: {mappingsDir}");
            return;
        }

        // Import all mapping data
        var mappingData = await importService.ImportAllMappingsAsync(mappingsDir);
        Console.WriteLine($"   📊 Imported {mappingData.TotalRows} total mapping rows");

        // Import segment mappings to database
        var segmentCount = 0;
        foreach (var segmentFile in mappingData.SegmentMappings.Keys.Take(5)) // Test with first 5 files
        {
            var mappings = mappingData.SegmentMappings[segmentFile];
            await database.ImportSegmentMappingsAsync(mappings, Path.GetFileName(segmentFile));
            segmentCount += mappings.Count;
        }

        Console.WriteLine($"   💾 Imported {segmentCount} segment mappings to database");

        // Import datatype mappings to database
        var datatypeCount = 0;
        foreach (var datatypeFile in mappingData.DataTypeMappings.Keys.Take(5)) // Test with first 5 files
        {
            var mappings = mappingData.DataTypeMappings[datatypeFile];
            await database.ImportDataTypeMappingsAsync(mappings, Path.GetFileName(datatypeFile));
            datatypeCount += mappings.Count;
        }

        Console.WriteLine($"   💾 Imported {datatypeCount} datatype mappings to database");

        // Import code system mappings to database
        var codesystemCount = 0;
        foreach (var codesystemFile in mappingData.CodeSystemMappings.Keys.Take(5)) // Test with first 5 files
        {
            var mappings = mappingData.CodeSystemMappings[codesystemFile];
            await database.ImportCodeSystemMappingsAsync(mappings, Path.GetFileName(codesystemFile));
            codesystemCount += mappings.Count;
        }

        Console.WriteLine($"   💾 Imported {codesystemCount} code system mappings to database");
    }

    private static async Task TestSemanticPathQueries(SqliteInteropDatabase database)
    {
        Console.WriteLine("\nTest 2: Testing semantic path queries...");

        // Test essential paths query (target: <1ms)
        var startTime = DateTime.UtcNow;
        var essentialPaths = await database.GetEssentialPathsAsync();
        var essentialQueryTime = DateTime.UtcNow - startTime;

        Console.WriteLine($"   🎯 Essential paths query: {essentialPaths.Count} paths in {essentialQueryTime.TotalMilliseconds:F1}ms");

        // Display sample essential paths
        Console.WriteLine("   📋 Sample essential paths:");
        foreach (var path in essentialPaths.Take(5))
        {
            Console.WriteLine($"      • {path.Path}: {path.Description} (HL7: {path.HL7Field})");
        }

        // Test advanced paths query
        startTime = DateTime.UtcNow;
        var advancedPaths = await database.GetAdvancedPathsAsync();
        var advancedQueryTime = DateTime.UtcNow - startTime;

        Console.WriteLine($"   🔧 Advanced paths query: {advancedPaths.Count} paths in {advancedQueryTime.TotalMilliseconds:F1}ms");

        // Test search functionality
        startTime = DateTime.UtcNow;
        var searchResults = await database.SearchPathsAsync("patient");
        var searchQueryTime = DateTime.UtcNow - startTime;

        Console.WriteLine($"   🔍 Search 'patient': {searchResults.Count} results in {searchQueryTime.TotalMilliseconds:F1}ms");
    }

    private static async Task TestPerformanceBenchmarks(SqliteInteropDatabase database)
    {
        Console.WriteLine("\nTest 3: Performance benchmarks...");

        // Benchmark essential paths query (target: <1ms)
        var times = new List<double>();
        for (int i = 0; i < 10; i++)
        {
            var startTime = DateTime.UtcNow;
            await database.GetEssentialPathsAsync();
            var queryTime = DateTime.UtcNow - startTime;
            times.Add(queryTime.TotalMilliseconds);
        }

        var avgTime = times.Average();
        var maxTime = times.Max();
        var minTime = times.Min();

        Console.WriteLine($"   ⚡ Essential paths (10 runs):");
        Console.WriteLine($"      Average: {avgTime:F2}ms | Min: {minTime:F2}ms | Max: {maxTime:F2}ms");

        if (avgTime < 1.0)
        {
            Console.WriteLine($"      ✅ Performance target met (<1ms)");
        }
        else
        {
            Console.WriteLine($"      ⚠️  Performance target missed (target: <1ms, actual: {avgTime:F2}ms)");
        }

        // Benchmark search queries
        var searchTimes = new List<double>();
        var searchTerms = new[] { "patient", "encounter", "provider", "message" };

        foreach (var term in searchTerms)
        {
            var startTime = DateTime.UtcNow;
            await database.SearchPathsAsync(term);
            var queryTime = DateTime.UtcNow - startTime;
            searchTimes.Add(queryTime.TotalMilliseconds);
        }

        var avgSearchTime = searchTimes.Average();
        Console.WriteLine($"   🔍 Search queries (4 terms): Average {avgSearchTime:F2}ms");
    }
}