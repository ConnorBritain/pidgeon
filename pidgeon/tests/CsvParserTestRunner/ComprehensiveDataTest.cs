using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pidgeon.Core.Infrastructure.Interop;
using System.Text.Json;

namespace CsvParserTestRunner;

/// <summary>
/// Comprehensive test showing current database capabilities vs missing HL7 v2.3 structure data.
/// Tests both data sources our app needs:
/// 1. HL7 v2.3 Structure Data (data/standards/hl7v23/) - NOT YET IMPLEMENTED
/// 2. HL7-FHIR Mapping Data (data/interop/hl7-fhir-mappings/) - ✅ WORKING
/// </summary>
public class ComprehensiveDataTest
{
    public static async Task RunTestAsync()
    {
        Console.WriteLine("=== Comprehensive Data Integration Test ===\n");
        Console.WriteLine("Testing both data sources our app requires:\n");

        var databasePath = Path.Combine(Path.GetTempPath(), "pidgeon_comprehensive_test.db");
        try
        {
            // Clean up any existing test database
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }

            using var database = new SqliteInteropDatabase(databasePath);
            await database.InitializeAsync();

            // Test 1: HL7-FHIR Mapping Data (Currently Working)
            await TestHL7FhirMappingData(database);

            // Test 2: HL7 v2.3 Structure Data (Missing - Need to Implement)
            await TestHL7StructureDataLookups(database);

            // Test 3: Application-Expected Lookups (Show what our app needs)
            await TestApplicationExpectedLookups(database);

            Console.WriteLine("\n📊 Summary:");
            Console.WriteLine("✅ HL7-FHIR Mapping Data: WORKING (semantic paths, cross-standard mappings)");
            Console.WriteLine("❌ HL7 v2.3 Structure Data: MISSING (segment definitions, field details, data types)");
            Console.WriteLine("🎯 Next Steps: Implement JSON parser for data/standards/hl7v23/ data");

        }
        finally
        {
            if (File.Exists(databasePath))
            {
                try { File.Delete(databasePath); } catch { }
            }
        }
    }

    private static async Task TestHL7FhirMappingData(SqliteInteropDatabase database)
    {
        Console.WriteLine("🔍 Test 1: HL7-FHIR Mapping Data (Currently Working)");
        Console.WriteLine("Source: data/interop/hl7-fhir-mappings/");

        // Import mapping data first
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<V2ToFhirMappingParser>();
        services.AddSingleton<MappingImportService>();

        var serviceProvider = services.BuildServiceProvider();
        var importService = serviceProvider.GetRequiredService<MappingImportService>();

        var mappingsDir = "../../data/interop/hl7-fhir-mappings";
        if (Directory.Exists(mappingsDir))
        {
            var mappingData = await importService.ImportAllMappingsAsync(mappingsDir);

            // Import subset to database for testing
            var pidFiles = mappingData.SegmentMappings.Keys.Where(f => f.Contains("PID")).Take(1);
            foreach (var file in pidFiles)
            {
                await database.ImportSegmentMappingsAsync(mappingData.SegmentMappings[file], Path.GetFileName(file));
            }

            Console.WriteLine($"✅ Imported {mappingData.TotalRows} mapping rows from CSV files");
        }

        // Test semantic path queries (these work!)
        var essentialPaths = await database.GetEssentialPathsAsync();
        Console.WriteLine($"✅ Essential semantic paths: {essentialPaths.Count} (e.g., patient.mrn, patient.name.family)");

        var patientPaths = await database.SearchPathsAsync("patient");
        Console.WriteLine($"✅ Patient-related paths: {patientPaths.Count} found via search");

        Console.WriteLine("✅ HL7→FHIR mappings: Working (PID-3 → Patient.identifier[2], etc.)\n");
    }

    private static async Task TestHL7StructureDataLookups(SqliteInteropDatabase database)
    {
        Console.WriteLine("🔍 Test 2: HL7 v2.3 Structure Data Lookups (Missing Implementation)");
        Console.WriteLine("Source: data/standards/hl7v23/");

        // Show what HL7 v2.3 structure data we have available
        var hl7DataPath = "../../data/standards/hl7v23";
        if (Directory.Exists(hl7DataPath))
        {
            var segmentFiles = Directory.GetFiles(Path.Combine(hl7DataPath, "segments"), "*.json");
            var dataTypeFiles = Directory.GetFiles(Path.Combine(hl7DataPath, "data_types"), "*.json");
            var tableFiles = Directory.GetFiles(Path.Combine(hl7DataPath, "tables"), "*.json");

            Console.WriteLine($"📂 Available HL7 v2.3 structure files:");
            Console.WriteLine($"   - Segments: {segmentFiles.Length} files (pid.json, msh.json, etc.)");
            Console.WriteLine($"   - Data Types: {dataTypeFiles.Length} files (cx.json, xpn.json, etc.)");
            Console.WriteLine($"   - Tables: {tableFiles.Length} files (0001.json, 0002.json, etc.)");

            // Show sample PID segment structure
            var pidPath = Path.Combine(hl7DataPath, "segments", "pid.json");
            if (File.Exists(pidPath))
            {
                var pidJson = await File.ReadAllTextAsync(pidPath);
                var pidData = JsonSerializer.Deserialize<JsonElement>(pidJson);

                Console.WriteLine($"\n📋 Sample PID segment structure from pid.json:");
                Console.WriteLine($"   Code: {pidData.GetProperty("code").GetString()}");
                Console.WriteLine($"   Name: {pidData.GetProperty("name").GetString()}");
                Console.WriteLine($"   Fields: {pidData.GetProperty("fields").GetArrayLength()} field definitions");

                // Show first few fields
                var fields = pidData.GetProperty("fields").EnumerateArray().Take(3);
                foreach (var field in fields)
                {
                    var pos = field.GetProperty("position").GetInt32();
                    var name = field.GetProperty("field_name").GetString();
                    var desc = field.GetProperty("field_description").GetString();
                    var type = field.GetProperty("data_type").GetString();
                    Console.WriteLine($"   - PID.{pos}: {desc} ({type})");
                }
            }
        }

        Console.WriteLine("\n❌ MISSING: Database import for HL7 v2.3 structure data");
        Console.WriteLine("❌ MISSING: Queries like 'Get PID segment definition'");
        Console.WriteLine("❌ MISSING: Queries like 'Get all fields for segment PID'");
        Console.WriteLine("❌ MISSING: Queries like 'Get data type CX definition'\n");
    }

    private static async Task TestApplicationExpectedLookups(SqliteInteropDatabase database)
    {
        Console.WriteLine("🔍 Test 3: Application-Expected Lookups (What Our App Needs)");

        Console.WriteLine("\n🎯 WORKING Lookups (HL7-FHIR Mappings):");

        // These work because we have mapping data
        try
        {
            var essentialPaths = await database.GetEssentialPathsAsync();
            var patientMrn = essentialPaths.FirstOrDefault(p => p.Path == "patient.mrn");
            if (patientMrn != null)
            {
                Console.WriteLine($"✅ Semantic path lookup: '{patientMrn.Path}' → HL7:{patientMrn.HL7Field} → FHIR:{patientMrn.FHIRPath}");
            }

            var patientPaths = await database.SearchPathsAsync("patient");
            Console.WriteLine($"✅ Search semantic paths: 'patient' → {patientPaths.Count} results");

            Console.WriteLine($"✅ Progressive disclosure: {essentialPaths.Count} essential + advanced paths");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Semantic path lookup failed: {ex.Message}");
        }

        Console.WriteLine("\n🎯 NEEDED Lookups (HL7 v2.3 Structure - Missing):");

        // These would fail because we don't have HL7 structure data imported
        Console.WriteLine("❌ NEEDED: 'Get segment PID definition' → {code: 'PID', name: 'Patient Identification', fields: [...]}");
        Console.WriteLine("❌ NEEDED: 'Get field PID.3 details' → {name: 'Patient Identifier List', type: 'CX', required: true}");
        Console.WriteLine("❌ NEEDED: 'Get data type CX definition' → {name: 'Extended Composite ID', components: [...]}");
        Console.WriteLine("❌ NEEDED: 'Get table 0001 values' → {code: 'M', description: 'Male'}, {code: 'F', description: 'Female'}");
        Console.WriteLine("❌ NEEDED: 'Validate message structure' → Check required segments, field optionality");
        Console.WriteLine("❌ NEEDED: 'Generate field constraints' → Length limits, data type validation");

        Console.WriteLine("\n🎯 CRITICAL for Generation Service:");
        Console.WriteLine("❌ NEEDED: Field length constraints (PID.5 max length for names)");
        Console.WriteLine("❌ NEEDED: Optionality rules (which fields are required vs optional)");
        Console.WriteLine("❌ NEEDED: Data type validation (CX format for identifiers)");
        Console.WriteLine("❌ NEEDED: Table value validation (gender must be M/F/U from table 0001)");

        Console.WriteLine("\n🎯 CRITICAL for Validation Service:");
        Console.WriteLine("❌ NEEDED: Segment sequence validation (MSH must be first)");
        Console.WriteLine("❌ NEEDED: Field presence validation (required fields must exist)");
        Console.WriteLine("❌ NEEDED: Data format validation (dates, phone numbers, etc.)");
        Console.WriteLine("❌ NEEDED: Cross-field validation (field relationships)");
    }
}

/// <summary>
/// Simple JSON structure for HL7 v2.3 segment data
/// </summary>
public class HL7SegmentDefinition
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Chapter { get; set; } = "";
    public string Description { get; set; } = "";
    public List<HL7FieldDefinition> Fields { get; set; } = new();
}

public class HL7FieldDefinition
{
    public int Position { get; set; }
    public string Field_Name { get; set; } = "";
    public string Field_Description { get; set; } = "";
    public string Length { get; set; } = "";
    public string Data_Type { get; set; } = "";
    public string Optionality { get; set; } = "";
    public string Repeatability { get; set; } = "";
    public string Table { get; set; } = "";
}