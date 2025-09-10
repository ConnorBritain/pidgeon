// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Generation;
using System.Text.Json;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// File-based implementation of vendor specification repository.
/// Loads detailed vendor interface specifications from JSON files.
/// Supports enterprise-level vendor configurations with complete field mappings.
/// </summary>
internal class VendorSpecRepository : IVendorSpecRepository
{
    private readonly ILogger<VendorSpecRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _cacheLock = new();
    private Dictionary<string, VendorSpecification> _specificationCache = new();
    private DateTime _lastRefresh = DateTime.MinValue;

    public VendorSpecRepository(ILogger<VendorSpecRepository> logger)
    {
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = {
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            }
        };

        // Set default configuration path
        ConfigurationPath = GetDefaultConfigurationPath();
    }

    /// <inheritdoc />
    public string ConfigurationPath { get; }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<VendorSpecification>>> LoadAllSpecificationsAsync()
    {
        try
        {
            var loadResult = await EnsureSpecificationsLoadedAsync();
            if (loadResult.IsFailure)
                return Result<IReadOnlyList<VendorSpecification>>.Failure(loadResult.Error);

            lock (_cacheLock)
            {
                var specifications = _specificationCache.Values.ToList();
                _logger.LogInformation("Loaded {SpecCount} vendor specifications", specifications.Count);
                return Result<IReadOnlyList<VendorSpecification>>.Success(specifications);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vendor specifications");
            return Result<IReadOnlyList<VendorSpecification>>.Failure($"Failed to load specifications: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<VendorSpecification>>> LoadSpecificationsForStandardAsync(string standard)
    {
        try
        {
            var allSpecsResult = await LoadAllSpecificationsAsync();
            if (allSpecsResult.IsFailure)
                return Result<IReadOnlyList<VendorSpecification>>.Failure(allSpecsResult.Error);

            // Filter specifications that support the requested standard
            var standardSpecs = allSpecsResult.Value
                .Where(spec => StandardMatcher.SupportsStandard(spec, standard))
                .ToList();

            _logger.LogDebug("Found {SpecCount} specifications for standard {Standard}", 
                standardSpecs.Count, standard);

            return Result<IReadOnlyList<VendorSpecification>>.Success(standardSpecs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading specifications for standard {Standard}", standard);
            return Result<IReadOnlyList<VendorSpecification>>.Failure($"Failed to load specifications: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<VendorSpecification?>> GetSpecificationAsync(string specificationId)
    {
        try
        {
            var loadResult = await EnsureSpecificationsLoadedAsync();
            if (loadResult.IsFailure)
                return Result<VendorSpecification?>.Failure(loadResult.Error);

            lock (_cacheLock)
            {
                if (_specificationCache.TryGetValue(specificationId, out var specification))
                {
                    return Result<VendorSpecification?>.Success(specification);
                }
            }

            return Result<VendorSpecification?>.Success(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting specification {SpecId}", specificationId);
            return Result<VendorSpecification?>.Failure($"Failed to get specification: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<VendorSpecification>>> GetSpecificationsForVendorAsync(string vendorName)
    {
        try
        {
            var allSpecsResult = await LoadAllSpecificationsAsync();
            if (allSpecsResult.IsFailure)
                return Result<IReadOnlyList<VendorSpecification>>.Failure(allSpecsResult.Error);

            var vendorSpecs = allSpecsResult.Value
                .Where(spec => spec.Specification.VendorName.Equals(vendorName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogDebug("Found {SpecCount} specifications for vendor {VendorName}", 
                vendorSpecs.Count, vendorName);

            return Result<IReadOnlyList<VendorSpecification>>.Success(vendorSpecs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting specifications for vendor {VendorName}", vendorName);
            return Result<IReadOnlyList<VendorSpecification>>.Failure($"Failed to get vendor specifications: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveSpecificationAsync(VendorSpecification specification)
    {
        try
        {
            if (specification == null)
                return Result.Failure("Specification cannot be null");

            // Ensure directory exists
            if (!Directory.Exists(ConfigurationPath))
            {
                Directory.CreateDirectory(ConfigurationPath);
                _logger.LogInformation("Created configuration directory: {Path}", ConfigurationPath);
            }

            // Save to file
            var filename = $"{specification.Id.ToLowerInvariant()}_spec.json";
            var filepath = Path.Combine(ConfigurationPath, filename);
            
            var json = JsonSerializer.Serialize(specification, _jsonOptions);
            await File.WriteAllTextAsync(filepath, json);

            // Update cache
            lock (_cacheLock)
            {
                _specificationCache[specification.Id] = specification;
            }

            _logger.LogInformation("Saved vendor specification {SpecId} to {FilePath}", 
                specification.Id, filepath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving vendor specification {SpecId}", specification.Id);
            return Result.Failure($"Failed to save specification: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> RefreshSpecificationsAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing vendor specifications from {Path}", ConfigurationPath);
            
            lock (_cacheLock)
            {
                _specificationCache.Clear();
                _lastRefresh = DateTime.MinValue;
            }

            return await EnsureSpecificationsLoadedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing vendor specifications");
            return Result.Failure($"Failed to refresh specifications: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> CreateDefaultSpecificationsAsync()
    {
        try
        {
            _logger.LogInformation("Creating default vendor specifications");

            foreach (var specification in VendorSpecFactory.CreateDefaultSpecifications())
            {
                await SaveSpecificationAsync(specification);
            }

            _logger.LogInformation("Created default vendor specifications");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default vendor specifications");
            return Result.Failure($"Failed to create default specifications: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private async Task<Result> EnsureSpecificationsLoadedAsync()
    {
        try
        {
            lock (_cacheLock)
            {
                // Check if cache is still fresh (5 minutes)
                if (_lastRefresh > DateTime.UtcNow.AddMinutes(-5) && _specificationCache.Count > 0)
                {
                    return Result.Success();
                }
            }

            // Load specifications from directory
            var specifications = new Dictionary<string, VendorSpecification>();

            // Create directory if it doesn't exist
            if (!Directory.Exists(ConfigurationPath))
            {
                Directory.CreateDirectory(ConfigurationPath);
                _logger.LogInformation("Created configuration directory: {Path}", ConfigurationPath);
                
                // Create default specifications
                await CreateDefaultSpecificationsAsync();
            }

            // Load all JSON files with specification naming pattern
            var jsonFiles = Directory.GetFiles(ConfigurationPath, "*_spec.json", SearchOption.TopDirectoryOnly);
            
            foreach (var file in jsonFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var specification = JsonSerializer.Deserialize<VendorSpecification>(json, _jsonOptions);
                    
                    if (specification != null && !string.IsNullOrEmpty(specification.Id))
                    {
                        specifications[specification.Id] = specification;
                        _logger.LogDebug("Loaded vendor specification {SpecId} from {File}", 
                            specification.Id, Path.GetFileName(file));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load specification from {File}", Path.GetFileName(file));
                }
            }

            lock (_cacheLock)
            {
                _specificationCache = specifications;
                _lastRefresh = DateTime.UtcNow;
            }

            _logger.LogInformation("Loaded {SpecCount} vendor specifications", specifications.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring specifications are loaded");
            return Result.Failure($"Failed to load specifications: {ex.Message}");
        }
    }

    private string GetDefaultConfigurationPath()
    {
        // Look for configuration in order of preference:
        // 1. Environment variable
        var envPath = Environment.GetEnvironmentVariable("PIDGEON_VENDOR_SPECS");
        if (!string.IsNullOrEmpty(envPath))
            return envPath;

        // 2. User's home directory
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
            return Path.Combine(userProfile, ".pidgeon", "vendors", "specs");

        // 3. Current directory fallback
        return Path.Combine(Directory.GetCurrentDirectory(), "vendors", "specs");
    }


    #endregion
}