// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.CommandLine;
// using System.CommandLine.NamingConventionBinders; // Temporarily commented out due to version mismatch
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Segmint.Core.Configuration.Inference;

namespace Segmint.CLI.Commands;

/// <summary>
/// CLI commands for configuration inference and management.
/// </summary>
public static class ConfigurationCommands
{
    /// <summary>
    /// Creates the main configuration command with subcommands.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <returns>The configuration command.</returns>
    public static Command CreateConfigCommand(IServiceProvider serviceProvider)
    {
        var configCommand = new Command("config", "Manage vendor-specific HL7 configurations")
        {
            CreateInferCommand(serviceProvider),
            CreateValidateCommand(serviceProvider),
            CreateDiffCommand(serviceProvider),
            CreateListCommand(serviceProvider),
            CreateExportCommand(serviceProvider),
            CreateUpdateCommand(serviceProvider),
            CreateDeleteCommand(serviceProvider)
        };

        return configCommand;
    }

    /// <summary>
    /// Creates the configuration inference command.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <returns>The infer command.</returns>
    private static Command CreateInferCommand(IServiceProvider serviceProvider)
    {
        var samplesOption = new Option<DirectoryInfo>(
            aliases: new[] { "--samples", "-s" },
            description: "Directory containing sample HL7 messages");
        samplesOption.IsRequired = true;

        var outputOption = new Option<FileInfo>(
            aliases: new[] { "--output", "-o" },
            description: "Output file for inferred configuration");
        outputOption.IsRequired = true;

        var vendorOption = new Option<string>(
            aliases: new[] { "--vendor", "-v" },
            description: "Vendor or system name (e.g., 'Epic', 'Cerner')");
        vendorOption.IsRequired = true;

        var messageTypeOption = new Option<string>(
            aliases: new[] { "--message-type", "-t" },
            description: "HL7 message type (e.g., 'ORM^O01', 'ADT^A01')");
        messageTypeOption.IsRequired = true;

        var confidenceOption = new Option<double>(
            aliases: new[] { "--confidence", "-c" },
            getDefaultValue: () => 0.7,
            description: "Minimum confidence threshold (0.0-1.0)");

        var formatOption = new Option<string>(
            aliases: new[] { "--format", "-f" },
            getDefaultValue: () => "json",
            description: "Output format (json, yaml)");

        var command = new Command("infer", "Infer vendor configuration from sample HL7 messages")
        {
            samplesOption,
            outputOption,
            vendorOption,
            messageTypeOption,
            confidenceOption,
            formatOption
        };

        command.SetHandler(async (samplesDir, outputFile, vendor, messageType, confidence, format) =>
        {
            await HandleInferCommand(serviceProvider, samplesDir, outputFile, vendor, messageType, confidence, format);
        }, samplesOption, outputOption, vendorOption, messageTypeOption, confidenceOption, formatOption);

        return command;
    }

    /// <summary>
    /// Creates the configuration validation command.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <returns>The validate command.</returns>
    private static Command CreateValidateCommand(IServiceProvider serviceProvider)
    {
        var configOption = new Option<FileInfo>(
            aliases: new[] { "--config", "-c" },
            description: "Configuration file to validate against");
        configOption.IsRequired = true;

        var messageOption = new Option<FileInfo>(
            aliases: new[] { "--message", "-m" },
            description: "HL7 message file to validate");
        messageOption.IsRequired = true;

        var outputOption = new Option<FileInfo?>(
            aliases: new[] { "--output", "-o" },
            description: "Output file for validation results (optional)");

        var command = new Command("validate", "Validate HL7 message against vendor configuration")
        {
            configOption,
            messageOption,
            outputOption
        };

        command.SetHandler(async (configFile, messageFile, outputFile) =>
        {
            await HandleValidateCommand(serviceProvider, configFile, messageFile, outputFile);
        }, configOption, messageOption, outputOption);

        return command;
    }

