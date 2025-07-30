// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Segmint.Core.Configuration.Inference;

/// <summary>
/// Service interface for configuration inference and management operations.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Infers vendor configuration from a collection of sample HL7 messages.
    /// </summary>
    /// <param name="hl7Messages">Collection of HL7 message strings.</param>
    /// <param name="vendorName">Name of the vendor/system.</param>
    /// <param name="messageType">HL7 message type.</param>
    /// <param name="confidenceThreshold">Minimum confidence level for patterns.</param>
    /// <returns>Inferred vendor configuration.</returns>
    Task<VendorConfiguration> InferFromSamples(IEnumerable<string> hl7Messages, string vendorName, string messageType, double confidenceThreshold = 0.7);

    /// <summary>
    /// Validates an HL7 message against a specific configuration.
    /// </summary>
    /// <param name="configurationId">The configuration identifier.</param>
    /// <param name="hl7Message">The HL7 message to validate.</param>
    /// <returns>Validation result with conformance score and deviations.</returns>
    Task<ConfigurationValidationResult> ValidateAgainstConfig(string configurationId, string hl7Message);

    /// <summary>
    /// Compares two configurations to identify differences.
    /// </summary>
    /// <param name="config1Id">First configuration identifier.</param>
    /// <param name="config2Id">Second configuration identifier.</param>
    /// <returns>Configuration diff result.</returns>
    Task<ConfigurationDiff> CompareConfigurations(string config1Id, string config2Id);

    /// <summary>
    /// Finds configurations similar to the target configuration.
    /// </summary>
    /// <param name="target">Target configuration to find matches for.</param>
    /// <param name="similarityThreshold">Minimum similarity score (0.0-1.0).</param>
    /// <returns>List of similar configurations with similarity scores.</returns>
    Task<IEnumerable<ConfigurationMatch>> FindSimilarConfigurations(VendorConfiguration target, double similarityThreshold = 0.8);

    /// <summary>
    /// Updates an existing configuration with new sample data.
    /// </summary>
    /// <param name="configurationId">Configuration to update.</param>
    /// <param name="newSamples">New HL7 message samples.</param>
    /// <returns>Updated configuration.</returns>
    Task<VendorConfiguration> UpdateConfigurationFromSamples(string configurationId, IEnumerable<string> newSamples);

    /// <summary>
    /// Saves a configuration to persistent storage.
    /// </summary>
    /// <param name="configuration">Configuration to save.</param>
    /// <returns>Saved configuration with updated metadata.</returns>
    Task<VendorConfiguration> SaveConfiguration(VendorConfiguration configuration);

    /// <summary>
    /// Loads a configuration by identifier.
    /// </summary>
    /// <param name="configurationId">Configuration identifier.</param>
    /// <returns>Configuration or null if not found.</returns>
    Task<VendorConfiguration?> LoadConfiguration(string configurationId);

    /// <summary>
    /// Lists all available configurations with optional filtering.
    /// </summary>
    /// <param name="vendor">Optional vendor filter.</param>
    /// <param name="messageType">Optional message type filter.</param>
    /// <returns>List of configuration summaries.</returns>
    Task<IEnumerable<ConfigurationSummary>> ListConfigurations(string? vendor = null, string? messageType = null);

    /// <summary>
    /// Deletes a configuration.
    /// </summary>
    /// <param name="configurationId">Configuration identifier to delete.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteConfiguration(string configurationId);
}

/// <summary>
/// Implementation of configuration service with file-based storage.
/// </summary>
public class FileBasedConfigurationService : IConfigurationService
{
    private readonly string _configurationDirectory;
    private readonly ILogger<FileBasedConfigurationService> _logger;
    private readonly Dictionary<string, VendorConfiguration> _configurationCache = new();

