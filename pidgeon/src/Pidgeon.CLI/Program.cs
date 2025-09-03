// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pidgeon.CLI.Commands;
using Pidgeon.CLI.Extensions;
using Pidgeon.CLI.Output;
using Pidgeon.Core.Extensions;
using System.CommandLine;
using System.Reflection;

namespace Pidgeon.CLI;

/// <summary>
/// Entry point for the Pidgeon CLI application.
/// Sets up dependency injection, logging, and command line parsing.
/// </summary>
internal class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code</returns>
    public static async Task<int> Main(string[] args)
    {
        // Create the host builder with dependency injection
        var host = CreateHostBuilder(args).Build();
        
        try
        {
            // Create the root command with all subcommands
            var rootCommand = CreateRootCommand(host.Services);
            
            // Parse and invoke the command using beta5 API
            return await rootCommand.Parse(args).InvokeAsync();
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An unhandled exception occurred");
            return 1;
        }
    }

    /// <summary>
    /// Creates and configures the host builder with all necessary services.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Configured host builder</returns>
    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                // Add Pidgeon Core services
                services.AddPidgeonCore();
                
                // Add CLI commands using convention-based registration
                services.AddCliCommands();
                
                // Add console services
                services.AddSingleton<IConsoleOutput, ConsoleOutput>();
            });
    }

    /// <summary>
    /// Creates the root command with all subcommands configured.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    /// <returns>Configured root command</returns>
    private static RootCommand CreateRootCommand(IServiceProvider serviceProvider)
    {
        var rootCommand = new RootCommand("Pidgeon Healthcare Interoperability Platform")
        {
            Description = "AI-augmented universal healthcare standards platform supporting HL7, FHIR, and NCPDP"
        };

        // Add global options
        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Enable verbose output"
        };
        rootCommand.Add(verboseOption);

        // Add subcommands using convention-based discovery
        var assembly = typeof(Program).Assembly;
        var commandTypes = assembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          type.IsSubclassOf(typeof(CommandBuilderBase)))
            .ToList();
        
        foreach (var commandType in commandTypes)
        {
            var command = (CommandBuilderBase)serviceProvider.GetRequiredService(commandType);
            rootCommand.Add(command.CreateCommand());
        }

        return rootCommand;
    }
}