    /// <summary>
    /// Creates the configuration diff command.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <returns>The diff command.</returns>
    private static Command CreateDiffCommand(IServiceProvider serviceProvider)
    {
        var baselineOption = new Option<FileInfo>(
            aliases: new[] { "--baseline", "-b" },
            description: "Baseline configuration file");
        baselineOption.IsRequired = true;

        var currentOption = new Option<FileInfo>(
            aliases: new[] { "--current", "-c" },
            description: "Current configuration file to compare");
        currentOption.IsRequired = true;

        var outputOption = new Option<FileInfo?>(
            aliases: new[] { "--output", "-o" },
            description: "Output file for diff results (optional)");

        var command = new Command("diff", "Compare two vendor configurations")
        {
            baselineOption,
            currentOption,
            outputOption
        };

        command.SetHandler(async (baselineFile, currentFile, outputFile) =>
        {
            await HandleDiffCommand(serviceProvider, baselineFile, currentFile, outputFile);
        }, baselineOption, currentOption, outputOption);

        return command;
    }

    /// <summary>
    /// Creates the configuration list command.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <returns>The list command.</returns>
    private static Command CreateListCommand(IServiceProvider serviceProvider)
    {
        var vendorOption = new Option<string?>(
            aliases: new[] { "--vendor", "-v" },
            description: "Filter by vendor name");

        var messageTypeOption = new Option<string?>(
            aliases: new[] { "--message-type", "-t" },
            description: "Filter by message type");

        var command = new Command("list", "List available vendor configurations")
        {
            vendorOption,
            messageTypeOption
        };

        command.SetHandler(async (vendor, messageType) =>
        {
            await HandleListCommand(serviceProvider, vendor, messageType);
        }, vendorOption, messageTypeOption);

        return command;
    }

    /// <summary>
    /// Creates the configuration export command.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <returns>The export command.</returns>
    private static Command CreateExportCommand(IServiceProvider serviceProvider)
    {
        var configIdOption = new Option<string>(
            aliases: new[] { "--config-id", "-c" },
            description: "Configuration ID to export");
        configIdOption.IsRequired = true;

        var outputOption = new Option<FileInfo>(
            aliases: new[] { "--output", "-o" },
            description: "Output file for exported configuration");
        outputOption.IsRequired = true;

        var formatOption = new Option<string>(
            aliases: new[] { "--format", "-f" },
            getDefaultValue: () => "json",
            description: "Export format (json, yaml, openapi)");

        var command = new Command("export", "Export configuration to file")
        {
            configIdOption,
            outputOption,
            formatOption
        };

        command.SetHandler(async (configId, outputFile, format) =>
        {
            await HandleExportCommand(serviceProvider, configId, outputFile, format);
        }, configIdOption, outputOption, formatOption);

        return command;
    }

    /// <summary>
    /// Creates the configuration update command.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <returns>The update command.</returns>
    private static Command CreateUpdateCommand(IServiceProvider serviceProvider)
    {
        var configIdOption = new Option<string>(
            aliases: new[] { "--config-id", "-c" },
            description: "Configuration ID to update");
        configIdOption.IsRequired = true;

        var samplesOption = new Option<DirectoryInfo>(
            aliases: new[] { "--samples", "-s" },
            description: "Directory containing new sample messages");
        samplesOption.IsRequired = true;

        var command = new Command("update", "Update configuration with new sample data")
        {
            configIdOption,
            samplesOption
        };

        command.SetHandler(async (configId, samplesDir) =>
        {
            await HandleUpdateCommand(serviceProvider, configId, samplesDir);
        }, configIdOption, samplesOption);

        return command;
    }

    /// <summary>
    /// Creates the configuration delete command.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <returns>The delete command.</returns>
    private static Command CreateDeleteCommand(IServiceProvider serviceProvider)
    {
        var configIdOption = new Option<string>(
            aliases: new[] { "--config-id", "-c" },
            description: "Configuration ID to delete");
        configIdOption.IsRequired = true;

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Force deletion without confirmation");

        var command = new Command("delete", "Delete a configuration")
        {
            configIdOption,
            forceOption
        };

        command.SetHandler(async (configId, force) =>
        {
            await HandleDeleteCommand(serviceProvider, configId, force);
        }, configIdOption, forceOption);

        return command;
    }

