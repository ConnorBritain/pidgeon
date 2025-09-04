// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// In-memory implementation of configuration catalog for Phase 1A.
/// This will be extended with persistent storage and plugin architecture in Phase 1B.
/// </summary>
internal class ConfigurationCatalog : IConfigurationAnalyzer, IConfigurationQuery, IConfigurationRepository, IConfigurationComparator, IConfigurationAnalytics
{
    private readonly IConfigurationInferenceService _inferenceService;
    private readonly ILogger<ConfigurationCatalog> _logger;
    private readonly Dictionary<ConfigurationAddress, VendorConfiguration> _configurations = new();
    private readonly object _lock = new();

    public ConfigurationCatalog(
        IConfigurationInferenceService inferenceService,
        ILogger<ConfigurationCatalog> logger)
    {
        _inferenceService = inferenceService;
        _logger = logger;
    }

    public async Task<Result<VendorConfiguration>> AnalyzeMessagesAsync(
        IEnumerable<string> messages,
        ConfigurationAddress address,
        InferenceOptions? options = null)
    {
        try
        {
            _logger.LogInformation("Analyzing messages for address: {Address}", address);

            // Use the inference service to analyze messages
            var analysisResult = await _inferenceService.InferConfigurationAsync(messages, address, options);
            if (analysisResult.IsFailure)
                return Result<VendorConfiguration>.Failure(analysisResult.Error);

            var newConfiguration = analysisResult.Value;

            // Check if we have an existing configuration to merge with
            lock (_lock)
            {
                if (_configurations.TryGetValue(address, out var existingConfiguration))
                {
                    _logger.LogInformation("Merging with existing configuration for {Address}", address);
                    var mergeResult = existingConfiguration.MergeWith(newConfiguration);
                    if (mergeResult.IsFailure)
                        return Result<VendorConfiguration>.Failure($"Failed to merge configuration: {mergeResult.Error}");
                    
                    _configurations[address] = mergeResult.Value;
                    return Result<VendorConfiguration>.Success(mergeResult.Value);
                }
                else
                {
                    _logger.LogInformation("Storing new configuration for {Address}", address);
                    _configurations[address] = newConfiguration;
                    return Result<VendorConfiguration>.Success(newConfiguration);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing messages for address: {Address}", address);
            return Result<VendorConfiguration>.Failure($"Analysis failed: {ex.Message}");
        }
    }

    public Task<Result<VendorConfiguration?>> GetConfigurationAsync(ConfigurationAddress address)
    {
        lock (_lock)
        {
            var configuration = _configurations.TryGetValue(address, out var config) ? config : null;
            return Task.FromResult(Result<VendorConfiguration?>.Success(configuration));
        }
    }

    public Task<Result<IReadOnlyList<VendorConfiguration>>> GetByVendorAsync(string vendor)
    {
        lock (_lock)
        {
            var configurations = _configurations.Values
                .Where(c => c.Address.Vendor.Equals(vendor, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(Result<IReadOnlyList<VendorConfiguration>>.Success(configurations));
        }
    }

    public Task<Result<IReadOnlyList<VendorConfiguration>>> GetByStandardAsync(string standard)
    {
        lock (_lock)
        {
            var configurations = _configurations.Values
                .Where(c => c.Address.Standard.Equals(standard, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(Result<IReadOnlyList<VendorConfiguration>>.Success(configurations));
        }
    }

    public Task<Result<IReadOnlyList<VendorConfiguration>>> GetByMessageTypeAsync(string messageType)
    {
        lock (_lock)
        {
            var configurations = _configurations.Values
                .Where(c => c.Address.MessageType.Equals(messageType, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(Result<IReadOnlyList<VendorConfiguration>>.Success(configurations));
        }
    }

    public Task<Result<IReadOnlyList<VendorConfiguration>>> ListAllAsync()
    {
        lock (_lock)
        {
            var configurations = _configurations.Values.ToList();
            return Task.FromResult(Result<IReadOnlyList<VendorConfiguration>>.Success(configurations));
        }
    }

    public Task<Result<ConfigurationComparison>> CompareConfigurationsAsync(
        ConfigurationAddress fromAddress,
        ConfigurationAddress toAddress)
    {
        // TODO: Implement configuration comparison in Phase 1B
        return Task.FromResult(Result<ConfigurationComparison>.Failure(
            "Configuration comparison not yet implemented"));
    }

    public Task<Result<IReadOnlyList<VendorConfiguration>>> FindSimilarAsync(
        VendorConfiguration reference,
        double threshold = 0.7)
    {
        // TODO: Implement similarity analysis in Phase 1B
        return Task.FromResult(Result<IReadOnlyList<VendorConfiguration>>.Success(
            new List<VendorConfiguration>()));
    }

    public Task<Result> StoreConfigurationAsync(VendorConfiguration configuration)
    {
        lock (_lock)
        {
            _configurations[configuration.Address] = configuration;
            _logger.LogInformation("Stored configuration for {Address}", configuration.Address);
            return Task.FromResult(Result.Success());
        }
    }

    public Task<Result> RemoveConfigurationAsync(ConfigurationAddress address)
    {
        lock (_lock)
        {
            if (_configurations.Remove(address))
            {
                _logger.LogInformation("Removed configuration for {Address}", address);
                return Task.FromResult(Result.Success());
            }
            else
            {
                return Task.FromResult(Result.Failure($"Configuration not found for address: {address}"));
            }
        }
    }

    public Task<Result<IReadOnlyList<ConfigurationChange>>> GetChangeHistoryAsync(
        ConfigurationAddress address,
        TimeSpan? timeWindow = null)
    {
        lock (_lock)
        {
            if (_configurations.TryGetValue(address, out var configuration))
            {
                var changes = configuration.Metadata.Changes.AsReadOnly();
                if (timeWindow.HasValue)
                {
                    var cutoff = DateTime.UtcNow - timeWindow.Value;
                    changes = configuration.Metadata.Changes
                        .Where(c => c.ChangeDate >= cutoff)
                        .ToList()
                        .AsReadOnly();
                }
                return Task.FromResult(Result<IReadOnlyList<ConfigurationChange>>.Success(changes));
            }
            else
            {
                return Task.FromResult(Result<IReadOnlyList<ConfigurationChange>>.Failure(
                    $"Configuration not found for address: {address}"));
            }
        }
    }

    public Task<Result<ConfigurationCatalogStats>> GetStatisticsAsync()
    {
        lock (_lock)
        {
            var stats = new ConfigurationCatalogStats
            {
                TotalConfigurations = _configurations.Count,
                UniqueVendors = _configurations.Values.Select(c => c.Address.Vendor).Distinct().Count(),
                UniqueStandards = _configurations.Values.Select(c => c.Address.Standard).Distinct().Count(),
                UniqueMessageTypes = _configurations.Values.Select(c => c.Address.MessageType).Distinct().Count(),
                TotalMessagesAnalyzed = _configurations.Values.Sum(c => c.Metadata.MessagesSampled),
                AverageConfidence = _configurations.Count == 0 ? 0.0 : 
                    _configurations.Values.Average(c => c.Metadata.Confidence),
                LastUpdated = _configurations.Count == 0 ? DateTime.UtcNow :
                    _configurations.Values.Max(c => c.Metadata.LastUpdated),
                VendorCounts = _configurations.Values
                    .GroupBy(c => c.Address.Vendor)
                    .ToDictionary(g => g.Key, g => g.Count()),
                StandardCounts = _configurations.Values
                    .GroupBy(c => c.Address.Standard)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Task.FromResult(Result<ConfigurationCatalogStats>.Success(stats));
        }
    }

    public async Task<Result<ConfigurationValidationResult>> ValidateMessageAsync(
        string message,
        ConfigurationAddress address)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Result<ConfigurationValidationResult>.Success(
                    ConfigurationValidationResult.Failure(new[] { "Message content cannot be empty" }));
            }

            // Get the vendor configuration for this address
            var configResult = await GetConfigurationAsync(address);
            if (configResult.IsFailure)
            {
                return Result<ConfigurationValidationResult>.Success(
                    ConfigurationValidationResult.Failure(new[] { $"Configuration not found for address: {address}" }));
            }

            var configuration = configResult.Value;
            if (configuration == null)
            {
                return Result<ConfigurationValidationResult>.Success(
                    ConfigurationValidationResult.Failure(new[] { $"No configuration available for address: {address}" }));
            }

            // Validate against the detected vendor patterns
            var errors = new List<string>();
            var warnings = new List<string>();
            double confidenceScore = configuration.Metadata.Confidence;

            // Check if message type matches expected patterns
            var expectedMessageTypes = configuration.MessagePatterns.Keys;
            if (expectedMessageTypes.Any())
            {
                var messageContainsExpectedType = expectedMessageTypes.Any(msgType => 
                    message.Contains(msgType) || message.StartsWith($"MSH|") && message.Contains(msgType));
                
                if (!messageContainsExpectedType)
                {
                    warnings.Add($"Message does not match expected types: {string.Join(", ", expectedMessageTypes)}");
                    confidenceScore *= 0.8; // Reduce confidence for unexpected message type
                }
            }

            // Check format deviations
            if (configuration.FormatDeviations.Any())
            {
                warnings.Add($"Configuration has {configuration.FormatDeviations.Count} known format deviations");
                confidenceScore *= 0.9; // Slight confidence reduction for known deviations
            }

            // Validate minimum confidence threshold
            if (configuration.Metadata.Confidence < 0.7)
            {
                warnings.Add($"Configuration confidence is low: {configuration.Metadata.Confidence:P1}");
            }

            var result = errors.Count == 0 
                ? ConfigurationValidationResult.Success(confidenceScore)
                : ConfigurationValidationResult.Failure(errors, warnings);

            return Result<ConfigurationValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating message against configuration {Address}", address);
            return Result<ConfigurationValidationResult>.Failure($"Validation failed: {ex.Message}");
        }
    }
}