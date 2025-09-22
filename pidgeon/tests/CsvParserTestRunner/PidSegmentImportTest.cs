using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Pidgeon.Core.Infrastructure.Interop;

namespace CsvParserTestRunner;

public static class PidSegmentImportTest
{
    public static async Task RunTestAsync()
    {
        Console.WriteLine("=== PID SEGMENT JSON IMPORT & DATABASE INTEGRATION TEST ===");

        try
        {
            // Test database path
            var testDbPath = Path.Combine(Path.GetTempPath(), $"pidgeon_test_{DateTime.Now:yyyyMMdd_HHmmss}.db");

            Console.WriteLine($"Creating test database: {testDbPath}");

            // Initialize database with complete schema
            using var db = new SqliteInteropDatabase(testDbPath);
            await db.InitializeAsync();

            Console.WriteLine("✅ Database initialized with complete schema");

            // Import HL7 v2.3 structure data
            var projectPath = FindProjectRoot();
            var hl7DataPath = Path.Combine(projectPath, "data", "standards", "hl7v23");

            if (!Directory.Exists(hl7DataPath))
            {
                Console.WriteLine($"❌ HL7 data directory not found: {hl7DataPath}");
                return;
            }

            // Create simple console logger for testing
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<HL7JsonImportService>();

            // Create connection for import service
            using var importConnection = new SqliteConnection($"Data Source={testDbPath}");
            await importConnection.OpenAsync();

            var importService = new HL7JsonImportService(importConnection, logger);
            Console.WriteLine($"Importing HL7 v2.3 data from: {hl7DataPath}");

            var results = await importService.ImportAllAsync(hl7DataPath);

            Console.WriteLine($"✅ Import Results:");
            Console.WriteLine($"   - Segments: {results.SegmentsImported}");
            Console.WriteLine($"   - Data Types: {results.DataTypesImported}");
            Console.WriteLine($"   - Code Tables: {results.CodeTablesImported}");
            Console.WriteLine($"   - Trigger Events: {results.TriggerEventsImported}");
            Console.WriteLine($"   - Errors: {results.Errors.Count}");

            // Test 1: Verify PID segment structure
            await TestPidSegmentStructure(testDbPath);

            // Test 2: Verify PID field definitions
            await TestPidFieldDefinitions(testDbPath);

            // Test 3: Test semantic path integration
            await TestSemanticPathIntegration(testDbPath);

            // Test 4: Test unified lookups (HL7 structure + semantic paths)
            await TestUnifiedLookups(testDbPath);

            // Clean up
            File.Delete(testDbPath);
            Console.WriteLine("✅ Test database cleaned up");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private static async Task TestPidSegmentStructure(string dbPath)
    {
        Console.WriteLine("\n--- Testing PID Segment Structure ---");

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        // Query PID segment definition
        var cmd = new SqliteCommand(@"
            SELECT s.code, s.name, s.description
            FROM segments s
            WHERE s.code = 'PID'", connection);

        using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var code = reader.GetString("code");
            var name = reader.GetString("name");
            var description = reader.IsDBNull("description") ? "N/A" : reader.GetString("description");

            Console.WriteLine($"✅ PID Segment Found:");
            Console.WriteLine($"   Code: {code}");
            Console.WriteLine($"   Name: {name}");
            Console.WriteLine($"   Description: {description}");
        }
        else
        {
            Console.WriteLine("❌ PID segment not found in database");
        }
    }

    private static async Task TestPidFieldDefinitions(string dbPath)
    {
        Console.WriteLine("\n--- Testing PID Field Definitions ---");

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        // Query PID fields (first 5 fields for brevity)
        var cmd = new SqliteCommand(@"
            SELECT f.position, f.field_description, dt.code as data_type, f.optionality, f.length
            FROM fields f
            JOIN segments s ON f.segment_id = s.id
            LEFT JOIN data_types dt ON f.data_type_id = dt.id
            WHERE s.code = 'PID' AND f.position <= 5
            ORDER BY f.position", connection);

        using var reader = await cmd.ExecuteReaderAsync();

        var fieldCount = 0;
        while (await reader.ReadAsync())
        {
            fieldCount++;
            var position = reader.GetInt32("position");
            var description = reader.GetString("field_description");
            var dataType = reader.IsDBNull("data_type") ? "Unknown" : reader.GetString("data_type");
            var optionality = reader.IsDBNull("optionality") ? "Unknown" : reader.GetString("optionality");
            var maxLength = reader.IsDBNull("length") ? "N/A" : reader.GetInt32("length").ToString();

            Console.WriteLine($"   PID.{position}: {description} ({dataType}, {optionality}, max: {maxLength})");
        }

        if (fieldCount > 0)
        {
            Console.WriteLine($"✅ Found {fieldCount} PID field definitions");
        }
        else
        {
            Console.WriteLine("❌ No PID field definitions found");
        }
    }

    private static async Task TestSemanticPathIntegration(string dbPath)
    {
        Console.WriteLine("\n--- Testing Semantic Path Integration ---");

        using var db = new SqliteInteropDatabase(dbPath);

        // Test essential semantic paths (existing functionality)
        var essentialPaths = await db.GetEssentialPathsAsync();
        Console.WriteLine($"✅ Essential semantic paths: {essentialPaths.Count}");

        // Show patient-related paths
        var patientPaths = essentialPaths.Where(p => p.Category.Contains("patient", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var path in patientPaths.Take(3))
        {
            Console.WriteLine($"   {path.Path}: {path.Description}");
            if (!string.IsNullOrEmpty(path.HL7Field))
            {
                Console.WriteLine($"      → HL7: {path.HL7Field}");
            }
        }
    }

    private static async Task TestUnifiedLookups(string dbPath)
    {
        Console.WriteLine("\n--- Testing Unified Lookups (Structure + Semantic Paths) ---");

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        // Test unified query: Find HL7 field structure for semantic path
        var cmd = new SqliteCommand(@"
            SELECT
                sp.semantic_path,
                sp.description as semantic_description,
                sp.hl7_field,
                s.code as segment_code,
                s.name as segment_name,
                f.position,
                f.field_description,
                dt.code as data_type
            FROM semantic_paths sp
            LEFT JOIN segments s ON sp.hl7_field LIKE s.code || '%'
            LEFT JOIN fields f ON s.id = f.segment_id AND sp.hl7_field = s.code || '-' || f.position
            LEFT JOIN data_types dt ON f.data_type_id = dt.id
            WHERE sp.semantic_path LIKE '%patient%'
            AND sp.hl7_field IS NOT NULL
            LIMIT 3", connection);

        using var reader = await cmd.ExecuteReaderAsync();

        var unifiedCount = 0;
        while (await reader.ReadAsync())
        {
            unifiedCount++;
            var semanticPath = reader.GetString("semantic_path");
            var semanticDesc = reader.GetString("semantic_description");
            var hl7Field = reader.IsDBNull("hl7_field") ? "N/A" : reader.GetString("hl7_field");
            var segmentCode = reader.IsDBNull("segment_code") ? "N/A" : reader.GetString("segment_code");
            var fieldDesc = reader.IsDBNull("field_description") ? "N/A" : reader.GetString("field_description");
            var dataType = reader.IsDBNull("data_type") ? "N/A" : reader.GetString("data_type");

            Console.WriteLine($"   Semantic: {semanticPath}");
            Console.WriteLine($"      Description: {semanticDesc}");
            Console.WriteLine($"      HL7 Field: {hl7Field} → {fieldDesc} ({dataType})");
            Console.WriteLine($"      Segment: {segmentCode}");
            Console.WriteLine();
        }

        if (unifiedCount > 0)
        {
            Console.WriteLine($"✅ Unified lookups working: {unifiedCount} examples found");
            Console.WriteLine("✅ Database successfully integrates HL7 structure + semantic paths");
        }
        else
        {
            Console.WriteLine("⚠️  Unified lookups need adjustment - relationship mapping incomplete");
        }
    }

    private static string FindProjectRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null)
        {
            // Look for pidgeon root (has Pidgeon.sln file)
            if (File.Exists(Path.Combine(currentDir, "Pidgeon.sln")))
            {
                return currentDir;
            }

            // Keep going up to find pidgeon directory
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        // Fallback: look for pidgeon directory in current path
        var pathParts = Directory.GetCurrentDirectory().Split(Path.DirectorySeparatorChar);
        for (int i = pathParts.Length - 1; i >= 0; i--)
        {
            if (pathParts[i] == "pidgeon")
            {
                var pidgeonPath = string.Join(Path.DirectorySeparatorChar.ToString(), pathParts.Take(i + 1));
                if (Directory.Exists(pidgeonPath))
                {
                    return pidgeonPath;
                }
            }
        }

        return Directory.GetCurrentDirectory();
    }
}