    /// <summary>
    /// Handles the configuration inference command.
    /// </summary>
    private static async Task HandleInferCommand(IServiceProvider serviceProvider, DirectoryInfo samplesDir, FileInfo outputFile, 
        string vendor, string messageType, double confidence, string format)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var configService = serviceProvider.GetRequiredService<IConfigurationService>();

        try
        {
            if (!samplesDir.Exists)
            {
                Console.WriteLine($"Error: Samples directory '{samplesDir.FullName}' does not exist.");
                return;
            }

            Console.WriteLine($"Analyzing HL7 samples from {samplesDir.FullName}...");

            // Read all HL7 files from samples directory
            var hl7Files = samplesDir.GetFiles("*.hl7")
                .Concat(samplesDir.GetFiles("*.txt"))
                .ToList();

            if (!hl7Files.Any())
            {
                Console.WriteLine("No HL7 files found in samples directory. Looking for .hl7 and .txt files.");
                return;
            }

            var messages = new List<string>();
            foreach (var file in hl7Files)
            {
                var content = await File.ReadAllTextAsync(file.FullName);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    messages.Add(content);
                }
            }

            Console.WriteLine($"Found {messages.Count} HL7 messages in {hl7Files.Count} files.");

            // Infer configuration
            var configuration = await configService.InferFromSamples(messages, vendor, messageType, confidence);

            // Output configuration
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var configJson = JsonSerializer.Serialize(configuration, jsonOptions);

            await File.WriteAllTextAsync(outputFile.FullName, configJson);

            Console.WriteLine($"Configuration inferred successfully!");
            Console.WriteLine($"- Vendor: {configuration.Vendor}");
            Console.WriteLine($"- Message Type: {configuration.MessageType}");
            Console.WriteLine($"- Confidence: {configuration.InferredFrom.Confidence:F2}");
            Console.WriteLine($"- Segments: {configuration.Segments.Count}");
            Console.WriteLine($"- Validation Rules: {configuration.ValidationRules.Count}");
            Console.WriteLine($"- Saved to: {outputFile.FullName}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to infer configuration");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the configuration validation command.
    /// </summary>
    private static async Task HandleValidateCommand(IServiceProvider serviceProvider, FileInfo configFile, 
        FileInfo messageFile, FileInfo? outputFile)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            if (!configFile.Exists)
            {
                Console.WriteLine($"Error: Configuration file '{configFile.FullName}' does not exist.");
                return;
            }

            if (!messageFile.Exists)
            {
                Console.WriteLine($"Error: Message file '{messageFile.FullName}' does not exist.");
                return;
            }

            Console.WriteLine($"Validating message against configuration...");

            // Load configuration
            var configJson = await File.ReadAllTextAsync(configFile.FullName);
            var configuration = JsonSerializer.Deserialize<VendorConfiguration>(configJson);
            
            if (configuration == null)
            {
                Console.WriteLine("Error: Invalid configuration file format.");
                return;
            }

            // Load message
            var hl7Message = await File.ReadAllTextAsync(messageFile.FullName);

            // Validate
            var validator = new ConfigurationValidator(configuration);
            var result = validator.ValidateMessage(hl7Message);

            // Display results
            Console.WriteLine($"Validation Results:");
            Console.WriteLine($"- Overall Conformance: {result.OverallConformance:F2}");
            Console.WriteLine($"- Valid: {(result.IsValid ? "✓" : "✗")}");
            Console.WriteLine($"- Deviations: {result.Deviations.Count}");

