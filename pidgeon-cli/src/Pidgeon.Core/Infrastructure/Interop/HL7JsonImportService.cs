using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Pidgeon.Core.Infrastructure.Interop;

/// <summary>
/// Service for importing HL7 v2.3 structure data from JSON files into SQLite database.
/// Handles segments, data types, code tables, and trigger events.
/// Source: data/standards/hl7v23/*.json files
/// </summary>
public class HL7JsonImportService
{
    private readonly ILogger<HL7JsonImportService> _logger;
    private readonly SqliteConnection _connection;

    public HL7JsonImportService(SqliteConnection connection, ILogger<HL7JsonImportService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    /// <summary>
    /// Import all HL7 v2.3 structure data from JSON files
    /// </summary>
    public async Task<ImportResults> ImportAllAsync(string hl7DataDirectory)
    {
        _logger.LogInformation("Starting complete HL7 v2.3 data import from {Directory}", hl7DataDirectory);

        var results = new ImportResults();

        using var transaction = (SqliteTransaction)await _connection.BeginTransactionAsync();
        try
        {
            // Get HL7 v2.3 standard ID (with transaction)
            var standardId = await GetOrCreateStandardAsync("HL7v2.3", transaction);

            // Import in dependency order
            await ImportDataTypesAsync(Path.Combine(hl7DataDirectory, "data_types"), standardId, results, transaction);
            await ImportCodeTablesAsync(Path.Combine(hl7DataDirectory, "tables"), standardId, results, transaction);
            await ImportSegmentsAsync(Path.Combine(hl7DataDirectory, "segments"), standardId, results, transaction);
            await ImportTriggerEventsAsync(Path.Combine(hl7DataDirectory, "trigger_events"), standardId, results, transaction);

            await transaction.CommitAsync();
            _logger.LogInformation("HL7 import completed successfully: {Results}", results);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "HL7 import failed, transaction rolled back");
            throw;
        }

        return results;
    }

