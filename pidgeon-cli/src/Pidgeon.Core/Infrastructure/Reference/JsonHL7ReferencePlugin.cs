// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Pidgeon.Core;
using Pidgeon.Core.Application.Interfaces.Reference;
using Pidgeon.Core.Domain.Reference.Entities;

namespace Pidgeon.Core.Infrastructure.Reference;

/// <summary>
/// Configuration for HL7 version-specific reference plugin.
/// </summary>
public record HL7VersionConfig(
    string Version,
    string StandardId, 
    string StandardName,
    string DataPath);

/// <summary>
/// JSON-based HL7 standards reference plugin supporting multiple versions.
/// Loads segment and field definitions from JSON files with lazy loading and caching.
/// </summary>
public class JsonHL7ReferencePlugin : IStandardReferencePlugin, IAdvancedStandardReferencePlugin
{
    private readonly ILogger<JsonHL7ReferencePlugin> _logger;
    private readonly IMemoryCache _cache;
    private readonly HL7VersionConfig _config;
    private string _dataBasePath;
    private bool _isInitialized;
    private bool _useEmbeddedResources;
    private string _resourcePrefix = string.Empty;
    private readonly object _initLock = new object();
    
    private static readonly Regex HL7PathPattern = new(@"^[A-Z]{2,3}(\.\d+){0,2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TablePattern = new(@"^\d{4}$", RegexOptions.Compiled);
    private static readonly Regex NamedTablePattern = new(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);
    private static readonly Regex TriggerEventPattern = new(@"^[A-Z]\d{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MessageTypePattern = new(@"^[A-Z]{3}_[A-Z]\d{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DataTypePattern = new(@"^[A-Z]{2,3}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public string StandardIdentifier => _config.StandardId;
    public string StandardName => _config.StandardName;
    public string Version => _config.Version;

    public JsonHL7ReferencePlugin(HL7VersionConfig config, ILogger<JsonHL7ReferencePlugin> logger, IMemoryCache cache)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger;
        _cache = cache;

        // Defer expensive initialization to first use
        _dataBasePath = string.Empty; // Will be initialized lazily
        _isInitialized = false;
    }

    /// <summary>
    /// Lazy initialization of data paths and logging. Only called when plugin is first used.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_isInitialized)
            return;

        lock (_initLock)
        {
            if (_isInitialized)
                return;

            _logger.LogDebug("Initializing JsonHL7ReferencePlugin for {StandardName}...", _config.StandardName);

            // Try embedded resources first
            var assembly = Assembly.GetExecutingAssembly();
            _resourcePrefix = $"data.standards.{_config.DataPath}";

            if (HasEmbeddedResources(assembly, _resourcePrefix))
            {
                _useEmbeddedResources = true;
                _logger.LogInformation("JsonHL7ReferencePlugin ({StandardName}) using embedded resources",
                    _config.StandardName);
            }
            else
            {
                // Fallback to file system approach
                var possiblePaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "data", "standards", _config.DataPath),
                    Path.Combine(Directory.GetCurrentDirectory(), "data", "standards", _config.DataPath),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "data", "standards", _config.DataPath)
                };

                _dataBasePath = possiblePaths.FirstOrDefault(Directory.Exists)
                                ?? possiblePaths[0];

                _logger.LogInformation("JsonHL7ReferencePlugin ({StandardName}) using file system path: {DataPath}",
                    _config.StandardName, _dataBasePath);
            }

            _isInitialized = true;
        }
    }

    public bool CanHandle(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var normalizedPath = path.ToUpperInvariant();

        // Field patterns: PID.3.5, MSH.9, OBR.4
        if (HL7PathPattern.IsMatch(normalizedPath))
            return true;

        // Table patterns: 0001, 0203, 0076
        if (TablePattern.IsMatch(normalizedPath))
            return true;

        // Named table patterns: FirstName, ZipCode, ISO3166
        if (NamedTablePattern.IsMatch(normalizedPath))
            return true;

        // Trigger event patterns: A01, O01, R01
        if (TriggerEventPattern.IsMatch(normalizedPath))
            return true;

        // Message type patterns: ADT_A01, ORU_R01
        if (MessageTypePattern.IsMatch(normalizedPath))
            return true;

        // Segment and data type patterns: PID, MSH, AD, CE (differentiated by existence check)
        if (DataTypePattern.IsMatch(normalizedPath))
        {
            // Check if it's a known segment, data type, or search term
            return IsKnownElement(normalizedPath);
        }

        return false;
    }

    public double GetConfidence(string path)
    {
        if (!CanHandle(path))
            return 0.0;

        var normalizedPath = path.ToUpperInvariant();

        // Very high confidence for exact pattern matches
        if (TablePattern.IsMatch(normalizedPath))
            return 0.98; // Tables are very specific 4-digit patterns

        if (NamedTablePattern.IsMatch(normalizedPath))
            return 0.96; // Named tables like FirstName, ZipCode

        if (TriggerEventPattern.IsMatch(normalizedPath))
            return 0.95; // Trigger events are specific letter+2-digit patterns

        if (MessageTypePattern.IsMatch(normalizedPath))
            return 0.94; // Message types like ADT_A01

        if (HL7PathPattern.IsMatch(normalizedPath))
        {
            var parts = normalizedPath.Split('.');
            var segment = parts[0];

            // High confidence for known HL7 segments with field references
            var commonSegments = new[] { "MSH", "PID", "OBR", "OBX", "RXE", "RXA", "PV1", "EVN", "NK1", "DG1", "AL1", "GT1", "IN1", "IN2", "PR1" };
            if (commonSegments.Contains(segment))
                return 0.95;

            // Medium-high confidence for HL7-like segment patterns with fields
            return 0.8;
        }

        if (DataTypePattern.IsMatch(normalizedPath))
        {
            // Check if it exists as segment vs data type
            var segmentFile = Path.Combine(_dataBasePath, "segments", $"{normalizedPath.ToLowerInvariant()}.json");
            var dataTypeFile = Path.Combine(_dataBasePath, "data_types", $"{normalizedPath.ToLowerInvariant()}.json");

            if (File.Exists(segmentFile))
                return 0.9; // Known segment

            if (File.Exists(dataTypeFile))
                return 0.85; // Known data type

            // Medium confidence for letter patterns
            return 0.6;
        }

        return 0.3;
    }

    public async Task<Result<StandardElement>> LookupAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!CanHandle(path))
        {
            return Result<StandardElement>.Failure($"Invalid HL7 path format: '{path}'. Expected format: PID.3.5 or MSH.3");
        }

        // Initialize on first use (lazy loading)
        EnsureInitialized();

        var normalizedPath = path.ToUpperInvariant();
        var cacheKey = $"{StandardIdentifier}:element:{normalizedPath}";

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out StandardElement? cachedElement))
        {
            return Result<StandardElement>.Success(cachedElement!);
        }

        try
        {
            var element = await LoadElementAsync(normalizedPath, cancellationToken);
            if (element != null)
            {
                // Cache for future lookups
                _cache.Set(cacheKey, element, TimeSpan.FromMinutes(30));
                return Result<StandardElement>.Success(element);
            }

            return Result<StandardElement>.Failure($"Element '{path}' not found in HL7 v2.3 specification");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading element {Path}", path);
            return Result<StandardElement>.Failure($"Error loading element: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<StandardElement>>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Result<IReadOnlyList<StandardElement>>.Success(Array.Empty<StandardElement>());
        }

        // Initialize on first use (lazy loading)
        EnsureInitialized();

        try
        {
            var results = new List<StandardElement>();
            var queryLower = query.ToLowerInvariant();

            // Load all available segments and search through them
            var segmentFiles = Directory.GetFiles(Path.Combine(_dataBasePath, "segments"), "*.json");
            
            foreach (var segmentFile in segmentFiles)
            {
                var segmentElements = await LoadSegmentElementsAsync(segmentFile, cancellationToken);
                
                var matches = segmentElements.Where(e =>
                    e.Name.ToLowerInvariant().Contains(queryLower) ||
                    e.Description.ToLowerInvariant().Contains(queryLower) ||
                    e.Path.ToLowerInvariant().Contains(queryLower) ||
                    e.Examples.Any(ex => ex.ToLowerInvariant().Contains(queryLower)));
                
                results.AddRange(matches);
            }

            var sortedResults = results
                .OrderBy(e => e.Path)
                .ToList();

            return Result<IReadOnlyList<StandardElement>>.Success(sortedResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for '{Query}'", query);
            return Result<IReadOnlyList<StandardElement>>.Failure($"Search failed: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<StandardElement>>> ListChildrenAsync(string parentPath, CancellationToken cancellationToken = default)
    {
        // Initialize on first use (lazy loading)
        EnsureInitialized();

        try
        {
            var parentPathUpper = parentPath.ToUpperInvariant();
            var segment = parentPathUpper.Split('.')[0];
            
            var segmentFile = Path.Combine(_dataBasePath, "segments", $"{segment.ToLowerInvariant()}.json");
            if (!File.Exists(segmentFile))
            {
                return Result<IReadOnlyList<StandardElement>>.Success(Array.Empty<StandardElement>());
            }

            var segmentElements = await LoadSegmentElementsAsync(segmentFile, cancellationToken);
            
            var children = segmentElements
                .Where(e => e.ParentPath?.Equals(parentPathUpper, StringComparison.OrdinalIgnoreCase) == true)
                .OrderBy(e => e.Path)
                .ToList();

            return Result<IReadOnlyList<StandardElement>>.Success(children);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing children for {ParentPath}", parentPath);
            return Result<IReadOnlyList<StandardElement>>.Failure($"List children failed: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<StandardElement>>> ListTopLevelElementsAsync(CancellationToken cancellationToken = default)
    {
        // Initialize on first use (lazy loading)
        EnsureInitialized();

        try
        {
            var results = new List<StandardElement>();
            var segmentFiles = Directory.GetFiles(Path.Combine(_dataBasePath, "segments"), "*.json");

            foreach (var segmentFile in segmentFiles)
            {
                var segmentElements = await LoadSegmentElementsAsync(segmentFile, cancellationToken);
                var topLevel = segmentElements.Where(e => !e.Path.Contains('.')).FirstOrDefault();
                if (topLevel != null)
                {
                    results.Add(topLevel);
                }
            }

            var sortedResults = results.OrderBy(e => e.Path).ToList();
            return Result<IReadOnlyList<StandardElement>>.Success(sortedResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing top-level elements");
            return Result<IReadOnlyList<StandardElement>>.Failure($"List top-level elements failed: {ex.Message}");
        }
    }

    public Result ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Failure("Path cannot be empty");
        }

        if (!HL7PathPattern.IsMatch(path))
        {
            return Result.Failure($"Invalid HL7 path format: '{path}'. Expected format: PID.3.5, OBR.2, or MSH");
        }

        var parts = path.Split('.');
        var segment = parts[0].ToUpperInvariant();

        if (segment.Length is < 2 or > 3)
        {
            return Result.Failure($"Invalid segment name: '{segment}'. Segment names must be 2-3 characters");
        }

        if (parts.Length > 1)
        {
            for (int i = 1; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out var fieldNumber) || fieldNumber <= 0)
                {
                    return Result.Failure($"Invalid field number: '{parts[i]}'. Field numbers must be positive integers");
                }
            }
        }

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<string>>> GetSuggestionsAsync(string path, CancellationToken cancellationToken = default)
    {
        // Initialize on first use (lazy loading)
        EnsureInitialized();

        var suggestions = new List<string>();

        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Result<IReadOnlyList<string>>.Success(suggestions);
            }

            // For segment-only paths, suggest common fields
            if (!path.Contains('.'))
            {
                var segment = path.ToUpperInvariant();
                var segmentFile = Path.Combine(_dataBasePath, "segments", $"{segment.ToLowerInvariant()}.json");
                
                if (File.Exists(segmentFile))
                {
                    var segmentElements = await LoadSegmentElementsAsync(segmentFile, cancellationToken);
                    suggestions.AddRange(segmentElements
                        .Where(e => e.Path.Count(c => c == '.') == 1) // Top-level fields only
                        .Select(e => e.Path)
                        .Take(5));
                }
            }
            else
            {
                // For invalid paths, suggest similar paths using Levenshtein distance
                var allElements = await LoadAllElementsAsync(cancellationToken);
                var similarPaths = allElements
                    .Where(e => LevenshteinDistance(e.Path, path.ToUpperInvariant()) <= 2)
                    .Select(e => e.Path)
                    .Take(5)
                    .ToList();
                
                suggestions.AddRange(similarPaths);
            }

            return Result<IReadOnlyList<string>>.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions for path {Path}", path);
            return Result<IReadOnlyList<string>>.Success(suggestions);
        }
    }

    public async Task<Result<StandardElement>> GetVendorVariationAsync(string path, string vendor, CancellationToken cancellationToken = default)
    {
        // Initialize on first use (lazy loading)
        EnsureInitialized();

        var lookupResult = await LookupAsync(path, cancellationToken);
        if (lookupResult.IsFailure)
        {
            return lookupResult;
        }

        var element = lookupResult.Value;
        var vendorVariation = element.VendorVariations
            .FirstOrDefault(v => string.Equals(v.Vendor, vendor, StringComparison.OrdinalIgnoreCase));

        if (vendorVariation == null)
        {
            // Return base element with note that no vendor-specific info is available
            var baseElement = element with
            {
                VendorVariations = [new VendorNote 
                { 
                    Vendor = vendor, 
                    Note = $"No {vendor}-specific variations documented for this field. Using standard HL7 v2.3 definition.", 
                    Examples = [] 
                }]
            };
            return Result<StandardElement>.Success(baseElement);
        }

        return Result<StandardElement>.Success(element);
    }

    // IAdvancedStandardReferencePlugin implementation
    public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Initialize on first use (lazy loading)
        EnsureInitialized();

        try
        {
            // Initialization is now handled by lazy loading
            await Task.Yield();
            
            _logger.LogInformation("HL7 v2.3 reference plugin initialized");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize HL7 v2.3 reference plugin");
            return Result.Failure($"Initialization failed: {ex.Message}");
        }
    }

    public async Task<Result> PreloadElementsAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        // Initialize on first use (lazy loading)
        EnsureInitialized();

        var loadTasks = paths.Select(async path =>
        {
            try
            {
                await LookupAsync(path, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to preload element {Path}", path);
                return false;
            }
        });

        var results = await Task.WhenAll(loadTasks);
        var successCount = results.Count(r => r);
        
        _logger.LogInformation("Preloaded {SuccessCount}/{TotalCount} elements", successCount, results.Length);
        return Result.Success();
    }

    public ReferencePluginStatistics GetStatistics()
    {
        // TODO: Implement actual statistics tracking
        return new ReferencePluginStatistics
        {
            TotalElements = 500, // Estimated
            LoadedElements = 0, // Would track cache count
            MemoryUsage = 0, // Would need to implement memory tracking
            AverageLookupTime = 50.0, // Would track actual lookup times
            CacheHitRate = 85.0, // Would track cache hits vs misses
            SuccessfulLookups = 0, // Would track successful lookups
            FailedLookups = 0 // Would track failed lookups
        };
    }

    public void ClearCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0); // Remove all entries
        }
        _logger.LogInformation("HL7 v2.3 reference plugin cache cleared");
    }

    // Private helper methods
    private async Task<StandardElement?> LoadElementAsync(string path, CancellationToken cancellationToken)
    {
        var normalizedPath = path.ToUpperInvariant();
        var pathParts = normalizedPath.Split('.');

        // Determine element type and load accordingly
        if (pathParts.Length >= 2)
        {
            // Field or component path (e.g., PID.3, PID.3.5)
            return await LoadFieldElementAsync(normalizedPath, cancellationToken);
        }
        else if (pathParts.Length == 1)
        {
            var query = pathParts[0];

            // Check if it's a numeric table (4 digits)
            if (query.Length == 4 && query.All(char.IsDigit))
            {
                return await LoadTableElementAsync(query, cancellationToken);
            }

            // Check if it's a named table (PascalCase, e.g., FirstName)
            if (NamedTablePattern.IsMatch(query))
            {
                return await LoadNamedTableElementAsync(query, cancellationToken);
            }

            // Check if it's a message type pattern (e.g., ADT_A01)
            if (MessageTypePattern.IsMatch(query))
            {
                return await LoadMessageTypeElementAsync(query, cancellationToken);
            }

            // Check if it's a trigger event (letter + 2 digits)
            if (Regex.IsMatch(query, @"^[A-Z]\d{2}$"))
            {
                return await LoadTriggerEventElementAsync(query, cancellationToken);
            }

            // Check if it's a segment or data type (2-3 letters)
            if (query.Length is >= 2 and <= 3 && query.All(char.IsLetter))
            {
                // Try segment first
                var segmentResult = await LoadSegmentElementAsync(query, cancellationToken);
                if (segmentResult != null)
                {
                    return segmentResult;
                }

                // If no segment found, try data type
                var dataTypeResult = await LoadDataTypeElementAsync(query, cancellationToken);
                if (dataTypeResult != null)
                {
                    return dataTypeResult;
                }
            }
        }

        return null;
    }

    private async Task<StandardElement?> LoadSegmentElementAsync(string segmentName, CancellationToken cancellationToken)
    {
        try
        {
            var json = await LoadResourceContentAsync($"segments/{segmentName.ToLowerInvariant()}.json", cancellationToken);
            var segmentData = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            if (!segmentData.TryGetProperty("code", out var codeProp))
            {
                return null;
            }

            var element = new StandardElement
            {
                Path = codeProp.GetString() ?? segmentName,
                Name = segmentData.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Description = segmentData.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                Standard = _config.StandardId,
                Version = _config.Version,
                DataType = "Segment",
                Usage = segmentData.TryGetProperty("usage", out var usage) ? usage.GetString() ?? "" : "",
                Examples = ExtractExamples(segmentData),
                ChildPaths = ExtractChildPaths(segmentData)
            };

            return element;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading segment {SegmentName}", segmentName);
            return null;
        }
    }

    private async Task<StandardElement?> LoadFieldElementAsync(string fieldPath, CancellationToken cancellationToken)
    {
        var pathParts = fieldPath.Split('.');
        var segmentName = pathParts[0];

        try
        {
            var json = await LoadResourceContentAsync($"segments/{segmentName.ToLowerInvariant()}.json", cancellationToken);
            var segmentData = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            if (!segmentData.TryGetProperty("fields", out var fields))
            {
                return null;
            }

            // Handle both field-level (PID.3) and component-level (PID.3.5) paths
            StandardElement? element = null;
            
            if (pathParts.Length == 2)
            {
                // Field-level lookup (e.g., PID.3)
                element = ExtractFieldElement(fields, fieldPath, segmentData);
            }
            else if (pathParts.Length == 3)
            {
                // Component-level lookup (e.g., PID.3.5)
                var fieldName = $"{pathParts[0]}.{pathParts[1]}";
                var componentPath = pathParts[2];
                element = ExtractComponentElement(fields, fieldName, componentPath, segmentData);
            }

            return element;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading field {FieldPath}", fieldPath);
            return null;
        }
    }

    private async Task<StandardElement?> LoadTableElementAsync(string tableNumber, CancellationToken cancellationToken)
    {
        var tableFile = Path.Combine(_dataBasePath, "tables", $"{tableNumber}.json");
        if (!File.Exists(tableFile))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(tableFile, cancellationToken);
            var tableData = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            var element = new StandardElement
            {
                Path = tableNumber,
                Name = tableData.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Description = tableData.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                Standard = _config.StandardId,
                Version = _config.Version,
                DataType = "Table",
                Usage = "Reference",
                ValidValues = ExtractValidValues(tableData),
                Examples = ExtractTableExamples(tableData)
            };

            return element;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading table {TableNumber}", tableNumber);
            return null;
        }
    }

    private async Task<StandardElement?> LoadNamedTableElementAsync(string tableName, CancellationToken cancellationToken)
    {
        var tableFile = Path.Combine(_dataBasePath, "tables", $"{tableName}.json");
        if (!File.Exists(tableFile))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(tableFile, cancellationToken);
            var tableData = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            var element = new StandardElement
            {
                Path = tableName,
                Name = tableData.TryGetProperty("name", out var name) ? name.GetString() ?? tableName : tableName,
                Description = tableData.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                Standard = _config.StandardId,
                Version = _config.Version,
                DataType = "Table",
                Usage = "Reference Dataset",
                ValidValues = ExtractValidValues(tableData),
                Examples = ExtractTableExamples(tableData)
            };

            return element;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading named table {TableName}", tableName);
            return null;
        }
    }

    private async Task<StandardElement?> LoadMessageTypeElementAsync(string messageType, CancellationToken cancellationToken)
    {
        var triggerFile = Path.Combine(_dataBasePath, "trigger_events", $"{messageType.ToLowerInvariant()}.json");
        if (!File.Exists(triggerFile))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(triggerFile, cancellationToken);
            var triggerData = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            var element = new StandardElement
            {
                Path = messageType.ToUpperInvariant(),
                Name = triggerData.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Description = triggerData.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                Standard = _config.StandardId,
                Version = _config.Version,
                DataType = "MessageType",
                Usage = triggerData.TryGetProperty("usage", out var usage) ? usage.GetString() ?? "" : "",
                Examples = ExtractExamples(triggerData)
            };

            return element;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading message type {MessageType}", messageType);
            return null;
        }
    }

    private async Task<StandardElement?> LoadDataTypeElementAsync(string dataTypeName, CancellationToken cancellationToken)
    {
        var dataTypeFile = Path.Combine(_dataBasePath, "data_types", $"{dataTypeName.ToLowerInvariant()}.json");
        if (!File.Exists(dataTypeFile))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(dataTypeFile, cancellationToken);
            var dataTypeData = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            var codeProperty = dataTypeData.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : dataTypeName.ToUpperInvariant();

            var element = new StandardElement
            {
                Path = codeProperty ?? dataTypeName.ToUpperInvariant(),
                Name = dataTypeData.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Description = dataTypeData.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                Standard = _config.StandardId,
                Version = _config.Version,
                DataType = "DataType",
                Usage = dataTypeData.TryGetProperty("category", out var category) ? category.GetString() ?? "" : "",
                Examples = ExtractExamples(dataTypeData),
                ChildPaths = ExtractDataTypeComponents(dataTypeData)
            };

            return element;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data type {DataTypeName}", dataTypeName);
            return null;
        }
    }

    private async Task<StandardElement?> LoadTriggerEventElementAsync(string triggerCode, CancellationToken cancellationToken)
    {
        var triggerFile = Path.Combine(_dataBasePath, "trigger_events", $"{triggerCode.ToLowerInvariant()}.json");
        if (!File.Exists(triggerFile))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(triggerFile, cancellationToken);
            var triggerData = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            var element = new StandardElement
            {
                Path = triggerCode.ToUpperInvariant(),
                Name = triggerData.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Description = triggerData.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                Standard = _config.StandardId,
                Version = _config.Version,
                DataType = "TriggerEvent",
                Usage = triggerData.TryGetProperty("usage", out var usage) ? usage.GetString() ?? "" : "",
                Examples = ExtractExamples(triggerData)
            };

            return element;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading trigger event {TriggerCode}", triggerCode);
            return null;
        }
    }

    private async Task<List<StandardElement>> LoadSegmentElementsAsync(string segmentFile, CancellationToken cancellationToken)
    {
        try
        {
            var json = await File.ReadAllTextAsync(segmentFile, cancellationToken);
            var segmentData = JsonSerializer.Deserialize<JsonElement>(json);
            
            var elements = new List<StandardElement>();
            
            // Add segment-level element
            if (segmentData.TryGetProperty("code", out var segmentName))
            {
                var segmentElement = new StandardElement
                {
                    Path = segmentName.GetString() ?? "",
                    Name = segmentData.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                    Description = segmentData.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    Standard = _config.StandardId
                };
                elements.Add(segmentElement);
                
                // Add field elements
                if (segmentData.TryGetProperty("fields", out var fields))
                {
                    if (fields.ValueKind == JsonValueKind.Array)
                    {
                        // Array format: fields is an array of field objects
                        foreach (var fieldData in fields.EnumerateArray())
                        {
                            if (fieldData.TryGetProperty("field_name", out var fieldNameProp))
                            {
                                var fieldPath = fieldNameProp.GetString();
                                if (!string.IsNullOrEmpty(fieldPath))
                                {
                                    var fieldElement = new StandardElement
                                    {
                                        Path = fieldPath,
                                        Name = GetFieldProperty(fieldData, "name", "field_description"),
                                        Description = GetFieldProperty(fieldData, "description", "field_description"),
                                        Standard = _config.StandardId,
                                        Version = _config.Version,
                                        DataType = GetFieldProperty(fieldData, "dataType", "data_type"),
                                        Usage = GetFieldProperty(fieldData, "usage", "optionality"),
                                        MaxLength = GetFieldIntProperty(fieldData, "maxLength", "length"),
                                        ParentPath = segmentName.GetString() ?? "",
                                        Examples = ExtractExamples(fieldData),
                                        ValidValues = ExtractValidValues(fieldData),
                                        ChildPaths = ExtractFieldComponents(fieldData)
                                    };
                                    elements.Add(fieldElement);
                                }
                            }
                        }
                    }
                    else if (fields.ValueKind == JsonValueKind.Object)
                    {
                        // Object format: fields is an object with field names as keys
                        foreach (var fieldProperty in fields.EnumerateObject())
                        {
                            var fieldPath = fieldProperty.Name;
                            var fieldData = fieldProperty.Value;
                        
                        var fieldElement = new StandardElement
                        {
                            Path = fieldPath,
                            Name = fieldData.TryGetProperty("name", out var fieldName) ? fieldName.GetString() ?? "" : "",
                            Description = fieldData.TryGetProperty("description", out var fieldDesc) ? fieldDesc.GetString() ?? "" : "",
                            Standard = _config.StandardId,
                            ParentPath = segmentName.GetString()
                        };
                        
                        // Add valid values if they exist
                        if (fieldData.TryGetProperty("validValues", out var validValues) && validValues.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var value in validValues.EnumerateArray())
                            {
                                if (value.TryGetProperty("code", out var code) && value.TryGetProperty("description", out var valueDesc))
                                {
                                    fieldElement.ValidValues.Add(new ValidValue
                                    {
                                        Code = code.GetString() ?? "",
                                        Description = valueDesc.GetString() ?? ""
                                    });
                                }
                            }
                        }
                        
                            elements.Add(fieldElement);
                        }
                    }
                }
            }
            
            return elements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading segment elements from {SegmentFile}", segmentFile);
            return [];
        }
    }

    private async Task<List<StandardElement>> LoadAllElementsAsync(CancellationToken cancellationToken)
    {
        var allElements = new List<StandardElement>();
        
        try
        {
            // Load all segments
            var segmentFiles = Directory.GetFiles(Path.Combine(_dataBasePath, "segments"), "*.json");
            foreach (var segmentFile in segmentFiles)
            {
                var segmentElements = await LoadSegmentElementsAsync(segmentFile, cancellationToken);
                allElements.AddRange(segmentElements);
            }
            
            // Load all tables
            var tableFiles = Directory.GetFiles(Path.Combine(_dataBasePath, "tables"), "*.json");
            foreach (var tableFile in tableFiles)
            {
                var tableNumber = Path.GetFileNameWithoutExtension(tableFile);
                var tableElement = await LoadTableElementAsync(tableNumber, cancellationToken);
                if (tableElement != null)
                {
                    allElements.Add(tableElement);
                }
            }
            
            // Load all data types
            var dataTypeFiles = Directory.GetFiles(Path.Combine(_dataBasePath, "data_types"), "*.json");
            foreach (var dataTypeFile in dataTypeFiles)
            {
                var dataTypeName = Path.GetFileNameWithoutExtension(dataTypeFile).ToUpperInvariant();
                var dataTypeElement = await LoadDataTypeElementAsync(dataTypeName, cancellationToken);
                if (dataTypeElement != null)
                {
                    allElements.Add(dataTypeElement);
                }
            }
            
            // Load all trigger events
            var triggerFiles = Directory.GetFiles(Path.Combine(_dataBasePath, "trigger_events"), "*.json");
            foreach (var triggerFile in triggerFiles)
            {
                var triggerCode = Path.GetFileNameWithoutExtension(triggerFile).ToUpperInvariant();
                var triggerElement = await LoadTriggerEventElementAsync(triggerCode, cancellationToken);
                if (triggerElement != null)
                {
                    allElements.Add(triggerElement);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all elements");
        }
        
        return allElements;
    }
    
    // Helper methods for JSON data extraction
    private static List<string> ExtractExamples(JsonElement data)
    {
        var examples = new List<string>();
        
        if (data.TryGetProperty("examples", out var examplesArray) && examplesArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var example in examplesArray.EnumerateArray())
            {
                if (example.ValueKind == JsonValueKind.String)
                {
                    var exampleValue = example.GetString();
                    if (!string.IsNullOrEmpty(exampleValue))
                    {
                        examples.Add(exampleValue);
                    }
                }
            }
        }
        
        return examples;
    }
    
    private static List<string> ExtractChildPaths(JsonElement segmentData)
    {
        var childPaths = new List<string>();

        if (segmentData.TryGetProperty("fields", out var fields))
        {
            if (fields.ValueKind == JsonValueKind.Array)
            {
                // Array format: fields is an array of field objects
                foreach (var field in fields.EnumerateArray())
                {
                    if (field.TryGetProperty("field_name", out var fieldNameProp))
                    {
                        var fieldName = fieldNameProp.GetString();
                        if (!string.IsNullOrEmpty(fieldName))
                        {
                            childPaths.Add(fieldName);
                        }
                    }
                }
            }
            else if (fields.ValueKind == JsonValueKind.Object)
            {
                // Object format: fields is an object with field names as keys
                foreach (var field in fields.EnumerateObject())
                {
                    childPaths.Add(field.Name);
                }
            }
        }

        return childPaths;
    }
    
    private static List<string> ExtractDataTypeComponents(JsonElement dataTypeData)
    {
        var components = new List<string>();
        
        if (dataTypeData.TryGetProperty("components", out var componentsObj))
        {
            foreach (var component in componentsObj.EnumerateObject())
            {
                components.Add(component.Name);
            }
        }
        
        return components;
    }
    
    private StandardElement? ExtractFieldElement(JsonElement fields, string fieldPath, JsonElement segmentData)
    {
        JsonElement fieldData = default;
        bool fieldFound = false;

        if (fields.ValueKind == JsonValueKind.Array)
        {
            // Array format: search for field by field_name
            foreach (var field in fields.EnumerateArray())
            {
                if (field.TryGetProperty("field_name", out var fieldNameProp))
                {
                    var fieldName = fieldNameProp.GetString();
                    if (fieldName == fieldPath)
                    {
                        fieldData = field;
                        fieldFound = true;
                        break;
                    }
                }
            }
        }
        else if (fields.ValueKind == JsonValueKind.Object)
        {
            // Object format: direct property lookup
            fieldFound = fields.TryGetProperty(fieldPath, out fieldData);
        }

        if (!fieldFound)
        {
            return null;
        }
        
        var element = new StandardElement
        {
            Path = fieldPath,
            Name = GetFieldProperty(fieldData, "name", "field_description"),
            Description = GetFieldProperty(fieldData, "description", "field_description"),
            Standard = _config.StandardId,
            Version = _config.Version,
            DataType = GetFieldProperty(fieldData, "dataType", "data_type"),
            Usage = GetFieldProperty(fieldData, "usage", "optionality"),
            MaxLength = GetFieldIntProperty(fieldData, "maxLength", "length"),
            Position = GetFieldIntProperty(fieldData, "position", "position"),
            Repeatability = GetFieldProperty(fieldData, "repeatability", "repeatability"),
            TableReference = GetFieldProperty(fieldData, "table", "table"),
            ParentPath = fieldPath.Split('.')[0], // Segment name
            Examples = ExtractExamples(fieldData),
            ValidValues = ExtractValidValues(fieldData),
            ChildPaths = ExtractFieldComponents(fieldData)
        };

        return element;
    }

    private static string GetFieldProperty(JsonElement fieldData, string primaryName, string fallbackName)
    {
        if (fieldData.TryGetProperty(primaryName, out var primary))
        {
            return primary.GetString() ?? "";
        }
        if (fieldData.TryGetProperty(fallbackName, out var fallback))
        {
            return fallback.GetString() ?? "";
        }
        return "";
    }

    private static int? GetFieldIntProperty(JsonElement fieldData, string primaryName, string fallbackName)
    {
        if (fieldData.TryGetProperty(primaryName, out var primary) && primary.ValueKind == JsonValueKind.Number)
        {
            return primary.GetInt32();
        }
        if (fieldData.TryGetProperty(fallbackName, out var fallback))
        {
            if (fallback.ValueKind == JsonValueKind.Number)
            {
                return fallback.GetInt32();
            }
            // Try parsing string to int for length field
            var lengthStr = fallback.GetString();
            if (int.TryParse(lengthStr, out var parsed))
            {
                return parsed;
            }
        }
        return null;
    }
    
    private StandardElement? ExtractComponentElement(JsonElement fields, string fieldName, string componentPath, JsonElement segmentData)
    {
        // Step 1: Find the field in the fields array
        JsonElement fieldData = default;
        bool fieldFound = false;

        if (fields.ValueKind == JsonValueKind.Array)
        {
            // Array format: search for field by field_name
            foreach (var field in fields.EnumerateArray())
            {
                if (field.TryGetProperty("field_name", out var fieldNameProp))
                {
                    var currentFieldName = fieldNameProp.GetString();
                    if (currentFieldName == fieldName)
                    {
                        fieldData = field;
                        fieldFound = true;
                        break;
                    }
                }
            }
        }

        if (!fieldFound)
        {
            return null;
        }

        if (!fieldData.TryGetProperty("data_type", out var dataTypeElement))
        {
            return null;
        }

        var dataTypeName = dataTypeElement.GetString();
        if (string.IsNullOrEmpty(dataTypeName))
        {
            return null;
        }

        // Step 2: Load the data type definition to get its components
        var dataTypeFile = Path.Combine(_dataBasePath, "data_types", $"{dataTypeName.ToLowerInvariant()}.json");
        if (!File.Exists(dataTypeFile))
        {
            return null;
        }

        var dataTypeJson = File.ReadAllText(dataTypeFile);
        using var dataTypeDoc = JsonDocument.Parse(dataTypeJson);
        var dataTypeRoot = dataTypeDoc.RootElement;

        if (!dataTypeRoot.TryGetProperty("fields", out var dataTypeFields))
        {
            return null;
        }

        // Step 3: Find the specific component by position
        if (!int.TryParse(componentPath, out var componentPosition))
        {
            return null;
        }

        JsonElement? componentData = null;
        foreach (var field in dataTypeFields.EnumerateArray())
        {
            if (field.TryGetProperty("position", out var positionElement) &&
                positionElement.GetInt32() == componentPosition)
            {
                componentData = field;
                break;
            }
        }

        if (!componentData.HasValue)
        {
            return null;
        }

        var component = componentData.Value;
        var fullPath = $"{fieldName}.{componentPath}";

        var element = new StandardElement
        {
            Path = fullPath,
            Name = component.TryGetProperty("field_description", out var desc) ? desc.GetString() ?? "" : "",
            Description = component.TryGetProperty("field_description", out var desc2) ? desc2.GetString() ?? "" : "",
            Standard = _config.StandardId,
            Version = _config.Version,
            DataType = component.TryGetProperty("data_type", out var compDataType) ? compDataType.GetString() ?? "" : "",
            Usage = component.TryGetProperty("optionality", out var usage) ? usage.GetString() ?? "" : "",
            MaxLength = component.TryGetProperty("length", out var length) ?
                (int.TryParse(length.GetString(), out var len) ? len : (int?)null) : null,
            ParentPath = fieldName,
            Examples = ExtractExamples(component),
            ValidValues = ExtractValidValues(component)
        };

        // Check for table reference
        if (component.TryGetProperty("table", out var table) && !string.IsNullOrEmpty(table.GetString()))
        {
            var tableRef = table.GetString();
            element = element with
            {
                Description = element.Description + $" (Table {tableRef})"
            };
        }

        return element;
    }
    
    private static List<string> ExtractFieldComponents(JsonElement fieldData)
    {
        var components = new List<string>();
        
        if (fieldData.TryGetProperty("components", out var componentsObj))
        {
            foreach (var component in componentsObj.EnumerateObject())
            {
                components.Add(component.Name);
            }
        }
        
        return components;
    }
    
    private static List<ValidValue> ExtractValidValues(JsonElement data)
    {
        var validValues = new List<ValidValue>();
        
        if (data.TryGetProperty("validValues", out var valuesArray) && valuesArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var value in valuesArray.EnumerateArray())
            {
                if (value.TryGetProperty("code", out var code) && value.TryGetProperty("description", out var desc))
                {
                    var validValue = new ValidValue
                    {
                        Code = code.GetString() ?? "",
                        Description = desc.GetString() ?? "",
                        IsDeprecated = value.TryGetProperty("deprecated", out var deprecated) && deprecated.GetBoolean()
                    };
                    validValues.Add(validValue);
                }
            }
        }
        else if (data.TryGetProperty("values", out var tableValuesArray) && tableValuesArray.ValueKind == JsonValueKind.Array)
        {
            // Handle table format
            foreach (var value in tableValuesArray.EnumerateArray())
            {
                if (value.TryGetProperty("code", out var code) && value.TryGetProperty("description", out var desc))
                {
                    var validValue = new ValidValue
                    {
                        Code = code.GetString() ?? "",
                        Description = desc.GetString() ?? "",
                        IsDeprecated = value.TryGetProperty("deprecated", out var deprecated) && deprecated.GetBoolean()
                    };
                    validValues.Add(validValue);
                }
            }
        }
        
        return validValues;
    }
    
    private static List<string> ExtractTableExamples(JsonElement tableData)
    {
        var examples = new List<string>();
        
        // Extract examples from valid values
        if (tableData.TryGetProperty("values", out var valuesArray) && valuesArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var value in valuesArray.EnumerateArray())
            {
                if (value.TryGetProperty("code", out var code))
                {
                    var codeValue = code.GetString();
                    if (!string.IsNullOrEmpty(codeValue))
                    {
                        examples.Add(codeValue);
                    }
                }
                
                if (examples.Count >= 5) break; // Limit examples
            }
        }
        
        return examples;
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;

        var distance = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            distance[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++)
            distance[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[s1.Length, s2.Length];
    }

    private bool IsKnownElement(string normalizedPath)
    {
        if (_useEmbeddedResources)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var segmentResource = $"{_resourcePrefix}.segments.{normalizedPath.ToLowerInvariant()}.json";
            var dataTypeResource = $"{_resourcePrefix}.data_types.{normalizedPath.ToLowerInvariant()}.json";
            var tableResource = $"{_resourcePrefix}.tables.{normalizedPath}.json";
            var triggerResource = $"{_resourcePrefix}.trigger_events.{normalizedPath.ToLowerInvariant()}.json";

            return assembly.GetManifestResourceStream(segmentResource) != null ||
                   assembly.GetManifestResourceStream(dataTypeResource) != null ||
                   assembly.GetManifestResourceStream(tableResource) != null ||
                   assembly.GetManifestResourceStream(triggerResource) != null;
        }
        else
        {
            // Check if it exists as any type of element
            var segmentFile = Path.Combine(_dataBasePath, "segments", $"{normalizedPath.ToLowerInvariant()}.json");
            var dataTypeFile = Path.Combine(_dataBasePath, "data_types", $"{normalizedPath.ToLowerInvariant()}.json");
            var tableFile = Path.Combine(_dataBasePath, "tables", $"{normalizedPath}.json");
            var triggerFile = Path.Combine(_dataBasePath, "trigger_events", $"{normalizedPath.ToLowerInvariant()}.json");

            return File.Exists(segmentFile) || File.Exists(dataTypeFile) || File.Exists(tableFile) || File.Exists(triggerFile);
        }
    }

    /// <summary>
    /// Checks if embedded resources exist for the specified resource prefix.
    /// </summary>
    private static bool HasEmbeddedResources(Assembly assembly, string resourcePrefix)
    {
        var resourceNames = assembly.GetManifestResourceNames();
        return resourceNames.Any(name => name.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Loads content from embedded resource or file system based on current configuration.
    /// </summary>
    private async Task<string> LoadResourceContentAsync(string relativePath, CancellationToken cancellationToken)
    {
        if (_useEmbeddedResources)
        {
            var resourceName = $"{_resourcePrefix}.{relativePath.Replace('/', '.').Replace('\\', '.')}";
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileNotFoundException($"Embedded resource not found: {resourceName}");

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        else
        {
            var filePath = Path.Combine(_dataBasePath, relativePath);
            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }
    }
}