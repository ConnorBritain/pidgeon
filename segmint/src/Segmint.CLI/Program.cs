// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Segmint.CLI.Commands;
using Segmint.CLI.Services;
using Segmint.Core.Configuration;

namespace Segmint.CLI;

/// <summary>
/// Main entry point for the Segmint HL7 CLI application.
/// </summary>
public class Program
{
    /// <summary>
    /// Main application entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 for success, non-zero for failure).</returns>
    public static async Task<int> Main(string[] args)
    {
        // Create host builder for dependency injection
        var host = CreateHostBuilder(args).Build();

        // Create root command
        var rootCommand = CreateRootCommand(host.Services);

        // Parse and execute
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Creates the host builder with dependency injection configuration.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Configured host builder.</returns>
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register validation components
                services.AddSingleton<Segmint.Core.HL7.Validation.ICompositeValidator, Segmint.Core.HL7.Validation.CompositeValidator>();
                
                // Register configuration services
                services.AddSingleton<ConfigurationManager>();
                services.AddSingleton<Segmint.Core.Configuration.ConfigurationService>();
                
                // Register core services
                services.AddSingleton<IMessageGeneratorService, MessageGeneratorService>();
                services.AddSingleton<IValidationService, ValidationService>();
                services.AddSingleton<IConfigurationService, CLI.Services.ConfigurationService>();
                services.AddSingleton<IOutputService, OutputService>();

                // Register command handlers
                services.AddTransient<GenerateCommandHandler>();
                services.AddTransient<ValidateCommandHandler>();
                services.AddTransient<AnalyzeCommandHandler>();
                services.AddTransient<ConfigCommandHandler>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            });

    /// <summary>
    /// Creates the root command with all subcommands.
    /// </summary>
    /// <param name="serviceProvider">Dependency injection service provider.</param>
    /// <returns>Configured root command.</returns>
    private static RootCommand CreateRootCommand(IServiceProvider serviceProvider)
    {
        var rootCommand = new RootCommand("Segmint HL7 Generator - Professional HL7 v2.3 message generation and validation")
        {
            Name = "segmint"
        };

        // Create and add subcommands
        var generateCommand = CreateGenerateCommand(serviceProvider);
        var validateCommand = CreateValidateCommand(serviceProvider);
        var analyzeCommand = CreateAnalyzeCommand(serviceProvider);
        var configCommand = CreateConfigCommand(serviceProvider);

        rootCommand.AddCommand(generateCommand);
        rootCommand.AddCommand(validateCommand);
        rootCommand.AddCommand(analyzeCommand);
        rootCommand.AddCommand(configCommand);

        return rootCommand;
    }

    /// <summary>
    /// Creates the generate command.
    /// </summary>
    private static Command CreateGenerateCommand(IServiceProvider serviceProvider)
    {
        var typeOption = new Option<string>(
            ["--type", "-t"],
            "Message type to generate (RDE, ADT, ACK)")
            { IsRequired = true };

        var configOption = new Option<string?>(
            ["--config", "-c"],
            "Configuration file path");

        var outputOption = new Option<string>(
            ["--output", "-o"],
            "Output directory or file path")
            { IsRequired = true };

        var countOption = new Option<int>(
            ["--count", "-n"],
            () => 1,
            "Number of messages to generate");

        var formatOption = new Option<string?>(
            ["--format", "-f"],
            "Output format (hl7, json, xml)");

        var batchOption = new Option<bool>(
            ["--batch"],
            "Enable batch processing mode");

        var templateOption = new Option<string[]>(
            ["--template", "--templates"],
            "Template names to use");

        var command = new Command("generate", "Generate HL7 messages")
        {
            typeOption,
            configOption,
            outputOption,
            countOption,
            formatOption,
            batchOption,
            templateOption
        };

        var handler = serviceProvider.GetRequiredService<GenerateCommandHandler>();
        command.SetHandler(handler.HandleAsync,
            typeOption,
            configOption,
            outputOption,
            countOption,
            formatOption,
            batchOption,
            templateOption);

        return command;
    }

    /// <summary>
    /// Creates the validate command.
    /// </summary>
    private static Command CreateValidateCommand(IServiceProvider serviceProvider)
    {
        var inputArgument = new Argument<string>(
            "input",
            "Input file or directory path");

        var levelsOption = new Option<string[]>(
            ["--levels", "-l"],
            () => new[] { "syntax", "semantic" },
            "Validation levels (syntax, semantic, clinical)");

        var configOption = new Option<string?>(
            ["--config", "-c"],
            "Configuration file for validation rules");

        var reportOption = new Option<string?>(
            ["--report", "-r"],
            "Generate validation report file");

        var strictOption = new Option<bool>(
            ["--strict"],
            "Enable strict validation mode");

        var summaryOption = new Option<bool>(
            ["--summary"],
            "Show validation summary only");

        var command = new Command("validate", "Validate HL7 messages")
        {
            inputArgument,
            levelsOption,
            configOption,
            reportOption,
            strictOption,
            summaryOption
        };

        var handler = serviceProvider.GetRequiredService<ValidateCommandHandler>();
        command.SetHandler(handler.HandleAsync,
            inputArgument,
            levelsOption,
            configOption,
            reportOption,
            strictOption,
            summaryOption);

        return command;
    }

    /// <summary>
    /// Creates the analyze command.
    /// </summary>
    private static Command CreateAnalyzeCommand(IServiceProvider serviceProvider)
    {
        var inputArgument = new Argument<string>(
            "input",
            "Input file or directory containing HL7 messages");

        var outputOption = new Option<string>(
            ["--output", "-o"],
            "Output configuration file path")
            { IsRequired = true };

        var nameOption = new Option<string?>(
            ["--name"],
            "Configuration name");

        var sampleSizeOption = new Option<int>(
            ["--sample-size"],
            () => 100,
            "Maximum number of messages to analyze");

        var includeStatsOption = new Option<bool>(
            ["--include-stats"],
            "Include statistical analysis in output");

        var segmentsOption = new Option<string[]>(
            ["--segments"],
            "Specific segments to analyze");

        var command = new Command("analyze", "Analyze HL7 messages and infer configurations")
        {
            inputArgument,
            outputOption,
            nameOption,
            sampleSizeOption,
            includeStatsOption,
            segmentsOption
        };

        var handler = serviceProvider.GetRequiredService<AnalyzeCommandHandler>();
        command.SetHandler(handler.HandleAsync,
            inputArgument,
            outputOption,
            nameOption,
            sampleSizeOption,
            includeStatsOption,
            segmentsOption);

        return command;
    }

    /// <summary>
    /// Creates the config command.
    /// </summary>
    private static Command CreateConfigCommand(IServiceProvider serviceProvider)
    {
        var configService = serviceProvider.GetRequiredService<Segmint.Core.Configuration.ConfigurationService>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        
        return ConfigCommands.CreateConfigCommand(configService, logger);
    }
}