    /// <summary>
    /// Import data type definitions from JSON files
    /// </summary>
    public async Task ImportDataTypesAsync(string dataTypesDirectory, int standardId, ImportResults results, SqliteTransaction transaction)
    {
        if (!Directory.Exists(dataTypesDirectory))
        {
            _logger.LogWarning("Data types directory not found: {Directory}", dataTypesDirectory);
            return;
        }

        var files = Directory.GetFiles(dataTypesDirectory, "*.json");
        _logger.LogInformation("Importing {Count} data type files", files.Length);

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var dataType = JsonSerializer.Deserialize<DataTypeDefinition>(json, JsonOptions);

                if (dataType?.Code == null) continue;

                // Insert data type
                var dataTypeId = await InsertDataTypeAsync(dataType, standardId, transaction);

                // Insert components for composite types
                if (dataType.Fields?.Any() == true)
                {
                    foreach (var field in dataType.Fields)
                    {
                        await InsertDataTypeComponentAsync(field, dataTypeId, transaction);
                    }
                }

                results.DataTypesImported++;
                _logger.LogDebug("Imported data type: {Code} - {Name}", dataType.Code, dataType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import data type from {File}", file);
                results.Errors.Add($"Data type {Path.GetFileName(file)}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Import code table definitions from JSON files
    /// </summary>
    public async Task ImportCodeTablesAsync(string tablesDirectory, int standardId, ImportResults results, SqliteTransaction transaction)
    {
        if (!Directory.Exists(tablesDirectory))
        {
            _logger.LogWarning("Tables directory not found: {Directory}", tablesDirectory);
            return;
        }

        var files = Directory.GetFiles(tablesDirectory, "*.json");
        _logger.LogInformation("Importing {Count} code table files", files.Length);

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var table = JsonSerializer.Deserialize<CodeTableDefinition>(json, JsonOptions);

                if (table?.Code == null) continue;

                // Insert code table
                var tableId = await InsertCodeTableAsync(table, standardId, transaction);

                // Insert code values
                if (table.Values?.Any() == true)
                {
                    foreach (var value in table.Values)
                    {
                        await InsertCodeValueAsync(value, tableId, transaction);
                    }
                }

                results.CodeTablesImported++;
                _logger.LogDebug("Imported code table: {Code} - {Name} ({ValueCount} values)",
                    table.Code, table.Name, table.Values?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import code table from {File}", file);
                results.Errors.Add($"Code table {Path.GetFileName(file)}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Import segment definitions from JSON files
    /// </summary>
    public async Task ImportSegmentsAsync(string segmentsDirectory, int standardId, ImportResults results, SqliteTransaction transaction)
    {
        if (!Directory.Exists(segmentsDirectory))
        {
            _logger.LogWarning("Segments directory not found: {Directory}", segmentsDirectory);
            return;
        }

        var files = Directory.GetFiles(segmentsDirectory, "*.json");
        _logger.LogInformation("Importing {Count} segment files", files.Length);

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var segment = JsonSerializer.Deserialize<SegmentDefinition>(json, JsonOptions);

                if (segment?.Code == null) continue;

                // Insert segment
                var segmentId = await InsertSegmentAsync(segment, standardId, transaction);

                // Insert fields
                if (segment.Fields?.Any() == true)
                {
                    foreach (var field in segment.Fields)
                    {
                        await InsertFieldAsync(field, segmentId, transaction);
                    }
                }

                results.SegmentsImported++;
                _logger.LogDebug("Imported segment: {Code} - {Name} ({FieldCount} fields)",
                    segment.Code, segment.Name, segment.Fields?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import segment from {File}", file);
                results.Errors.Add($"Segment {Path.GetFileName(file)}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Import trigger event definitions from JSON files
    /// </summary>
    public async Task ImportTriggerEventsAsync(string eventsDirectory, int standardId, ImportResults results, SqliteTransaction transaction)
    {
        if (!Directory.Exists(eventsDirectory))
        {
            _logger.LogWarning("Trigger events directory not found: {Directory}", eventsDirectory);
            return;
        }

        var files = Directory.GetFiles(eventsDirectory, "*.json");
        _logger.LogInformation("Importing {Count} trigger event files", files.Length);

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var triggerEvent = JsonSerializer.Deserialize<TriggerEventDefinition>(json, JsonOptions);

                if (triggerEvent?.Code == null) continue;

                // Insert trigger event
                var eventId = await InsertTriggerEventAsync(triggerEvent, standardId, transaction);

                // TODO: Import event structure hierarchy (complex - for later phase)
                // This would require parsing the segments array with hierarchy

                results.TriggerEventsImported++;
                _logger.LogDebug("Imported trigger event: {Code} - {Title}", triggerEvent.Code, triggerEvent.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import trigger event from {File}", file);
                results.Errors.Add($"Trigger event {Path.GetFileName(file)}: {ex.Message}");
            }
        }
    }

    // ============================================================================
    // PRIVATE INSERT METHODS
    // ============================================================================

    private async Task<int> GetOrCreateStandardAsync(string version, SqliteTransaction transaction)
    {
        var sql = "SELECT id FROM standards WHERE version = @version";
        using var command = new SqliteCommand(sql, _connection, transaction);
        command.Parameters.AddWithValue("@version", version);

        var existingId = await command.ExecuteScalarAsync();
        if (existingId != null)
        {
            return Convert.ToInt32(existingId);
        }

        // Create new standard
        var insertSql = @"
            INSERT INTO standards (version, description, is_active)
            VALUES (@version, @description, @isActive)
            RETURNING id";

        using var insertCommand = new SqliteCommand(insertSql, _connection, transaction);
        insertCommand.Parameters.AddWithValue("@version", version);
        insertCommand.Parameters.AddWithValue("@description", $"Health Level Seven Version {version.Replace("HL7v", "")}");
        insertCommand.Parameters.AddWithValue("@isActive", true);

        var result = await insertCommand.ExecuteScalarAsync();
        return Convert.ToInt32(result!);
    }

    private async Task<int> InsertDataTypeAsync(DataTypeDefinition dataType, int standardId, SqliteTransaction transaction)
    {
        var sql = @"
            INSERT INTO data_types (standard_id, code, name, description, category, max_length, example)
            VALUES (@standardId, @code, @name, @description, @category, @maxLength, @example)
            RETURNING id";

        using var command = new SqliteCommand(sql, _connection, transaction);
        command.Parameters.AddWithValue("@standardId", standardId);
        command.Parameters.AddWithValue("@code", dataType.Code);
        command.Parameters.AddWithValue("@name", dataType.Name ?? dataType.Code);
        command.Parameters.AddWithValue("@description", dataType.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@category", dataType.Category ?? "primitive");
        command.Parameters.AddWithValue("@maxLength", (object)DBNull.Value); // TODO: Parse from description
        command.Parameters.AddWithValue("@example", dataType.Description ?? (object)DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result!);
    }

    private async Task InsertDataTypeComponentAsync(FieldDefinition field, int dataTypeId, SqliteTransaction transaction)
    {
        var sql = @"
            INSERT INTO data_type_components (data_type_id, position, field_name, field_description,
                                              length, optionality, repeatability)
            VALUES (@dataTypeId, @position, @fieldName, @fieldDescription,
                    @length, @optionality, @repeatability)";

        using var command = new SqliteCommand(sql, _connection, transaction);
        command.Parameters.AddWithValue("@dataTypeId", dataTypeId);
        command.Parameters.AddWithValue("@position", field.Position);
        command.Parameters.AddWithValue("@fieldName", field.FieldName ?? $"Component {field.Position}");
        command.Parameters.AddWithValue("@fieldDescription", field.FieldDescription ?? "");
        command.Parameters.AddWithValue("@length", ParseIntOrNull(field.Length) ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@optionality", field.Optionality ?? "O");
        command.Parameters.AddWithValue("@repeatability", field.Repeatability ?? "-");

        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> InsertCodeTableAsync(CodeTableDefinition table, int standardId, SqliteTransaction transaction)
    {
        var sql = @"
            INSERT INTO code_tables (standard_id, table_number, name, chapter, description, type)
            VALUES (@standardId, @tableNumber, @name, @chapter, @description, @type)
            RETURNING id";

        using var command = new SqliteCommand(sql, _connection, transaction);
        command.Parameters.AddWithValue("@standardId", standardId);
        command.Parameters.AddWithValue("@tableNumber", table.Code);
        command.Parameters.AddWithValue("@name", table.Name ?? "");
        command.Parameters.AddWithValue("@chapter", table.Chapter ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@description", table.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@type", table.Type ?? "User");

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result!);
    }

    private async Task InsertCodeValueAsync(CodeValueDefinition value, int tableId, SqliteTransaction transaction)
    {
        var sql = @"
            INSERT INTO code_values (table_id, code, description, comment, sort_order)
            VALUES (@tableId, @code, @description, @comment, @sortOrder)";

        using var command = new SqliteCommand(sql, _connection, transaction);
        command.Parameters.AddWithValue("@tableId", tableId);
        command.Parameters.AddWithValue("@code", value.Value ?? "");
        command.Parameters.AddWithValue("@description", value.Description ?? "");
        command.Parameters.AddWithValue("@comment", value.Comment ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@sortOrder", value.SortOrder ?? 0);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> InsertSegmentAsync(SegmentDefinition segment, int standardId, SqliteTransaction transaction)
    {
        var sql = @"
            INSERT INTO segments (standard_id, code, name, chapter, description)
            VALUES (@standardId, @code, @name, @chapter, @description)
            RETURNING id";

        using var command = new SqliteCommand(sql, _connection, transaction);
        command.Parameters.AddWithValue("@standardId", standardId);
        command.Parameters.AddWithValue("@code", segment.Code);
        command.Parameters.AddWithValue("@name", segment.Name ?? segment.Code);
        command.Parameters.AddWithValue("@chapter", segment.Chapter ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@description", segment.Description ?? (object)DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result!);
    }

    private async Task InsertFieldAsync(FieldDefinition field, int segmentId, SqliteTransaction transaction)
    {
        // Look up data type ID
        var dataTypeId = await GetDataTypeIdAsync(field.DataType, transaction);
        var tableId = !string.IsNullOrWhiteSpace(field.Table) ? await GetCodeTableIdAsync(field.Table, transaction) : (int?)null;

        var sql = @"
            INSERT INTO fields (segment_id, position, field_name, field_description, data_type_id,
                                length, optionality, repeatability, table_id)
            VALUES (@segmentId, @position, @fieldName, @fieldDescription, @dataTypeId,
                    @length, @optionality, @repeatability, @tableId)";

        using var command = new SqliteCommand(sql, _connection, transaction);
        command.Parameters.AddWithValue("@segmentId", segmentId);
        command.Parameters.AddWithValue("@position", field.Position);
        command.Parameters.AddWithValue("@fieldName", field.FieldName ?? $"Field {field.Position}");
        command.Parameters.AddWithValue("@fieldDescription", field.FieldDescription ?? "");
        command.Parameters.AddWithValue("@dataTypeId", dataTypeId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@length", ParseIntOrNull(field.Length) ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@optionality", field.Optionality ?? "O");
        command.Parameters.AddWithValue("@repeatability", field.Repeatability ?? "-");
        command.Parameters.AddWithValue("@tableId", tableId ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> InsertTriggerEventAsync(TriggerEventDefinition triggerEvent, int standardId, SqliteTransaction transaction)
    {
        var sql = @"
            INSERT INTO trigger_events (standard_id, code, title, chapter, description)
            VALUES (@standardId, @code, @title, @chapter, @description)
            RETURNING id";

        using var command = new SqliteCommand(sql, _connection, transaction);
        command.Parameters.AddWithValue("@standardId", standardId);
        command.Parameters.AddWithValue("@code", triggerEvent.Code);
        command.Parameters.AddWithValue("@title", triggerEvent.Title ?? triggerEvent.Code);
        command.Parameters.AddWithValue("@chapter", triggerEvent.Chapter ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@description", triggerEvent.Description ?? (object)DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result!);
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    private async Task<int?> GetDataTypeIdAsync(string? dataTypeCode, SqliteTransaction transaction)
    {
        if (string.IsNullOrWhiteSpace(dataTypeCode)) return null;

        var sql = "SELECT id FROM data_types WHERE code = @code";
        using var command = new SqliteCommand(sql, _connection, transaction);
        command.Parameters.AddWithValue("@code", dataTypeCode);

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : null;
    }

    private async Task<int?> GetCodeTableIdAsync(string? tableNumber, SqliteTransaction transaction)
    {
        if (string.IsNullOrWhiteSpace(tableNumber)) return null;

        var sql = "SELECT id FROM code_tables WHERE table_number = @tableNumber";
        using var command = new SqliteCommand(sql, _connection, transaction);
        command.Parameters.AddWithValue("@tableNumber", tableNumber);

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : null;
    }

    private static int? ParseIntOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "0") return null;
        return int.TryParse(value, out var result) ? result : null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}

// ============================================================================
// JSON MODEL CLASSES
// ============================================================================

public class SegmentDefinition
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Chapter { get; set; }
    public string? Description { get; set; }
    public List<FieldDefinition>? Fields { get; set; }
}

public class FieldDefinition
{
    public int Position { get; set; }
    public string? FieldName { get; set; } = "";
    public string? FieldDescription { get; set; } = "";
    public string? Length { get; set; }
    public string? DataType { get; set; }
    public string? Optionality { get; set; }
    public string? Repeatability { get; set; }
    public string? Table { get; set; }
}

public class DataTypeDefinition
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public List<FieldDefinition>? Fields { get; set; }
}

public class CodeTableDefinition
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Chapter { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public List<CodeValueDefinition>? Values { get; set; }
}

public class CodeValueDefinition
{
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string? Comment { get; set; }
    public int? SortOrder { get; set; }
}

/// <summary>
/// Domain model representing a code table value for use in the service interface.
/// </summary>
public record CodeValue(
    string Value,
    string Description,
    string? Comment = null,
    int? SortOrder = null
);

public class TriggerEventDefinition
{
    public string? Code { get; set; }
    public string? Title { get; set; }
    public string? Chapter { get; set; }
    public string? Description { get; set; }
    public string? MessageStructure { get; set; }
    // TODO: Add segments array for message structure
}

public class ImportResults
{
    public int SegmentsImported { get; set; }
    public int DataTypesImported { get; set; }
    public int CodeTablesImported { get; set; }
    public int TriggerEventsImported { get; set; }
    public List<string> Errors { get; set; } = new();

    public override string ToString()
    {
        return $"Segments: {SegmentsImported}, DataTypes: {DataTypesImported}, Tables: {CodeTablesImported}, Events: {TriggerEventsImported}, Errors: {Errors.Count}";
    }
}