    /// <summary>
    /// Initializes a new instance of the FileBasedConfigurationService class.
    /// </summary>
    /// <param name="configurationDirectory">Directory to store configuration files.</param>
    /// <param name="logger">Logger instance.</param>
    public FileBasedConfigurationService(string configurationDirectory, ILogger<FileBasedConfigurationService> logger)
    {
        _configurationDirectory = configurationDirectory ?? throw new ArgumentNullException(nameof(configurationDirectory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Ensure configuration directory exists
        Directory.CreateDirectory(_configurationDirectory);
    }

    /// <inheritdoc />
    public Task<VendorConfiguration> InferFromSamples(IEnumerable<string> hl7Messages, string vendorName, string messageType, double confidenceThreshold = 0.7)
    {
        _logger.LogInformation("Starting configuration inference for vendor {Vendor}, message type {MessageType}", vendorName, messageType);

        var analyzer = new MessageAnalyzer();
        var messageList = hl7Messages.ToList();

        // Analyze messages
        var analysisSummary = analyzer.AnalyzeMessages(messageList);
        _logger.LogInformation("Analyzed {MessageCount} messages, found {SegmentPatterns} segment patterns and {FieldPatterns} field patterns",
            analysisSummary.TotalMessages, analysisSummary.SegmentPatterns, analysisSummary.FieldPatterns);

        // Generate configuration
        var configuration = analyzer.GenerateConfiguration(vendorName, messageType, confidenceThreshold);
        
        // Add analysis metadata
        configuration.InferredFrom.Metadata["analysis_summary"] = analysisSummary;
        configuration.InferredFrom.Metadata["sample_messages"] = messageList.Count;
        configuration.InferredFrom.Metadata["vendor_signatures"] = analysisSummary.VendorSignatures;

        _logger.LogInformation("Generated configuration {ConfigId} with {SegmentCount} segments and {RuleCount} validation rules",
            configuration.ConfigurationId, configuration.Segments.Count, configuration.ValidationRules.Count);

        return Task.FromResult(configuration);
    }

    /// <inheritdoc />
    public async Task<ConfigurationValidationResult> ValidateAgainstConfig(string configurationId, string hl7Message)
    {
        var configuration = await LoadConfiguration(configurationId);
        if (configuration == null)
        {
            throw new ArgumentException($"Configuration {configurationId} not found", nameof(configurationId));
        }

        var validator = new ConfigurationValidator(configuration);
        var result = validator.ValidateMessage(hl7Message);

        _logger.LogInformation("Validated message against config {ConfigId}: conformance {Conformance:F2}, {DeviationCount} deviations",
            configurationId, result.OverallConformance, result.Deviations.Count);

        return result;
    }

    /// <inheritdoc />
    public async Task<ConfigurationDiff> CompareConfigurations(string config1Id, string config2Id)
    {
        var config1 = await LoadConfiguration(config1Id);
        var config2 = await LoadConfiguration(config2Id);

        if (config1 == null) throw new ArgumentException($"Configuration {config1Id} not found", nameof(config1Id));
        if (config2 == null) throw new ArgumentException($"Configuration {config2Id} not found", nameof(config2Id));

        var validator = new ConfigurationValidator(config1);
        var diff = validator.CompareConfigurations(config2);

        _logger.LogInformation("Compared configurations {Config1} and {Config2}: similarity {Similarity:F2}, {DifferenceCount} differences",
            config1Id, config2Id, diff.Similarity, diff.Differences.Count);

        return diff;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConfigurationMatch>> FindSimilarConfigurations(VendorConfiguration target, double similarityThreshold = 0.8)
    {
        var allConfigurations = await ListConfigurations();
        var matches = new List<ConfigurationMatch>();

        var targetValidator = new ConfigurationValidator(target);

        foreach (var configSummary in allConfigurations)
        {
            if (configSummary.ConfigurationId == target.ConfigurationId) continue;

            var config = await LoadConfiguration(configSummary.ConfigurationId);
            if (config == null) continue;

            var diff = targetValidator.CompareConfigurations(config);
            if (diff.Similarity >= similarityThreshold)
            {
                matches.Add(new ConfigurationMatch
                {
                    Configuration = config,
                    Similarity = diff.Similarity,
                    Differences = diff.Differences.Count
                });
            }
        }

        return matches.OrderByDescending(m => m.Similarity);
    }

    /// <inheritdoc />
    public async Task<VendorConfiguration> UpdateConfigurationFromSamples(string configurationId, IEnumerable<string> newSamples)
    {
        var existingConfig = await LoadConfiguration(configurationId);
        if (existingConfig == null)
        {
            throw new ArgumentException($"Configuration {configurationId} not found", nameof(configurationId));
        }

        // Create a new analyzer and infer patterns from new samples
        var analyzer = new MessageAnalyzer();
        var newSamplesList = newSamples.ToList();
        var analysisSummary = analyzer.AnalyzeMessages(newSamplesList);

        // Generate new configuration
        var newConfig = analyzer.GenerateConfiguration(existingConfig.Vendor, existingConfig.MessageType);

        // Merge with existing configuration (simple merge - in practice, this would be more sophisticated)
        var mergedConfig = MergeConfigurations(existingConfig, newConfig);
        mergedConfig.InferredFrom.SampleCount += newSamplesList.Count;
        mergedConfig.InferredFrom.Metadata["last_update"] = DateTime.UtcNow;
        mergedConfig.InferredFrom.Metadata["update_samples"] = newSamplesList.Count;

        await SaveConfiguration(mergedConfig);

        _logger.LogInformation("Updated configuration {ConfigId} with {NewSampleCount} new samples",
            configurationId, newSamplesList.Count);

        return mergedConfig;
    }

    /// <inheritdoc />
    public async Task<VendorConfiguration> SaveConfiguration(VendorConfiguration configuration)
    {
        var filePath = GetConfigurationFilePath(configuration.ConfigurationId);
        
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var jsonString = JsonSerializer.Serialize(configuration, jsonOptions);
        await File.WriteAllTextAsync(filePath, jsonString);

        // Update cache
        _configurationCache[configuration.ConfigurationId] = configuration;

        _logger.LogInformation("Saved configuration {ConfigId} to {FilePath}", configuration.ConfigurationId, filePath);

        return configuration;
    }

    /// <inheritdoc />
    public async Task<VendorConfiguration?> LoadConfiguration(string configurationId)
    {
        // Check cache first
        if (_configurationCache.TryGetValue(configurationId, out var cachedConfig))
        {
            return cachedConfig;
        }

        var filePath = GetConfigurationFilePath(configurationId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var jsonString = await File.ReadAllTextAsync(filePath);
            var configuration = JsonSerializer.Deserialize<VendorConfiguration>(jsonString);
            
            if (configuration != null)
            {
                _configurationCache[configurationId] = configuration;
            }

            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration {ConfigId} from {FilePath}", configurationId, filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConfigurationSummary>> ListConfigurations(string? vendor = null, string? messageType = null)
    {
        var summaries = new List<ConfigurationSummary>();
        var configFiles = Directory.GetFiles(_configurationDirectory, "*.json");

        foreach (var filePath in configFiles)
        {
            try
            {
                var jsonString = await File.ReadAllTextAsync(filePath);
                var config = JsonSerializer.Deserialize<VendorConfiguration>(jsonString);
                
                if (config != null)
                {
                    // Apply filters
                    if (vendor != null && !config.Vendor.Equals(vendor, StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    if (messageType != null && !config.MessageType.Equals(messageType, StringComparison.OrdinalIgnoreCase))
                        continue;

                    summaries.Add(new ConfigurationSummary
                    {
                        ConfigurationId = config.ConfigurationId,
                        Vendor = config.Vendor,
                        Version = config.Version,
                        MessageType = config.MessageType,
                        CreatedAt = config.CreatedAt,
                        SampleCount = config.InferredFrom.SampleCount,
                        Confidence = config.InferredFrom.Confidence,
                        SegmentCount = config.Segments.Count,
                        ValidationRuleCount = config.ValidationRules.Count
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read configuration file {FilePath}", filePath);
            }
        }

        return summaries.OrderBy(s => s.Vendor).ThenBy(s => s.MessageType);
    }

    /// <inheritdoc />
    public Task<bool> DeleteConfiguration(string configurationId)
    {
        var filePath = GetConfigurationFilePath(configurationId);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _configurationCache.Remove(configurationId);
            
            _logger.LogInformation("Deleted configuration {ConfigId}", configurationId);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Gets the file path for a configuration.
    /// </summary>
    /// <param name="configurationId">Configuration identifier.</param>
    /// <returns>Full file path.</returns>
    private string GetConfigurationFilePath(string configurationId)
    {
        var fileName = $"{configurationId}.json";
        return Path.Combine(_configurationDirectory, fileName);
    }

    /// <summary>
    /// Merges two configurations into a combined configuration.
    /// </summary>
    /// <param name="existing">Existing configuration.</param>
    /// <param name="newConfig">New configuration to merge.</param>
    /// <returns>Merged configuration.</returns>
    private static VendorConfiguration MergeConfigurations(VendorConfiguration existing, VendorConfiguration newConfig)
    {
        // Simple merge strategy - in practice, this would involve sophisticated merging logic
        var merged = new VendorConfiguration
        {
            ConfigurationId = existing.ConfigurationId,
            Vendor = existing.Vendor,
            Version = existing.Version,
            MessageType = existing.MessageType,
            InferredFrom = existing.InferredFrom,
            CreatedAt = existing.CreatedAt
        };

        // Merge segments (combine unique segments)
        merged.Segments = new Dictionary<string, Dictionary<string, object>>(existing.Segments);
        foreach (var newSegment in newConfig.Segments)
        {
            if (!merged.Segments.ContainsKey(newSegment.Key))
            {
                merged.Segments[newSegment.Key] = newSegment.Value;
            }
            else
            {
                // Merge fields within segment
                foreach (var newField in newSegment.Value)
                {
                    merged.Segments[newSegment.Key][newField.Key] = newField.Value;
                }
            }
        }

        // Merge patterns
        merged.Patterns = new Dictionary<string, object>(existing.Patterns);
        foreach (var newPattern in newConfig.Patterns)
        {
            merged.Patterns[newPattern.Key] = newPattern.Value;
        }

        // Merge validation rules (add new rules that don't conflict)
        merged.ValidationRules = existing.ValidationRules.ToList();
        foreach (var newRule in newConfig.ValidationRules)
        {
            if (!merged.ValidationRules.Any(r => r.Field == newRule.Field && r.Rule == newRule.Rule))
            {
                merged.ValidationRules.Add(newRule);
            }
        }

        return merged;
    }
}

/// <summary>
/// Summary information about a configuration.
/// </summary>
public class ConfigurationSummary
{
    /// <summary>
    /// Configuration identifier.
    /// </summary>
    public string ConfigurationId { get; set; } = "";

    /// <summary>
    /// Vendor or system name.
    /// </summary>
    public string Vendor { get; set; } = "";

    /// <summary>
    /// System version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// HL7 message type.
    /// </summary>
    public string MessageType { get; set; } = "";

    /// <summary>
    /// Configuration creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of sample messages used for inference.
    /// </summary>
    public int SampleCount { get; set; }

    /// <summary>
    /// Statistical confidence level.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Number of segments in configuration.
    /// </summary>
    public int SegmentCount { get; set; }

    /// <summary>
    /// Number of validation rules.
    /// </summary>
    public int ValidationRuleCount { get; set; }
}

/// <summary>
/// Represents a configuration match with similarity score.
/// </summary>
public class ConfigurationMatch
{
    /// <summary>
    /// Matched configuration.
    /// </summary>
    public VendorConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Similarity score (0.0 - 1.0).
    /// </summary>
    public double Similarity { get; set; }

    /// <summary>
    /// Number of differences detected.
    /// </summary>
    public int Differences { get; set; }
}