// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Application.Services.Configuration;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.CLI.Services;
using System.CommandLine;
using System.Text.Json;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Command for managing Pidgeon configuration intelligence with multi-standard support.
/// Implements smart standard inference across HL7, FHIR, NCPDP, and other healthcare standards.
/// </summary>
public class ConfigCommand : CommandBuilderBase
{
    private readonly IMultiStandardVendorDetectionService _vendorDetectionService;
    private readonly IConfigurationQuery _query;
    private readonly IConfigurationAnalytics _analytics;
    private readonly ConfigurationStorage _storage;

    public ConfigCommand(
        ILogger<ConfigCommand> logger, 
        IMultiStandardVendorDetectionService vendorDetectionService,
        IConfigurationQuery query,
        IConfigurationAnalytics analytics) 
        : base(logger)
    {
        _vendorDetectionService = vendorDetectionService ?? throw new ArgumentNullException(nameof(vendorDetectionService));
        _query = query ?? throw new ArgumentNullException(nameof(query));
        _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        _storage = new ConfigurationStorage();
    }

    public override Command CreateCommand()
    {
        var command = new Command("config", "Manage configuration intelligence");

        // Add subcommands
        command.Add(CreateAnalyzeCommand());
        command.Add(CreateListCommand());
        command.Add(CreateShowCommand());
        command.Add(CreateUseCommand());
        command.Add(CreateExportCommand());
        command.Add(CreateDiffCommand());
        command.Add(CreateStatsCommand());

        return command;
    }

    private Command CreateAnalyzeCommand()
    {
        // Positional argument for samples path
        var samplesArg = new Argument<string>("samples")
        {
            Description = "Path to file or directory containing sample messages (auto-detects HL7/FHIR/NCPDP)"
        };

        // Options with short flags
        var saveOption = CreateNullableOption("--save", "-s", "Save inferred configuration to file (JSON format)");
        var seedOption = CreateNullableOption("--seed", "Deterministic seed for reproducible analysis");
        var minConfidenceOption = CreateNullableOption("--min-confidence", "-c", "Minimum confidence threshold (0.0-1.0, default: 0.6)");
        var verboseOption = CreateBooleanOption("--verbose", "-v", "Show detailed analysis progress");

        // Redundant option for backward compatibility
        var samplesOption = CreateNullableOption("--samples", "Path to samples (redundant - use positional arg)");

        var command = new Command("analyze", "Infer vendor patterns from sample messages (auto-detects HL7/FHIR/NCPDP)")
        {
            samplesArg, saveOption, seedOption, minConfidenceOption, verboseOption, samplesOption
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            // Get samples path from positional arg or fallback to option
            var samples = parseResult.GetValue(samplesArg) ?? parseResult.GetValue(samplesOption);

            if (string.IsNullOrEmpty(samples))
            {
                Console.WriteLine("Error: Samples path is required. Usage:");
                Console.WriteLine("  pidgeon config analyze <samples-path>");
                Console.WriteLine("  pidgeon config analyze --samples <samples-path>");
                return 1;
            }
            var save = parseResult.GetValue(saveOption);
            var seedValue = parseResult.GetValue(seedOption);
            var minConfidenceValue = parseResult.GetValue(minConfidenceOption);
            var verbose = parseResult.GetValue(verboseOption);

            // Parse optional values
            int? seed = seedValue != null && int.TryParse(seedValue, out var s) ? s : null;
            double minConfidence = minConfidenceValue != null && double.TryParse(minConfidenceValue, out var mc) ? mc : 0.6;

            Console.WriteLine($"Analyzing vendor patterns from: {samples}");
            if (verbose)
            {
                Console.WriteLine("Smart standard inference enabled - auto-detecting HL7/FHIR/NCPDP");
            }

            // Load messages from samples path
            var messagesResult = await LoadMessagesFromPathAsync(samples, cancellationToken);
            if (messagesResult.IsFailure)
            {
                Console.WriteLine($"Error: {messagesResult.Error}");
                return 1;
            }

            var messages = messagesResult.Value;
            if (!messages.Any())
            {
                Console.WriteLine("Error: No valid messages found in samples path");
                return 1;
            }

            if (verbose)
            {
                Console.WriteLine($"Loaded {messages.Count} messages for analysis");
            }

            // Create inference options
            var options = new InferenceOptions
            {
                MinimumConfidence = minConfidence,
                Seed = seed
            };

            // Analyze vendor patterns using multi-standard detection
            Console.WriteLine("Analyzing vendor patterns with smart standard inference...");
            var analysisResult = await _vendorDetectionService.AnalyzeVendorPatternsAsync(messages, options);
            if (analysisResult.IsFailure)
            {
                Console.WriteLine($"Error: Analysis failed: {analysisResult.Error}");
                return 1;
            }

            var vendorConfig = analysisResult.Value;
            
            // Display results
            Console.WriteLine();
            Console.WriteLine("Vendor Pattern Analysis Complete");
            Console.WriteLine($"   Vendor: {vendorConfig.Address.Vendor}");
            Console.WriteLine($"   Standard: {vendorConfig.Address.Standard}");
            Console.WriteLine($"   Confidence: {vendorConfig.Metadata.Confidence:P1}");
            Console.WriteLine($"   Messages Analyzed: {vendorConfig.Metadata.MessagesSampled}");
            Console.WriteLine($"   Message Types: {string.Join(", ", vendorConfig.MessagePatterns.Keys)}");

            if (verbose)
            {
                Console.WriteLine();
                Console.WriteLine("Field Usage Summary:");
                DisplayFieldPatternsSummary(vendorConfig.FieldPatterns);
            }

            // Save configuration if requested
            if (!string.IsNullOrEmpty(save))
            {
                var vendorName = vendorConfig.Address.Vendor;
                var savedPath = await _storage.SaveConfigurationAsync(vendorConfig, save, vendorName);
                Console.WriteLine($"Configuration saved to: {savedPath}");
                Console.WriteLine($"Configuration directory: {_storage.ConfigDirectory}");
            }

            Console.WriteLine();
            Console.WriteLine("Next steps:");
            var configName = save ?? $"{vendorConfig.Address.Vendor.ToLower()}_config.json";
            Console.WriteLine($"   pidgeon config use --name {configName}");
            Console.WriteLine("   pidgeon validate --file message.hl7 --mode compatibility");

            return 0;
        });

