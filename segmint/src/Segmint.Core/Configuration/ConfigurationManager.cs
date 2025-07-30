// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Segmint.Core.HL7.Validation;

namespace Segmint.Core.Configuration;

/// <summary>
/// Manages loading, saving, and validation of Segmint configuration files.
/// </summary>
public class ConfigurationManager
{
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ConfigurationManager(ILogger<ConfigurationManager> logger)
    {
        _logger = logger;
        _jsonOptions = CreateJsonOptions();
    }
    
    /// <summary>
    /// Loads configuration from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded configuration.</returns>
    public async Task<SegmintConfiguration> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
            
        _logger.LogInformation("Loading configuration from {FilePath}", filePath);
        
        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var configuration = JsonSerializer.Deserialize<SegmintConfiguration>(json, _jsonOptions);
            
            if (configuration == null)
                throw new InvalidOperationException("Failed to deserialize configuration - result was null.");
                
            _logger.LogInformation("Successfully loaded configuration from {FilePath}", filePath);
            return configuration;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON configuration file: {FilePath}", filePath);
            throw new InvalidOperationException($"Invalid JSON in configuration file: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {FilePath}", filePath);
            throw;
        }
    }
    
    /// <summary>
    /// Saves configuration to a JSON file.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    /// <param name="filePath">Path where to save the configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SaveAsync(SegmintConfiguration configuration, string filePath, CancellationToken cancellationToken = default)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
            
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            
        _logger.LogInformation("Saving configuration to {FilePath}", filePath);
        
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory: {Directory}", directory);
            }
            
            var json = JsonSerializer.Serialize(configuration, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            
            _logger.LogInformation("Successfully saved configuration to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {FilePath}", filePath);
            throw;
        }
    }
    
    /// <summary>
    /// Validates a configuration object.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>List of validation issues found.</returns>
    public List<string> ValidateConfiguration(SegmintConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
            
        var issues = new List<string>();
        
        // Validate HL7 configuration
        ValidateHL7Configuration(configuration.HL7, issues);
        
        // Validate validation configuration
        ValidateValidationConfiguration(configuration.Validation, issues);
        
        // Validate interface configuration
        ValidateInterfaceConfiguration(configuration.Interface, issues);
        
        _logger.LogDebug("Configuration validation completed with {IssueCount} issues", issues.Count);
        
        return issues;
    }
    
    /// <summary>
    /// Creates a default configuration with recommended settings.
    /// </summary>
    /// <returns>A new configuration with default values.</returns>
    public SegmintConfiguration CreateDefault()
    {
        _logger.LogDebug("Creating default configuration");
        
        return new SegmintConfiguration
        {
            HL7 = new HL7Configuration
            {
                DefaultVersion = "2.3",
                DefaultSendingApplication = "Segmint",
                DefaultProcessingId = "P",
                AutoGenerateControlId = true,
                IncludeTimestamps = true,
                DefaultTimeZone = "UTC"
            },
            Validation = new ValidationConfiguration
            {
                EnabledTypes = new List<ValidationType> { ValidationType.Syntax, ValidationType.Semantic },
                StrictMode = false,
                ValidateRequiredFields = true,
                ValidateDataTypes = true,
                ValidateCodeTables = false
            },
            Interface = new InterfaceConfiguration
            {
                Name = "Default Interface",
                ConnectionTimeoutSeconds = 30,
                Retry = new RetryConfiguration
                {
                    MaxAttempts = 3,
                    InitialDelayMs = 5000
                }
            },
            Logging = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Information,
                LogHL7Messages = true,
                LogValidationResults = true,
                MaxLogFileSizeMB = 10,
                RetainedLogFileCount = 5
            },
            Performance = new PerformanceConfiguration
            {
                MaxConcurrentOperations = Environment.ProcessorCount * 2,
                BatchProcessing = new BatchProcessingConfiguration
                {
                    BatchSize = 100
                },
                EnableCaching = true,
                CacheExpirationMinutes = 30
            }
        };
    }
    
    /// <summary>
    /// Compares two configurations and returns the differences.
    /// </summary>
    /// <param name="config1">First configuration.</param>
    /// <param name="config2">Second configuration.</param>
    /// <returns>List of differences found.</returns>
    public List<string> CompareConfigurations(SegmintConfiguration config1, SegmintConfiguration config2)
    {
        if (config1 == null) throw new ArgumentNullException(nameof(config1));
        if (config2 == null) throw new ArgumentNullException(nameof(config2));
        
        var differences = new List<string>();
        
        // Compare HL7 configurations
        CompareHL7Configurations(config1.HL7, config2.HL7, differences);
        
        // Compare validation configurations  
        CompareValidationConfigurations(config1.Validation, config2.Validation, differences);
        
        _logger.LogDebug("Configuration comparison completed with {DifferenceCount} differences", differences.Count);
        
        return differences;
    }
    
    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = 
            {
                new JsonStringEnumConverter()
            }
        };
    }
    
    private static void ValidateHL7Configuration(HL7Configuration hl7, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(hl7.DefaultVersion))
            issues.Add("HL7 default version cannot be empty");
            
        if (string.IsNullOrWhiteSpace(hl7.DefaultSendingApplication))
            issues.Add("Default sending application cannot be empty");
            
        if (hl7.FieldSeparator == hl7.ComponentSeparator)
            issues.Add("Field separator and component separator cannot be the same");
            
        if (string.IsNullOrWhiteSpace(hl7.ControlIdFormat))
            issues.Add("Control ID format cannot be empty");
    }
    
    private static void ValidateValidationConfiguration(ValidationConfiguration validation, List<string> issues)
    {
        if (validation.EnabledValidationTypes == null || !validation.EnabledValidationTypes.Any())
            issues.Add("At least one validation type must be enabled");
    }
    
    private static void ValidateInterfaceConfiguration(InterfaceConfiguration interfaceConfig, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(interfaceConfig.Name))
            issues.Add("Interface name cannot be empty");
            
        if (interfaceConfig.ConnectionTimeoutSeconds <= 0)
            issues.Add("Connection timeout must be positive");
            
        if (interfaceConfig.Retry.MaxAttempts < 0)
            issues.Add("Retry attempts cannot be negative");
    }
    
    private static void CompareHL7Configurations(HL7Configuration config1, HL7Configuration config2, List<string> differences)
    {
        if (config1.DefaultVersion != config2.DefaultVersion)
            differences.Add($"HL7 Version: '{config1.DefaultVersion}' vs '{config2.DefaultVersion}'");
            
        if (config1.DefaultSendingApplication != config2.DefaultSendingApplication)
            differences.Add($"Sending Application: '{config1.DefaultSendingApplication}' vs '{config2.DefaultSendingApplication}'");
            
        if (config1.FieldSeparator != config2.FieldSeparator)
            differences.Add($"Field Separator: '{config1.FieldSeparator}' vs '{config2.FieldSeparator}'");
    }
    
    private static void CompareValidationConfigurations(ValidationConfiguration config1, ValidationConfiguration config2, List<string> differences)
    {
        if (config1.StrictMode != config2.StrictMode)
            differences.Add($"Strict Mode: {config1.StrictMode} vs {config2.StrictMode}");
            
        if (config1.ValidateRequiredFields != config2.ValidateRequiredFields)
            differences.Add($"Validate Required Fields: {config1.ValidateRequiredFields} vs {config2.ValidateRequiredFields}");
    }
}