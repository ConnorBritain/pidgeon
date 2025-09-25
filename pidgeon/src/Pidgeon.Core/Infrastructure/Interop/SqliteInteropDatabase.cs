using System.Data;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Pidgeon.Core.Domain.Interop;

namespace Pidgeon.Core.Infrastructure.Interop;

/// <summary>
/// SQLite database service for HL7 interoperability data and semantic paths.
/// Provides high-performance access to HL7 structure, FHIR mappings, and semantic paths.
/// Target: <1ms for semantic path lookups, <10ms for complex structure queries.
/// </summary>
public class SqliteInteropDatabase : IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection _connection;
    private bool _disposed = false;

    public SqliteInteropDatabase(string databasePath)
    {
        _connectionString = $"Data Source={databasePath};Cache=Shared;";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        // Enable WAL mode and optimize for performance
        ExecuteNonQuery("PRAGMA journal_mode = WAL");
        ExecuteNonQuery("PRAGMA synchronous = NORMAL");
        ExecuteNonQuery("PRAGMA cache_size = 10000");
        ExecuteNonQuery("PRAGMA foreign_keys = ON");
    }

    /// <summary>
    /// Initializes database with schema if it doesn't exist.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Check if database is already initialized
        var tableCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'");

        if (tableCount == 0)
        {
            await CreateSchemaAsync();
        }
    }

    /// <summary>
    /// Creates database schema from embedded schema.sql file.
    /// </summary>
    private async Task CreateSchemaAsync()
    {
        var schemaSql = await LoadSchemaFromEmbeddedResourcesAsync();
        if (string.IsNullOrEmpty(schemaSql))
        {
            throw new FileNotFoundException("Schema file not found in embedded resources. Looking for data.schema.sql");
        }

        await ExecuteNonQueryAsync(schemaSql);
    }

    /// <summary>
    /// Loads schema SQL from embedded resources or file system fallback.
    /// </summary>
    private async Task<string?> LoadSchemaFromEmbeddedResourcesAsync()
    {
        // Try to find embedded schema from data assembly
        var dataAssembly = FindDataAssembly();
        if (dataAssembly != null)
        {
            var resourceName = "data.schema.sql";
            try
            {
                using var stream = dataAssembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    return await reader.ReadToEndAsync();
                }
            }
            catch
            {
                // Continue to fallback
            }
        }

        // Fallback to file system for development
        return await LoadSchemaFromFileSystemFallbackAsync();
    }

    /// <summary>
    /// Fallback method to load schema from file system during development.
    /// </summary>
    private async Task<string?> LoadSchemaFromFileSystemFallbackAsync()
    {
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "data", "schema.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "data", "schema.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "data", "schema.sql")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path);
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the assembly containing embedded data resources.
    /// </summary>
    private static Assembly? FindDataAssembly()
    {
        var resourcePrefix = "data.";
        return AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => HasEmbeddedResources(a, resourcePrefix));
    }

    /// <summary>
    /// Checks if embedded resources exist for the specified resource prefix.
    /// </summary>
    private static bool HasEmbeddedResources(Assembly assembly, string resourcePrefix)
    {
        var resourceNames = assembly.GetManifestResourceNames();
        return resourceNames.Any(name => name.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase));
    }

    // ============================================================================
    // SEMANTIC PATH OPERATIONS (Target: <1ms)
    // ============================================================================

    /// <summary>
    /// Gets essential semantic paths for progressive disclosure UI.
    /// Target performance: <1ms for 25-30 essential paths.
    /// </summary>
    public async Task<IReadOnlyList<SemanticPath>> GetEssentialPathsAsync()
    {
        var sql = @"
            SELECT semantic_path, category, description, hl7_field, fhir_path,
                   data_type_hint, example_value, usage_priority
            FROM semantic_paths
            WHERE tier = 'essential'
            ORDER BY usage_priority ASC";

        return await QueryAsync(sql, row => new SemanticPath
        {
            Path = row.GetString("semantic_path"),
            Category = row.GetString("category"),
            Description = row.GetString("description"),
            HL7Field = row.IsDBNull("hl7_field") ? null : row.GetString("hl7_field"),
            FHIRPath = row.IsDBNull("fhir_path") ? null : row.GetString("fhir_path"),
            DataTypeHint = row.IsDBNull("data_type_hint") ? null : row.GetString("data_type_hint"),
            ExampleValue = row.IsDBNull("example_value") ? null : row.GetString("example_value"),
            Priority = row.GetInt32("usage_priority"),
            Tier = SemanticPathTier.Essential
        });
    }

    /// <summary>
    /// Gets advanced semantic paths for power users.
    /// </summary>
    public async Task<IReadOnlyList<SemanticPath>> GetAdvancedPathsAsync()
    {
        var sql = @"
            SELECT semantic_path, category, description, hl7_field, fhir_path,
                   data_type_hint, example_value, usage_priority
            FROM semantic_paths
            WHERE tier = 'advanced'
            ORDER BY category, semantic_path";

        return await QueryAsync(sql, row => new SemanticPath
        {
            Path = row.GetString("semantic_path"),
            Category = row.GetString("category"),
            Description = row.GetString("description"),
            HL7Field = row.IsDBNull("hl7_field") ? null : row.GetString("hl7_field"),
            FHIRPath = row.IsDBNull("fhir_path") ? null : row.GetString("fhir_path"),
            DataTypeHint = row.IsDBNull("data_type_hint") ? null : row.GetString("data_type_hint"),
            ExampleValue = row.IsDBNull("example_value") ? null : row.GetString("example_value"),
            Priority = row.GetInt32("usage_priority"),
            Tier = SemanticPathTier.Advanced
        });
    }

    /// <summary>
    /// Searches semantic paths by keyword with fuzzy matching.
    /// </summary>
    public async Task<IReadOnlyList<SemanticPath>> SearchPathsAsync(string keyword)
    {
        var sql = @"
            SELECT semantic_path, category, description, hl7_field, fhir_path,
                   data_type_hint, example_value, usage_priority, tier
            FROM semantic_paths
            WHERE semantic_path LIKE @keyword
               OR description LIKE @keyword
               OR category LIKE @keyword
            ORDER BY
                CASE tier WHEN 'essential' THEN 1 ELSE 2 END,
                usage_priority ASC";

        var parameters = new[] { new SqliteParameter("@keyword", $"%{keyword}%") };
        return await QueryAsync(sql, row => new SemanticPath
        {
            Path = row.GetString("semantic_path"),
            Category = row.GetString("category"),
            Description = row.GetString("description"),
            HL7Field = row.IsDBNull("hl7_field") ? null : row.GetString("hl7_field"),
            FHIRPath = row.IsDBNull("fhir_path") ? null : row.GetString("fhir_path"),
            DataTypeHint = row.IsDBNull("data_type_hint") ? null : row.GetString("data_type_hint"),
            ExampleValue = row.IsDBNull("example_value") ? null : row.GetString("example_value"),
            Priority = row.GetInt32("usage_priority"),
            Tier = row.GetString("tier") == "essential" ? SemanticPathTier.Essential : SemanticPathTier.Advanced
        }, parameters);
    }

    // ============================================================================
    // MAPPING DATA IMPORT OPERATIONS
    // ============================================================================

    /// <summary>
    /// Imports segment mapping data from CSV parser results.
    /// </summary>
    public async Task ImportSegmentMappingsAsync(IEnumerable<SegmentMappingRow> mappings, string sourceFile)
    {
        var sql = @"
            INSERT OR REPLACE INTO segment_mappings
            (source_file, sort_order, hl7_identifier, hl7_name, hl7_data_type,
             hl7_cardinality_min, hl7_cardinality_max, condition_rule, fhir_attribute,
             fhir_data_type, fhir_cardinality_min, fhir_cardinality_max,
             data_type_mapping, vocabulary_mapping, assignment, comments)
            VALUES
            (@sourceFile, @sortOrder, @hl7Id, @hl7Name, @hl7DataType,
             @hl7CardMin, @hl7CardMax, @condition, @fhirAttr,
             @fhirDataType, @fhirCardMin, @fhirCardMax,
             @dataMapping, @vocabMapping, @assignment, @comments)";

        using var transaction = await _connection.BeginTransactionAsync();
        try
        {
            foreach (var mapping in mappings)
            {
                var parameters = new[]
                {
                    new SqliteParameter("@sourceFile", sourceFile),
                    new SqliteParameter("@sortOrder", ParseIntOrNull(mapping.SortOrder)),
                    new SqliteParameter("@hl7Id", mapping.HL7Identifier ?? (object)DBNull.Value),
                    new SqliteParameter("@hl7Name", mapping.HL7Name ?? (object)DBNull.Value),
                    new SqliteParameter("@hl7DataType", mapping.HL7DataType ?? (object)DBNull.Value),
                    new SqliteParameter("@hl7CardMin", ParseIntOrNull(mapping.HL7CardinalityMin)),
                    new SqliteParameter("@hl7CardMax", mapping.HL7CardinalityMax ?? (object)DBNull.Value),
                    new SqliteParameter("@condition", mapping.Condition ?? (object)DBNull.Value),
                    new SqliteParameter("@fhirAttr", mapping.FHIRAttribute ?? (object)DBNull.Value),
                    new SqliteParameter("@fhirDataType", mapping.FHIRDataType ?? (object)DBNull.Value),
                    new SqliteParameter("@fhirCardMin", ParseIntOrNull(mapping.FHIRCardinalityMin)),
                    new SqliteParameter("@fhirCardMax", mapping.FHIRCardinalityMax ?? (object)DBNull.Value),
                    new SqliteParameter("@dataMapping", mapping.DataTypeMapping ?? (object)DBNull.Value),
                    new SqliteParameter("@vocabMapping", mapping.VocabularyMapping ?? (object)DBNull.Value),
                    new SqliteParameter("@assignment", mapping.Assignment ?? (object)DBNull.Value),
                    new SqliteParameter("@comments", mapping.Comments ?? (object)DBNull.Value)
                };

                await ExecuteNonQueryAsync(sql, parameters);
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Imports datatype mapping data from CSV parser results.
    /// </summary>
    public async Task ImportDataTypeMappingsAsync(IEnumerable<DataTypeMappingRow> mappings, string sourceFile)
    {
        var sql = @"
            INSERT OR REPLACE INTO datatype_mappings
            (source_file, sort_order, hl7_identifier, hl7_name, hl7_data_type,
             hl7_cardinality_min, hl7_cardinality_max, condition_rule, fhir_attribute,
             fhir_data_type, fhir_cardinality_min, fhir_cardinality_max,
             data_type_mapping, vocabulary_mapping, assignment, comments)
            VALUES
            (@sourceFile, @sortOrder, @hl7Id, @hl7Name, @hl7DataType,
             @hl7CardMin, @hl7CardMax, @condition, @fhirAttr,
             @fhirDataType, @fhirCardMin, @fhirCardMax,
             @dataMapping, @vocabMapping, @assignment, @comments)";

        using var transaction = await _connection.BeginTransactionAsync();
        try
        {
            foreach (var mapping in mappings)
            {
                var parameters = new[]
                {
                    new SqliteParameter("@sourceFile", sourceFile),
                    new SqliteParameter("@sortOrder", ParseIntOrNull(mapping.SortOrder)),
                    new SqliteParameter("@hl7Id", mapping.HL7Identifier ?? (object)DBNull.Value),
                    new SqliteParameter("@hl7Name", mapping.HL7Name ?? (object)DBNull.Value),
                    new SqliteParameter("@hl7DataType", mapping.HL7DataType ?? (object)DBNull.Value),
                    new SqliteParameter("@hl7CardMin", ParseIntOrNull(mapping.HL7CardinalityMin)),
                    new SqliteParameter("@hl7CardMax", mapping.HL7CardinalityMax ?? (object)DBNull.Value),
                    new SqliteParameter("@condition", mapping.Condition ?? (object)DBNull.Value),
                    new SqliteParameter("@fhirAttr", mapping.FHIRAttribute ?? (object)DBNull.Value),
                    new SqliteParameter("@fhirDataType", mapping.FHIRDataType ?? (object)DBNull.Value),
                    new SqliteParameter("@fhirCardMin", ParseIntOrNull(mapping.FHIRCardinalityMin)),
                    new SqliteParameter("@fhirCardMax", mapping.FHIRCardinalityMax ?? (object)DBNull.Value),
                    new SqliteParameter("@dataMapping", mapping.DataTypeMapping ?? (object)DBNull.Value),
                    new SqliteParameter("@vocabMapping", mapping.VocabularyMapping ?? (object)DBNull.Value),
                    new SqliteParameter("@assignment", mapping.Assignment ?? (object)DBNull.Value),
                    new SqliteParameter("@comments", mapping.Comments ?? (object)DBNull.Value)
                };

                await ExecuteNonQueryAsync(sql, parameters);
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Imports code system mapping data from CSV parser results.
    /// </summary>
    public async Task ImportCodeSystemMappingsAsync(IEnumerable<CodeSystemMappingRow> mappings, string sourceFile)
    {
        var sql = @"
            INSERT OR REPLACE INTO codesystem_mappings
            (source_file, hl7_code, hl7_display, fhir_code, fhir_display, fhir_system, comments)
            VALUES
            (@sourceFile, @hl7Code, @hl7Display, @fhirCode, @fhirDisplay, @fhirSystem, @comments)";

        using var transaction = await _connection.BeginTransactionAsync();
        try
        {
            foreach (var mapping in mappings)
            {
                var parameters = new[]
                {
                    new SqliteParameter("@sourceFile", sourceFile),
                    new SqliteParameter("@hl7Code", mapping.HL7Code ?? (object)DBNull.Value),
                    new SqliteParameter("@hl7Display", mapping.HL7Display ?? (object)DBNull.Value),
                    new SqliteParameter("@fhirCode", mapping.FHIRCode ?? (object)DBNull.Value),
                    new SqliteParameter("@fhirDisplay", mapping.FHIRDisplay ?? (object)DBNull.Value),
                    new SqliteParameter("@fhirSystem", mapping.FHIRSystem ?? (object)DBNull.Value),
                    new SqliteParameter("@comments", mapping.Comments ?? (object)DBNull.Value)
                };

                await ExecuteNonQueryAsync(sql, parameters);
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    private static int? ParseIntOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return int.TryParse(value, out var result) ? result : null;
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, Func<SqliteDataReader, T> mapper, SqliteParameter[]? parameters = null)
    {
        using var command = new SqliteCommand(sql, _connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }

        using var reader = await command.ExecuteReaderAsync();
        var results = new List<T>();

        while (await reader.ReadAsync())
        {
            results.Add(mapper(reader));
        }

        return results;
    }

    private async Task<T> ExecuteScalarAsync<T>(string sql, SqliteParameter[]? parameters = null)
    {
        using var command = new SqliteCommand(sql, _connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }

        var result = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T));
    }

    private async Task<int> ExecuteNonQueryAsync(string sql, SqliteParameter[]? parameters = null)
    {
        using var command = new SqliteCommand(sql, _connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }

        return await command.ExecuteNonQueryAsync();
    }

    private int ExecuteNonQuery(string sql)
    {
        using var command = new SqliteCommand(sql, _connection);
        return command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents a semantic path for healthcare concepts.
/// </summary>
public record SemanticPath
{
    public required string Path { get; init; }
    public required string Category { get; init; }
    public required string Description { get; init; }
    public string? HL7Field { get; init; }
    public string? FHIRPath { get; init; }
    public string? DataTypeHint { get; init; }
    public string? ExampleValue { get; init; }
    public int Priority { get; init; }
    public SemanticPathTier Tier { get; init; }
}