            if (result.Deviations.Any())
            {
                Console.WriteLine("\nDeviations Found:");
                foreach (var deviation in result.Deviations.Take(10)) // Show first 10
                {
                    Console.WriteLine($"  • {deviation.Field}: {deviation.Description}");
                    Console.WriteLine($"    Expected: {deviation.Expected}, Actual: {deviation.Actual}");
                    Console.WriteLine($"    Severity: {deviation.Severity}, Confidence: {deviation.Confidence:F2}");
                }

                if (result.Deviations.Count > 10)
                {
                    Console.WriteLine($"  ... and {result.Deviations.Count - 10} more deviations");
                }
            }

            // Save detailed results if output specified
            if (outputFile != null)
            {
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var resultJson = JsonSerializer.Serialize(result, jsonOptions);
                await File.WriteAllTextAsync(outputFile.FullName, resultJson);
                Console.WriteLine($"\nDetailed results saved to: {outputFile.FullName}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate message");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the configuration diff command.
    /// </summary>
    private static async Task HandleDiffCommand(IServiceProvider serviceProvider, FileInfo baselineFile, 
        FileInfo currentFile, FileInfo? outputFile)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            if (!baselineFile.Exists)
            {
                Console.WriteLine($"Error: Baseline file '{baselineFile.FullName}' does not exist.");
                return;
            }

            if (!currentFile.Exists)
            {
                Console.WriteLine($"Error: Current file '{currentFile.FullName}' does not exist.");
                return;
            }

            Console.WriteLine("Comparing configurations...");

            // Load configurations
            var baselineJson = await File.ReadAllTextAsync(baselineFile.FullName);
            var currentJson = await File.ReadAllTextAsync(currentFile.FullName);

            var baselineConfig = JsonSerializer.Deserialize<VendorConfiguration>(baselineJson);
            var currentConfig = JsonSerializer.Deserialize<VendorConfiguration>(currentJson);

            if (baselineConfig == null || currentConfig == null)
            {
                Console.WriteLine("Error: Invalid configuration file format.");
                return;
            }

            // Compare
            var validator = new ConfigurationValidator(baselineConfig);
            var diff = validator.CompareConfigurations(currentConfig);

            // Display results
            Console.WriteLine($"Configuration Comparison:");
            Console.WriteLine($"- Baseline: {baselineConfig.Vendor} {baselineConfig.MessageType}");
            Console.WriteLine($"- Current: {currentConfig.Vendor} {currentConfig.MessageType}");
            Console.WriteLine($"- Similarity: {diff.Similarity:F2}");
            Console.WriteLine($"- Differences: {diff.Differences.Count}");

            if (diff.Differences.Any())
            {
                Console.WriteLine("\nDifferences Found:");
                foreach (var difference in diff.Differences.Take(10))
                {
                    Console.WriteLine($"  • {difference.Type}: {difference.Path}");
                    Console.WriteLine($"    Impact: {difference.Impact}");
                    Console.WriteLine($"    Description: {difference.Description}");
                }

                if (diff.Differences.Count > 10)
                {
                    Console.WriteLine($"  ... and {diff.Differences.Count - 10} more differences");
                }
            }

            // Save detailed results if output specified
            if (outputFile != null)
            {
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var diffJson = JsonSerializer.Serialize(diff, jsonOptions);
                await File.WriteAllTextAsync(outputFile.FullName, diffJson);
                Console.WriteLine($"\nDetailed diff saved to: {outputFile.FullName}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to compare configurations");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the configuration list command.
    /// </summary>
    private static async Task HandleListCommand(IServiceProvider serviceProvider, string? vendor, string? messageType)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var configService = serviceProvider.GetRequiredService<IConfigurationService>();

        try
        {
            var configurations = await configService.ListConfigurations(vendor, messageType);
            var configList = configurations.ToList();

            if (!configList.Any())
            {
                Console.WriteLine("No configurations found.");
                return;
            }

            Console.WriteLine($"Found {configList.Count} configuration(s):\n");
            Console.WriteLine($"{"Vendor",-15} {"Message Type",-12} {"Version",-10} {"Confidence",-10} {"Samples",-8} {"Created",-12} {"Config ID",-36}");
            Console.WriteLine(new string('-', 120));

            foreach (var config in configList)
            {
                Console.WriteLine($"{config.Vendor,-15} {config.MessageType,-12} {config.Version ?? "N/A",-10} {config.Confidence:F2,-10} {config.SampleCount,-8} {config.CreatedAt:MMM dd,-12} {config.ConfigurationId,-36}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list configurations");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the configuration export command.
    /// </summary>
    private static async Task HandleExportCommand(IServiceProvider serviceProvider, string configId, 
        FileInfo outputFile, string format)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var configService = serviceProvider.GetRequiredService<IConfigurationService>();

        try
        {
            var configuration = await configService.LoadConfiguration(configId);
            if (configuration == null)
            {
                Console.WriteLine($"Error: Configuration '{configId}' not found.");
                return;
            }

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var configJson = JsonSerializer.Serialize(configuration, jsonOptions);

            await File.WriteAllTextAsync(outputFile.FullName, configJson);

            Console.WriteLine($"Configuration exported successfully:");
            Console.WriteLine($"- Config ID: {configuration.ConfigurationId}");
            Console.WriteLine($"- Vendor: {configuration.Vendor}");
            Console.WriteLine($"- Message Type: {configuration.MessageType}");
            Console.WriteLine($"- Exported to: {outputFile.FullName}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export configuration");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the configuration update command.
    /// </summary>
    private static async Task HandleUpdateCommand(IServiceProvider serviceProvider, string configId, DirectoryInfo samplesDir)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var configService = serviceProvider.GetRequiredService<IConfigurationService>();

        try
        {
            if (!samplesDir.Exists)
            {
                Console.WriteLine($"Error: Samples directory '{samplesDir.FullName}' does not exist.");
                return;
            }

            Console.WriteLine($"Updating configuration {configId} with new samples...");

            // Read new sample messages
            var hl7Files = samplesDir.GetFiles("*.hl7")
                .Concat(samplesDir.GetFiles("*.txt"))
                .ToList();

            if (!hl7Files.Any())
            {
                Console.WriteLine("No HL7 files found in samples directory.");
                return;
            }

            var messages = new List<string>();
            foreach (var file in hl7Files)
            {
                var content = await File.ReadAllTextAsync(file.FullName);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    messages.Add(content);
                }
            }

            Console.WriteLine($"Found {messages.Count} new sample messages.");

            // Update configuration
            var updatedConfig = await configService.UpdateConfigurationFromSamples(configId, messages);

            Console.WriteLine($"Configuration updated successfully:");
            Console.WriteLine($"- Total Samples: {updatedConfig.InferredFrom.SampleCount}");
            Console.WriteLine($"- Confidence: {updatedConfig.InferredFrom.Confidence:F2}");
            Console.WriteLine($"- Segments: {updatedConfig.Segments.Count}");
            Console.WriteLine($"- Validation Rules: {updatedConfig.ValidationRules.Count}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update configuration");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the configuration delete command.
    /// </summary>
    private static async Task HandleDeleteCommand(IServiceProvider serviceProvider, string configId, bool force)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var configService = serviceProvider.GetRequiredService<IConfigurationService>();

        try
        {
            var configuration = await configService.LoadConfiguration(configId);
            if (configuration == null)
            {
                Console.WriteLine($"Error: Configuration '{configId}' not found.");
                return;
            }

            if (!force)
            {
                Console.WriteLine($"Are you sure you want to delete configuration '{configId}'?");
                Console.WriteLine($"- Vendor: {configuration.Vendor}");
                Console.WriteLine($"- Message Type: {configuration.MessageType}");
                Console.Write("Type 'yes' to confirm: ");
                
                var confirmation = Console.ReadLine();
                if (confirmation?.ToLowerInvariant() != "yes")
                {
                    Console.WriteLine("Delete cancelled.");
                    return;
                }
            }

            var deleted = await configService.DeleteConfiguration(configId);
            if (deleted)
            {
                Console.WriteLine($"Configuration '{configId}' deleted successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to delete configuration '{configId}'.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete configuration");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}