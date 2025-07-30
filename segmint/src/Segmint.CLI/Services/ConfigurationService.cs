// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Segmint.Core.Configuration;
using Segmint.CLI.Serialization;

namespace Segmint.CLI.Services;

/// <summary>
/// Implementation of configuration management service.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = SegmintJsonContext.Default
        };
    }

    /// <inheritdoc />
    public async Task<SegmintConfiguration> LoadConfigurationAsync(
        string configurationPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading configuration from {ConfigurationPath}", configurationPath);

        if (!File.Exists(configurationPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configurationPath}");
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(configurationPath, cancellationToken);
            var configuration = JsonSerializer.Deserialize<SegmintConfiguration>(jsonContent, _jsonOptions);
            
            if (configuration == null)
            {
                throw new InvalidOperationException("Failed to deserialize configuration file");
            }

            _logger.LogInformation("Successfully loaded configuration: {Name}", configuration.Interface.Name);
            return configuration;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in configuration file: {ConfigurationPath}", configurationPath);
            throw new InvalidOperationException($"Invalid JSON in configuration file: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task SaveConfigurationAsync(
        SegmintConfiguration configuration,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving configuration {Name} to {OutputPath}", configuration.Interface.Name, outputPath);

        try
        {
            // Ensure output directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var jsonContent = JsonSerializer.Serialize(configuration, _jsonOptions);
            await File.WriteAllTextAsync(outputPath, jsonContent, cancellationToken);
            
            _logger.LogInformation("Successfully saved configuration to {OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {OutputPath}", outputPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ConfigurationValidationResult> ValidateConfigurationAsync(
        string configurationPath,
        bool strictMode = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating configuration file {ConfigurationPath}", configurationPath);

        var result = new ConfigurationValidationResult();

        try
        {
            // Load and parse configuration
            var configuration = await LoadConfigurationAsync(configurationPath, cancellationToken);

            // Basic validation rules
            if (string.IsNullOrWhiteSpace(configuration.Interface.Name))
            {
                result.Errors.Add("Configuration name is required");
            }

            if (string.IsNullOrWhiteSpace(configuration.Interface.Version))
            {
                result.Warnings.Add("Configuration version is not specified");
            }

            // Validate message types
            if (configuration.Interface.SupportedMessageTypes == null || !configuration.Interface.SupportedMessageTypes.Any())
            {
                result.Errors.Add("At least one message type must be configured");
            }
            else
            {
                foreach (var messageType in configuration.Interface.SupportedMessageTypes)
                {
                    if (string.IsNullOrWhiteSpace(messageType))
                    {
                        result.Errors.Add("Message type name cannot be empty");
                    }
                }
            }

            // Validate segments
            if (configuration.HL7.CustomSegments == null || !configuration.HL7.CustomSegments.Any())
            {
                result.Warnings.Add("No custom segments configured");
            }

            // Additional strict mode validations
            if (strictMode)
            {
                // Add more rigorous validation rules
                if (configuration.Validation.CustomRules == null || !configuration.Validation.CustomRules.Any())
                {
                    result.Warnings.Add("No custom validation rules configured");
                }
            }

            result.IsValid = !result.Errors.Any();
            
            if (result.IsValid)
            {
                result.Information.Add("Configuration is valid");
                _logger.LogInformation("Configuration validation passed for {ConfigurationPath}", configurationPath);
            }
            else
            {
                _logger.LogWarning("Configuration validation failed for {ConfigurationPath} with {ErrorCount} errors", 
                    configurationPath, result.Errors.Count);
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Failed to validate configuration: {ex.Message}");
            _logger.LogError(ex, "Error validating configuration file {ConfigurationPath}", configurationPath);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ConfigurationComparisonResult> CompareConfigurationsAsync(
        string config1Path,
        string config2Path,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Comparing configurations {Config1Path} and {Config2Path}", config1Path, config2Path);

        var result = new ConfigurationComparisonResult();

        try
        {
            var config1 = await LoadConfigurationAsync(config1Path, cancellationToken);
            var config2 = await LoadConfigurationAsync(config2Path, cancellationToken);

            // Compare basic properties
            CompareProperty(result, "Name", config1.Interface.Name, config2.Interface.Name);
            CompareProperty(result, "Version", config1.Interface.Version, config2.Interface.Version);
            CompareProperty(result, "Description", config1.Interface.Description, config2.Interface.Description);

            // Compare message types
            CompareMessageTypes(result, config1.Interface.SupportedMessageTypes, config2.Interface.SupportedMessageTypes);

            // Compare segments
            CompareSegments(result, config1.HL7.CustomSegments, config2.HL7.CustomSegments);

            result.AreIdentical = !result.Differences.Any();
            
            if (result.AreIdentical)
            {
                result.Summary = "Configurations are identical";
                _logger.LogInformation("Configurations are identical");
            }
            else
            {
                result.Summary = $"Found {result.Differences.Count} differences between configurations";
                _logger.LogInformation("Found {DifferenceCount} differences between configurations", result.Differences.Count);
            }
        }
        catch (Exception ex)
        {
            result.Summary = $"Error comparing configurations: {ex.Message}";
            _logger.LogError(ex, "Error comparing configurations");
            throw;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<SegmintConfiguration> AnalyzeAndInferConfigurationAsync(
        string inputPath,
        string? configurationName = null,
        int sampleSize = 100,
        bool includeStats = false,
        string[]? segments = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing HL7 messages from {InputPath} to infer configuration", inputPath);

        // This is a placeholder implementation
        // In a full implementation, this would analyze actual HL7 messages
        // and infer the configuration structure
        
        var configuration = new SegmintConfiguration
        {
            Interface = new InterfaceConfiguration
            {
                Name = configurationName ?? $"Inferred_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                Description = $"Configuration inferred from analysis of {inputPath}"
            }
        };

        // TODO: Implement actual message analysis logic
        // - Read HL7 messages from inputPath
        // - Parse message structure
        // - Identify segments and fields
        // - Determine data types and constraints
        // - Generate configuration based on analysis

        _logger.LogInformation("Configuration inference completed for {InputPath}", inputPath);
        
        return await Task.FromResult(configuration);
    }

    /// <inheritdoc />
    public IEnumerable<ConfigurationTemplate> GetAvailableTemplates()
    {
        // Return built-in templates
        return new[]
        {
            new ConfigurationTemplate
            {
                Name = "standard-rde",
                Description = "Standard RDE (Pharmacy Order) message template",
                Category = "Pharmacy",
                MessageTypes = new List<string> { "RDE" },
                Version = "1.0.0"
            },
            new ConfigurationTemplate
            {
                Name = "standard-adt",
                Description = "Standard ADT (Patient Administration) message template",
                Category = "Patient",
                MessageTypes = new List<string> { "ADT" },
                Version = "1.0.0"
            },
            new ConfigurationTemplate
            {
                Name = "standard-ack",
                Description = "Standard ACK (Acknowledgment) message template",
                Category = "System",
                MessageTypes = new List<string> { "ACK" },
                Version = "1.0.0"
            }
        };
    }

    /// <inheritdoc />
    public async Task<SegmintConfiguration> LoadTemplateAsync(string templateName)
    {
        _logger.LogInformation("Loading configuration template {TemplateName}", templateName);

        // In a full implementation, templates would be loaded from embedded resources
        // or a templates directory
        var configuration = new SegmintConfiguration
        {
            Interface = new InterfaceConfiguration
            {
                Name = templateName,
                Version = "1.0.0",
                Description = $"Template configuration for {templateName}"
            }
        };

        return await Task.FromResult(configuration);
    }

    /// <summary>
    /// Compares a property between two configurations.
    /// </summary>
    private void CompareProperty(ConfigurationComparisonResult result, string propertyName, string? value1, string? value2)
    {
        if (value1 != value2)
        {
            result.Differences.Add(new ConfigurationDifference
            {
                Path = propertyName,
                Type = DifferenceType.Modified,
                Value1 = value1,
                Value2 = value2,
                Description = $"{propertyName} changed from '{value1}' to '{value2}'"
            });
        }
    }

    /// <summary>
    /// Compares message types between two configurations.
    /// </summary>
    private void CompareMessageTypes(ConfigurationComparisonResult result, 
        List<string>? messageTypes1, 
        List<string>? messageTypes2)
    {
        messageTypes1 ??= new List<string>();
        messageTypes2 ??= new List<string>();

        // Find added message types
        foreach (var messageType in messageTypes2.Except(messageTypes1))
        {
            result.Differences.Add(new ConfigurationDifference
            {
                Path = $"MessageTypes.{messageType}",
                Type = DifferenceType.Added,
                Value2 = messageType,
                Description = $"Message type '{messageType}' was added"
            });
        }

        // Find removed message types
        foreach (var messageType in messageTypes1.Except(messageTypes2))
        {
            result.Differences.Add(new ConfigurationDifference
            {
                Path = $"MessageTypes.{messageType}",
                Type = DifferenceType.Removed,
                Value1 = messageType,
                Description = $"Message type '{messageType}' was removed"
            });
        }
    }

    /// <summary>
    /// Compares segments between two configurations.
    /// </summary>
    private void CompareSegments(ConfigurationComparisonResult result,
        Dictionary<string, SegmentDefinition>? segments1,
        Dictionary<string, SegmentDefinition>? segments2)
    {
        segments1 ??= new Dictionary<string, SegmentDefinition>();
        segments2 ??= new Dictionary<string, SegmentDefinition>();

        // Find added segments
        foreach (var segment in segments2.Keys.Except(segments1.Keys))
        {
            result.Differences.Add(new ConfigurationDifference
            {
                Path = $"Segments.{segment}",
                Type = DifferenceType.Added,
                Value2 = segment,
                Description = $"Segment '{segment}' was added"
            });
        }

        // Find removed segments
        foreach (var segment in segments1.Keys.Except(segments2.Keys))
        {
            result.Differences.Add(new ConfigurationDifference
            {
                Path = $"Segments.{segment}",
                Type = DifferenceType.Removed,
                Value1 = segment,
                Description = $"Segment '{segment}' was removed"
            });
        }
    }
}