        return command;
    }

    private Command CreateListCommand()
    {
        var vendorOption = CreateNullableOption("--vendor", "Filter by vendor name");
        var standardOption = CreateNullableOption("--standard", "Filter by standard (hl7|fhir|ncpdp)");
        var formatOption = CreateNullableOption("--format", "Output format (table|json|yaml, default: table)");

        var command = new Command("list", "List available vendor configurations")
        {
            vendorOption, standardOption, formatOption
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var vendor = parseResult.GetValue(vendorOption);
            var standard = parseResult.GetValue(standardOption);
            var format = parseResult.GetValue(formatOption) ?? "table";

            Console.WriteLine("Available Vendor Configurations");
            Console.WriteLine();

            // Get configurations using multi-standard service
            Result<IReadOnlyList<VendorConfiguration>> result;

            if (!string.IsNullOrEmpty(standard))
            {
                result = await _vendorDetectionService.GetVendorConfigurationsForStandardAsync(standard);
            }
            else
            {
                result = await _vendorDetectionService.GetAllVendorConfigurationsAsync();
            }

            if (result.IsFailure)
            {
                Console.WriteLine($"Error: Failed to load configurations: {result.Error}");
                return 1;
            }

            var configurations = result.Value;

            // Apply vendor filter if specified
            var filtered = configurations.AsEnumerable();
            if (!string.IsNullOrEmpty(vendor))
            {
                filtered = filtered.Where(c => c.Address.Vendor.Contains(vendor, StringComparison.OrdinalIgnoreCase));
            }

            var filteredList = filtered.ToList();

            if (!filteredList.Any())
            {
                Console.WriteLine("No configurations found matching the specified filters.");
                return 0;
            }

            // Display configurations based on format
            DisplayConfigurationList(filteredList, format);

            Console.WriteLine();
            Console.WriteLine($"Found {filteredList.Count} configurations");
            if (configurations.Count != filteredList.Count)
            {
                Console.WriteLine($"({configurations.Count} total available)");
            }

            return 0;
        });

        return command;
    }

    private Command CreateShowCommand()
    {
        var nameOption = CreateRequiredOption("--name", "Configuration file or ID to display");

        var command = new Command("show", "Show detailed configuration information")
        {
            nameOption
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameOption)!;
            
            // Support both file paths and configuration addresses
            VendorConfiguration? config = null;
            
            // Try loading from storage first
            config = await _storage.LoadConfigurationAsync<VendorConfiguration>(name);
            
            if (config == null && ConfigurationAddress.TryParse(name, out var address) && address != null)
            {
                // Load from configuration store
                var result = await _query.GetConfigurationAsync(address);
                if (result.IsFailure)
                {
                    Console.WriteLine($"Error: {result.Error}");
                    return 1;
                }
                config = result.Value;
            }
            else
            {
                Console.WriteLine($"Error: Configuration not found: {name}");
                Console.WriteLine("Expected: file path or configuration address (Vendor-Standard-MessageType)");
                return 1;
            }

            if (config == null)
            {
                Console.WriteLine($"Error: Configuration not found: {name}");
                return 1;
            }

            Console.WriteLine($"Configuration: {config.Address}");
            Console.WriteLine($"Vendor: {config.Signature.Name} v{config.Signature.Version}");
            Console.WriteLine($"Confidence: {config.Metadata.Confidence:P1}");
            Console.WriteLine($"Messages Analyzed: {config.Metadata.MessagesSampled}");
            Console.WriteLine($"First Seen: {config.Metadata.FirstSeen:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Last Updated: {config.Metadata.LastUpdated:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Version: {config.Metadata.Version}");
            
            if (config.MessagePatterns.Count > 0)
            {
                Console.WriteLine("\nMessage Patterns:");
                foreach (var pattern in config.MessagePatterns)
                {
                    Console.WriteLine($"  • {pattern.Key}: {pattern.Value.Frequency} occurrences");
                }
            }

            if (config.FieldPatterns.SegmentPatterns.Count > 0)
            {
                Console.WriteLine("\nTop Field Patterns:");
                DisplayFieldPatternsSummary(config.FieldPatterns);
            }

            return 0;
        });

        return command;
    }

    private Command CreateUseCommand()
    {
        var nameOption = CreateRequiredOption("--name", "Configuration file or ID to activate");

        var command = new Command("use", "Set active pattern for this project/profile")
        {
            nameOption
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameOption)!;
            
            // TODO: Implement profile/project configuration management
            if (_storage.ConfigExists(name))
            {
                Console.WriteLine($"Configuration set to: {name}");
                Console.WriteLine($"Configuration directory: {_storage.ConfigDirectory}");
                Console.WriteLine("Note: Configuration management not yet implemented");
                Console.WriteLine("    This will be used for subsequent validate/generate commands");
                return 0;
            }
            else if (ConfigurationAddress.TryParse(name, out var address) && address != null)
            {
                var result = await _query.GetConfigurationAsync(address);
                if (result.IsFailure)
                {
                    Console.WriteLine($"Error: Configuration not found: {name}");
                    return 1;
                }
                
                Console.WriteLine($"Configuration set to: {address}");
                Console.WriteLine("Note: Configuration management not yet implemented");
                return 0;
            }
            else
            {
                Console.WriteLine($"Error: Configuration not found: {name}");
                return 1;
            }
        });

        return command;
    }

    private Command CreateExportCommand()
    {
        var nameOption = CreateRequiredOption("--name", "Configuration ID to export");
        var outOption = CreateRequiredOption("--out", "Output file path");

        var command = new Command("export", "Export pattern as JSON")
        {
            nameOption, outOption
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameOption)!;
            var outPath = parseResult.GetValue(outOption)!;

            if (!ConfigurationAddress.TryParse(name, out var address) || address == null)
            {
                Console.WriteLine($"Error: Invalid configuration address: {name}");
                Console.WriteLine("Expected format: Vendor-Standard-MessageType");
                return 1;
            }

            var result = await _query.GetConfigurationAsync(address);
            if (result.IsFailure)
            {
                Console.WriteLine($"Error: Configuration not found: {name}");
                return 1;
            }

            var config = result.Value;
            if (config == null)
            {
                Console.WriteLine($"Error: Configuration not found: {name}");
                return 1;
            }

            var vendorName = config.Address.Vendor;
            var savedPath = await _storage.SaveConfigurationAsync(config, outPath, vendorName);
            Console.WriteLine($"Configuration exported to: {savedPath}");

            return 0;
        });

        return command;
    }

    private Command CreateDiffCommand()
    {
        var leftOption = CreateRequiredOption("--left", "Left configuration ID");
        var rightOption = CreateRequiredOption("--right", "Right configuration ID");
        var reportOption = CreateNullableOption("--report", "Output report file (optional)");

        var command = new Command("diff", "Compare two patterns (rules and impacts)")
        {
            leftOption, rightOption, reportOption
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            await Task.Yield();
            var leftName = parseResult.GetValue(leftOption)!;
            var rightName = parseResult.GetValue(rightOption)!;
            var reportPath = parseResult.GetValue(reportOption);

            Console.WriteLine($"Comparing configurations: {leftName} vs {rightName}");
            
            // TODO: Implement configuration comparison logic
            Console.WriteLine("Configuration diff not yet implemented");
            Console.WriteLine("    Will compare field patterns, coverage, and vendor signatures");
            
            if (!string.IsNullOrEmpty(reportPath))
            {
                Console.WriteLine($"    Report will be saved to: {reportPath}");
            }

            return 0;
        });

        return command;
    }

    private Command CreateStatsCommand()
    {
        var command = new Command("stats", "Show configuration catalog statistics");

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var result = await _analytics.GetStatisticsAsync();

            if (result.IsFailure)
            {
                Console.WriteLine($"Error: {result.Error}");
                return 1;
            }

            var stats = result.Value;
            Console.WriteLine("Configuration Catalog Statistics");
            Console.WriteLine("================================");
            Console.WriteLine($"Total Configurations: {stats.TotalConfigurations}");
            Console.WriteLine($"Unique Vendors: {stats.UniqueVendors}");
            Console.WriteLine($"Unique Standards: {stats.UniqueStandards}");
            Console.WriteLine($"Unique Message Types: {stats.UniqueMessageTypes}");
            Console.WriteLine($"Total Messages Analyzed: {stats.TotalMessagesAnalyzed}");
            Console.WriteLine($"Average Confidence: {stats.AverageConfidence:P1}");
            Console.WriteLine($"Last Updated: {stats.LastUpdated:yyyy-MM-dd HH:mm:ss}");

            if (stats.VendorCounts.Count > 0)
            {
                Console.WriteLine("\nVendor Breakdown:");
                foreach (var kvp in stats.VendorCounts.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"  • {kvp.Key}: {kvp.Value} configurations");
                }
            }

            if (stats.StandardCounts.Count > 0)
            {
                Console.WriteLine("\nStandard Breakdown:");
                foreach (var kvp in stats.StandardCounts.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"  • {kvp.Key}: {kvp.Value} configurations");
                }
            }

            return 0;
        });

        return command;
    }

    private async Task<Result<List<string>>> LoadMessagesFromPathAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<string>();

            if (File.Exists(path))
            {
                var content = await File.ReadAllTextAsync(path, cancellationToken);
                messages.Add(content);
            }
            else if (Directory.Exists(path))
            {
                var extensions = new[] { "*.hl7", "*.txt", "*.dat", "*.json", "*.xml" };
                var files = extensions
                    .SelectMany(ext => Directory.GetFiles(path, ext, SearchOption.AllDirectories))
                    .ToArray();

                if (files.Length == 0)
                {
                    return Result<List<string>>.Failure($"No healthcare message files found in directory: {path}");
                }

                foreach (var file in files)
                {
                    var content = await File.ReadAllTextAsync(file, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        messages.Add(content);
                    }
                }
            }
            else
            {
                return Result<List<string>>.Failure($"Path not found: {path}");
            }

            if (messages.Count == 0)
            {
                return Result<List<string>>.Failure("No valid messages found in the specified path");
            }

            return Result<List<string>>.Success(messages);
        }
        catch (Exception ex)
        {
            return Result<List<string>>.Failure($"Error loading messages: {ex.Message}");
        }
    }


    private static void DisplayConfigurationList(IEnumerable<VendorConfiguration> configurations, string format)
    {
        switch (format.ToLowerInvariant())
        {
            case "json":
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(configurations, jsonOptions);
                Console.WriteLine(json);
                break;

            case "yaml":
                Console.WriteLine("YAML output not yet implemented");
                break;

            case "table":
            default:
                Console.WriteLine($"{"Vendor",-20} {"Standard",-10} {"Message Type",-15} {"Confidence",-12} {"Messages",-10}");
                Console.WriteLine(new string('-', 75));
                
                foreach (var config in configurations)
                {
                    Console.WriteLine($"{config.Address.Vendor,-20} " +
                                    $"{config.Address.Standard,-10} " +
                                    $"{config.Address.MessageType,-15} " +
                                    $"{config.Metadata.Confidence:P1,-12} " +
                                    $"{config.Metadata.MessagesSampled,-10}");
                }
                break;
        }
    }

    private static void DisplayFieldPatternsSummary(FieldPatterns fieldPatterns)
    {
        var allFields = new List<(string Path, double Frequency)>();
        
        // Extract field frequencies from all segments
        foreach (var (segmentId, segmentPattern) in fieldPatterns.SegmentPatterns)
        {
            foreach (var (fieldIndex, fieldFreq) in segmentPattern.Fields)
            {
                var path = $"{segmentId}.{fieldIndex}";
                allFields.Add((path, fieldFreq.Frequency));
            }
        }
        
        var sortedFields = allFields
            .OrderByDescending(f => f.Frequency)
            .Take(20)
            .ToList();

        foreach (var (path, frequency) in sortedFields)
        {
            var bar = new string('█', (int)(frequency * 20));
            Console.WriteLine($"  {path,-20} [{bar,-20}] {frequency:P1}");
        }

        if (allFields.Count > 20)
        {
            Console.WriteLine($"  ... and {allFields.Count - 20} more fields");
        }
    }

    private static Option<bool> CreateFlag(string name, string description)
    {
        return new Option<bool>(name)
        {
            Description = description,
            DefaultValueFactory = _ => false
        };
    }
}