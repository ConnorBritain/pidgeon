// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Application.Services.Configuration;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Command for managing Pidgeon configuration intelligence.
/// </summary>
public class ConfigCommand : CommandBuilderBase
{
    private readonly IConfigurationCatalog _configCatalog;

    public ConfigCommand(ILogger<ConfigCommand> logger, IConfigurationCatalog configCatalog) 
        : base(logger)
    {
        _configCatalog = configCatalog;
    }

    public override Command CreateCommand()
    {
        var command = new Command("config", "Manage configuration intelligence");

        // Add subcommands
        command.Add(CreateAnalyzeCommand());
        command.Add(CreateListCommand());
        command.Add(CreateShowCommand());
        command.Add(CreateStatsCommand());

        return command;
    }

    private Command CreateAnalyzeCommand()
    {
        var samplesOption = CreateRequiredOption("--samples", "Path to directory containing sample messages");
        var vendorOption = CreateRequiredOption("--vendor", "Vendor name (e.g., Epic, Cerner)");
        var standardOption = CreateRequiredOption("--standard", "Standard name (e.g., HL7v23, FHIRv4)");
        var messageTypeOption = CreateRequiredOption("--type", "Message type (e.g., ADT^A01, Patient)");
        var outputOption = CreateNullableOption("--output", "Output file for configuration (optional)");

        var command = new Command("analyze", "Analyze messages and create vendor configuration")
        {
            samplesOption, vendorOption, standardOption, messageTypeOption, outputOption
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var samples = parseResult.GetValue(samplesOption)!;
            var vendor = parseResult.GetValue(vendorOption)!;
            var standard = parseResult.GetValue(standardOption)!;
            var messageType = parseResult.GetValue(messageTypeOption)!;
            var output = parseResult.GetValue(outputOption);

            Logger.LogInformation("Analyzing messages from: {Samples}", samples);

            var directoryCheck = ValidateDirectoryExists(samples);
            if (directoryCheck != 0) return directoryCheck;

            var messagesResult = await ReadMessagesFromDirectoryAsync(samples, "hl7", cancellationToken);
            if (messagesResult.IsFailure)
            {
                Console.WriteLine(messagesResult.Error);
                return 1;
            }

            var address = new ConfigurationAddress(vendor, standard, messageType);
            Console.WriteLine($"Analyzing {messagesResult.Value.Count} messages for {address}...");

            var result = await _configCatalog.AnalyzeMessagesAsync(messagesResult.Value, address);
            if (result.IsFailure)
            {
                Console.WriteLine($"Analysis failed: {result.Error}");
                return 1;
            }

            var configuration = result.Value;
            Console.WriteLine("Analysis completed successfully!");
            Console.WriteLine($"Configuration: {configuration.GetSummary()}");

            if (!string.IsNullOrEmpty(output))
            {
                // TODO: Implement JSON serialization of configuration
                Console.WriteLine($"Configuration saved to: {output}");
            }

            return 0;
        });

        return command;
    }

    private Command CreateListCommand()
    {
        var vendorOption = CreateNullableOption("--vendor", "Filter by vendor");
        var standardOption = CreateNullableOption("--standard", "Filter by standard");

        var command = new Command("list", "List all configurations")
        {
            vendorOption, standardOption
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var vendor = parseResult.GetValue(vendorOption);
            var standard = parseResult.GetValue(standardOption);

            Result<IReadOnlyList<VendorConfiguration>> result;

            if (!string.IsNullOrEmpty(vendor))
                result = await _configCatalog.GetByVendorAsync(vendor);
            else if (!string.IsNullOrEmpty(standard))
                result = await _configCatalog.GetByStandardAsync(standard);
            else
                result = await _configCatalog.ListAllAsync();

            if (result.IsFailure)
            {
                Console.WriteLine($"Error: {result.Error}");
                return 1;
            }

            var configurations = result.Value;
            if (configurations.Count == 0)
            {
                Console.WriteLine("No configurations found.");
                return 0;
            }

            Console.WriteLine($"Found {configurations.Count} configuration(s):");
            Console.WriteLine();

            foreach (var config in configurations)
            {
                Console.WriteLine($"  • {config.GetSummary()}");
            }

            return 0;
        });

        return command;
    }

    private Command CreateShowCommand()
    {
        var addressOption = CreateRequiredOption("--address", "Configuration address (Vendor-Standard-MessageType)");

        var command = new Command("show", "Show detailed configuration information")
        {
            addressOption
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var addressString = parseResult.GetValue(addressOption)!;
            
            if (!ConfigurationAddress.TryParse(addressString, out var address) || address == null)
            {
                Console.WriteLine($"Invalid address format: {addressString}");
                Console.WriteLine("Expected format: Vendor-Standard-MessageType");
                return 1;
            }

            var result = await _configCatalog.GetConfigurationAsync(address);

            if (result.IsFailure)
            {
                Console.WriteLine($"Error: {result.Error}");
                return 1;
            }

            if (result.Value == null)
            {
                Console.WriteLine($"Configuration not found: {address}");
                return 1;
            }

            var config = result.Value;
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

            return 0;
        });

        return command;
    }

    private Command CreateStatsCommand()
    {
        var command = new Command("stats", "Show configuration catalog statistics");

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var result = await _configCatalog.GetStatisticsAsync();

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
}