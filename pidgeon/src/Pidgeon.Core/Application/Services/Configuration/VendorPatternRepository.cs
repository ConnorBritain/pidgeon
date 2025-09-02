// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Configuration.Entities;
using System.Text.Json;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// File-based implementation of vendor pattern repository.
/// Loads vendor detection patterns from JSON files in a configuration directory.
/// Follows MCP-style configuration pattern - JSON-driven, not code-driven.
/// </summary>
internal class VendorPatternRepository : IVendorPatternRepository
{
    private readonly ILogger<VendorPatternRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _cacheLock = new();
    private Dictionary<string, VendorDetectionPattern> _patternCache = new();
    private DateTime _lastRefresh = DateTime.MinValue;

    public VendorPatternRepository(ILogger<VendorPatternRepository> logger)
    {
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true
        };

        // Set default configuration path
        ConfigurationPath = GetDefaultConfigurationPath();
    }

    /// <inheritdoc />
    public string ConfigurationPath { get; }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<VendorDetectionPattern>>> LoadAllPatternsAsync()
    {
        try
        {
            // Ensure patterns are loaded
            var loadResult = await EnsurePatternsLoadedAsync();
            if (loadResult.IsFailure)
                return Result<IReadOnlyList<VendorDetectionPattern>>.Failure(loadResult.Error);

            lock (_cacheLock)
            {
                var patterns = _patternCache.Values.ToList();
                _logger.LogInformation("Loaded {PatternCount} vendor detection patterns", patterns.Count);
                return Result<IReadOnlyList<VendorDetectionPattern>>.Success(patterns);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vendor patterns");
            return Result<IReadOnlyList<VendorDetectionPattern>>.Failure($"Failed to load vendor patterns: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<VendorDetectionPattern>>> LoadPatternsForStandardAsync(string standard)
    {
        try
        {
            var allPatternsResult = await LoadAllPatternsAsync();
            if (allPatternsResult.IsFailure)
                return Result<IReadOnlyList<VendorDetectionPattern>>.Failure(allPatternsResult.Error);

            var standardPatterns = allPatternsResult.Value
                .Where(p => p.SupportedStandards.Contains(standard, StringComparer.OrdinalIgnoreCase))
                .ToList();

            _logger.LogDebug("Found {PatternCount} patterns for standard {Standard}", 
                standardPatterns.Count, standard);

            return Result<IReadOnlyList<VendorDetectionPattern>>.Success(standardPatterns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading patterns for standard {Standard}", standard);
            return Result<IReadOnlyList<VendorDetectionPattern>>.Failure($"Failed to load patterns: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<VendorDetectionPattern?>> GetPatternAsync(string patternId)
    {
        try
        {
            var loadResult = await EnsurePatternsLoadedAsync();
            if (loadResult.IsFailure)
                return Result<VendorDetectionPattern?>.Failure(loadResult.Error);

            lock (_cacheLock)
            {
                if (_patternCache.TryGetValue(patternId, out var pattern))
                {
                    return Result<VendorDetectionPattern?>.Success(pattern);
                }
            }

            return Result<VendorDetectionPattern?>.Success(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pattern {PatternId}", patternId);
            return Result<VendorDetectionPattern?>.Failure($"Failed to get pattern: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SavePatternAsync(VendorDetectionPattern pattern)
    {
        try
        {
            if (pattern == null)
                return Result.Failure("Pattern cannot be null");

            // Ensure directory exists
            if (!Directory.Exists(ConfigurationPath))
            {
                Directory.CreateDirectory(ConfigurationPath);
                _logger.LogInformation("Created configuration directory: {Path}", ConfigurationPath);
            }

            // Save to file
            var filename = $"{pattern.Id.ToLowerInvariant()}.json";
            var filepath = Path.Combine(ConfigurationPath, filename);
            
            var json = JsonSerializer.Serialize(pattern, _jsonOptions);
            await File.WriteAllTextAsync(filepath, json);

            // Update cache
            lock (_cacheLock)
            {
                _patternCache[pattern.Id] = pattern;
            }

            _logger.LogInformation("Saved vendor pattern {PatternId} to {FilePath}", pattern.Id, filepath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving vendor pattern {PatternId}", pattern.Id);
            return Result.Failure($"Failed to save pattern: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> RefreshPatternsAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing vendor patterns from {Path}", ConfigurationPath);
            
            lock (_cacheLock)
            {
                _patternCache.Clear();
                _lastRefresh = DateTime.MinValue;
            }

            return await EnsurePatternsLoadedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing vendor patterns");
            return Result.Failure($"Failed to refresh patterns: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private async Task<Result> EnsurePatternsLoadedAsync()
    {
        try
        {
            lock (_cacheLock)
            {
                // Check if cache is still fresh (5 minutes)
                if (_lastRefresh > DateTime.UtcNow.AddMinutes(-5) && _patternCache.Count > 0)
                {
                    return Result.Success();
                }
            }

            // Load patterns from directory
            var patterns = new Dictionary<string, VendorDetectionPattern>();

            // Create directory if it doesn't exist
            if (!Directory.Exists(ConfigurationPath))
            {
                Directory.CreateDirectory(ConfigurationPath);
                _logger.LogInformation("Created configuration directory: {Path}", ConfigurationPath);
                
                // Create default patterns
                await CreateDefaultPatternsAsync();
            }

            // Load all JSON files
            var jsonFiles = Directory.GetFiles(ConfigurationPath, "*.json", SearchOption.TopDirectoryOnly);
            
            foreach (var file in jsonFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var pattern = JsonSerializer.Deserialize<VendorDetectionPattern>(json, _jsonOptions);
                    
                    if (pattern != null && !string.IsNullOrEmpty(pattern.Id))
                    {
                        patterns[pattern.Id] = pattern;
                        _logger.LogDebug("Loaded vendor pattern {PatternId} from {File}", pattern.Id, Path.GetFileName(file));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load pattern from {File}", Path.GetFileName(file));
                }
            }

            lock (_cacheLock)
            {
                _patternCache = patterns;
                _lastRefresh = DateTime.UtcNow;
            }

            _logger.LogInformation("Loaded {PatternCount} vendor patterns", patterns.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring patterns are loaded");
            return Result.Failure($"Failed to load patterns: {ex.Message}");
        }
    }

    private string GetDefaultConfigurationPath()
    {
        // Look for configuration in order of preference:
        // 1. Environment variable
        var envPath = Environment.GetEnvironmentVariable("PIDGEON_VENDOR_PATTERNS");
        if (!string.IsNullOrEmpty(envPath))
            return envPath;

        // 2. User's home directory
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
            return Path.Combine(userProfile, ".pidgeon", "vendors");

        // 3. Current directory fallback
        return Path.Combine(Directory.GetCurrentDirectory(), "vendors");
    }

    private async Task CreateDefaultPatternsAsync()
    {
        _logger.LogInformation("Creating default vendor patterns");

        // Create Epic pattern
        var epicPattern = new VendorDetectionPattern
        {
            Id = "epic-systems",
            VendorName = "Epic",
            Description = "Epic Systems Corporation - Leading EHR vendor",
            SupportedStandards = new List<string> { "HL7v23", "HL7", "FHIRv4", "FHIR" },
            ApplicationPatterns = new List<DetectionRule>
            {
                new() { MatchType = Domain.Configuration.Entities.MatchType.Exact, Pattern = "EPIC", CaseSensitive = false, ConfidenceBoost = 0.15 },
                new() { MatchType = Domain.Configuration.Entities.MatchType.Contains, Pattern = "HYPERSPACE", CaseSensitive = false, ConfidenceBoost = 0.10 },
                new() { MatchType = Domain.Configuration.Entities.MatchType.Regex, Pattern = @"^EHR\d*$", CaseSensitive = false, ConfidenceBoost = 0.05 }
            },
            FacilityPatterns = new List<DetectionRule>
            {
                new() { MatchType = Domain.Configuration.Entities.MatchType.Contains, Pattern = "EPIC", CaseSensitive = false, ConfidenceBoost = 0.05 }
            },
            BaseConfidence = 0.80,
            VendorValidated = true,
            PatternVersion = "1.0",
            CommonDeviations = new List<CommonDeviation>
            {
                new()
                {
                    DeviationType = FormatDeviationType.DataFormatVariation,
                    Location = "MSH.3",
                    Description = "Epic often includes environment suffixes (_PROD, _TEST)",
                    Severity = DeviationSeverity.Info,
                    Frequency = 0.7
                }
            }
        };
        await SavePatternAsync(epicPattern);

        // Create Cerner pattern
        var cernerPattern = new VendorDetectionPattern
        {
            Id = "cerner-corp",
            VendorName = "Cerner",
            Description = "Cerner Corporation (Oracle Health) - Major EHR vendor",
            SupportedStandards = new List<string> { "HL7v23", "HL7", "FHIRv4", "FHIR" },
            ApplicationPatterns = new List<DetectionRule>
            {
                new() { MatchType = Domain.Configuration.Entities.MatchType.Contains, Pattern = "CERNER", CaseSensitive = false, ConfidenceBoost = 0.15 },
                new() { MatchType = Domain.Configuration.Entities.MatchType.Contains, Pattern = "MILLENNIUM", CaseSensitive = false, ConfidenceBoost = 0.10 },
                new() { MatchType = Domain.Configuration.Entities.MatchType.Contains, Pattern = "POWERCHART", CaseSensitive = false, ConfidenceBoost = 0.10 }
            },
            BaseConfidence = 0.80,
            VendorValidated = false,
            PatternVersion = "1.0"
        };
        await SavePatternAsync(cernerPattern);

        _logger.LogInformation("Created default vendor patterns");
    }

    #endregion
}