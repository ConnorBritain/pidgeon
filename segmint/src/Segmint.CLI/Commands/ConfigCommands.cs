// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Segmint.Core.Configuration;

namespace Segmint.CLI.Commands;

/// <summary>
/// Configuration management commands for the CLI.
/// </summary>
public static class ConfigCommands
{
    /// <summary>
    /// Creates the main config command with all subcommands.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">Logger instance.</param>
    /// <returns>The configured config command.</returns>
    public static Command CreateConfigCommand(
        ConfigurationService configurationService,
        ILogger logger)
    {
        var configCommand = new Command("config", "Manage Segmint configuration");

        // Add subcommands
        configCommand.AddCommand(CreateInitCommand(configurationService, logger));
        configCommand.AddCommand(CreateShowCommand(configurationService, logger));
        configCommand.AddCommand(CreateValidateCommand(configurationService, logger));
        configCommand.AddCommand(CreateTemplateCommand(configurationService, logger));
        configCommand.AddCommand(CreateCompareCommand(configurationService, logger));
        configCommand.AddCommand(CreateUpdateCommand(configurationService, logger));

        return configCommand;
    }

    /// <summary>
    /// Creates the config init command for initializing configuration files.
    /// </summary>
    private static Command CreateInitCommand(
        ConfigurationService configurationService,
        ILogger logger)
    {
        var initCommand = new Command("init", "Initialize a new configuration file");

        var pathOption = new Option<FileInfo?>(
            aliases: ["--path", "-p"],
            description: "Path for the configuration file")
        {
            ArgumentHelpName = "FILE"
        };

        var templateOption = new Option<string?>(
            aliases: ["--template", "-t"],
            description: "Template to use (pharmacy, patient-management, development, production, validation)")
        {
            ArgumentHelpName = "TEMPLATE"
        };

        var forceOption = new Option<bool>(
            aliases: ["--force", "-f"],
            description: "Overwrite existing configuration file");

        initCommand.AddOption(pathOption);
        initCommand.AddOption(templateOption);
        initCommand.AddOption(forceOption);

        initCommand.SetHandler(async (FileInfo? path, string? template, bool force) =>
        {
            await HandleInitCommand(configurationService, logger, path, template, force, CancellationToken.None);
        }, pathOption, templateOption, forceOption);

        return initCommand;
    }

    /// <summary>
    /// Creates the config show command for displaying configuration information.
    /// </summary>
    private static Command CreateShowCommand(
        ConfigurationService configurationService,
        ILogger logger)
    {
        var showCommand = new Command("show", "Display configuration information");

        var pathOption = new Option<FileInfo?>(
            aliases: ["--path", "-p"],
            description: "Path to configuration file (uses current if not specified)")
        {
            ArgumentHelpName = "FILE"
        };

        var sectionOption = new Option<string?>(
            aliases: ["--section", "-s"],
            description: "Specific section to display (hl7, validation, interface, logging, performance)")
        {
            ArgumentHelpName = "SECTION"
        };

        showCommand.AddOption(pathOption);
        showCommand.AddOption(sectionOption);

        showCommand.SetHandler(async (FileInfo? path, string? section) =>
        {
            await HandleShowCommand(configurationService, logger, path, section, CancellationToken.None);
        }, pathOption, sectionOption);

        return showCommand;
    }

    /// <summary>
    /// Creates the config validate command for validating configuration files.
    /// </summary>
    private static Command CreateValidateCommand(
        ConfigurationService configurationService,
        ILogger logger)
    {
        var validateCommand = new Command("validate", "Validate a configuration file");

        var pathOption = new Option<FileInfo?>(
            aliases: ["--path", "-p"],
            description: "Path to configuration file (uses current if not specified)")
        {
            ArgumentHelpName = "FILE"
        };

        validateCommand.AddOption(pathOption);

        validateCommand.SetHandler(async (FileInfo? path) =>
        {
            await HandleValidateCommand(configurationService, logger, path, CancellationToken.None);
        }, pathOption);

        return validateCommand;
    }

    /// <summary>
    /// Creates the config template command for managing templates.
    /// </summary>
    private static Command CreateTemplateCommand(
        ConfigurationService configurationService,
        ILogger logger)
    {
        var templateCommand = new Command("template", "Manage configuration templates");

        // List subcommand
        var listCommand = new Command("list", "List available templates");
        listCommand.SetHandler(() => HandleListTemplatesCommand(configurationService, logger));
        templateCommand.AddCommand(listCommand);

        // Create subcommand
        var createCommand = new Command("create", "Create configuration from template");
        
        var templateArg = new Argument<string>(
            name: "template",
            description: "Template name to use");

        var pathOption = new Option<FileInfo>(
            aliases: ["--output", "-o"],
            description: "Output path for configuration file")
        {
            ArgumentHelpName = "FILE"
        };

        createCommand.AddArgument(templateArg);
        createCommand.AddOption(pathOption);

        createCommand.SetHandler(async (string template, FileInfo path) =>
        {
            await HandleCreateTemplateCommand(configurationService, logger, template, path, CancellationToken.None);
        }, templateArg, pathOption);

        templateCommand.AddCommand(createCommand);

        return templateCommand;
    }

    /// <summary>
    /// Creates the config compare command for comparing configuration files.
    /// </summary>
    private static Command CreateCompareCommand(
        ConfigurationService configurationService,
        ILogger logger)
    {
        var compareCommand = new Command("compare", "Compare two configuration files");

        var path1Arg = new Argument<FileInfo>(
            name: "file1",
            description: "First configuration file");

        var path2Arg = new Argument<FileInfo>(
            name: "file2", 
            description: "Second configuration file");

        compareCommand.AddArgument(path1Arg);
        compareCommand.AddArgument(path2Arg);

        compareCommand.SetHandler(async (FileInfo path1, FileInfo path2) =>
        {
            await HandleCompareCommand(configurationService, logger, path1, path2, CancellationToken.None);
        }, path1Arg, path2Arg);

        return compareCommand;
    }

    /// <summary>
    /// Creates the config update command for updating configuration sections.
    /// </summary>
    private static Command CreateUpdateCommand(
        ConfigurationService configurationService,
        ILogger logger)
    {
        var updateCommand = new Command("update", "Update configuration settings");

        var pathOption = new Option<FileInfo?>(
            aliases: ["--path", "-p"],
            description: "Path to configuration file")
        {
            ArgumentHelpName = "FILE"
        };

        var sectionOption = new Option<string>(
            aliases: ["--section", "-s"],
            description: "Configuration section to update")
        {
            ArgumentHelpName = "SECTION"
        };

        var keyOption = new Option<string>(
            aliases: ["--key", "-k"],
            description: "Configuration key to update")
        {
            ArgumentHelpName = "KEY"
        };

        var valueOption = new Option<string>(
            aliases: ["--value", "-v"],
            description: "New value for the configuration key")
        {
            ArgumentHelpName = "VALUE"
        };

        updateCommand.AddOption(pathOption);
        updateCommand.AddOption(sectionOption);
        updateCommand.AddOption(keyOption);
        updateCommand.AddOption(valueOption);

        updateCommand.SetHandler(async (FileInfo? path, string section, string key, string value) =>
        {
            await HandleUpdateCommand(configurationService, logger, path, section, key, value, CancellationToken.None);
        }, pathOption, sectionOption, keyOption, valueOption);

        return updateCommand;
    }

    // Command handlers

    private static async Task HandleInitCommand(
        ConfigurationService configurationService,
        ILogger logger,
        FileInfo? path,
        string? template,
        bool force,
        CancellationToken cancellationToken)
    {
        try
        {
            var configPath = path?.FullName ?? "segmint-config.json";

            if (File.Exists(configPath) && !force)
            {
                logger.LogError("Configuration file already exists at {ConfigPath}. Use --force to overwrite.", configPath);
                return;
            }

            bool success;
            if (!string.IsNullOrEmpty(template))
            {
                success = await configurationService.CreateFromTemplateAsync(template, configPath, true, cancellationToken);
                if (success)
                {
                    logger.LogInformation("Created {Template} configuration at {ConfigPath}", template, configPath);
                }
            }
            else
            {
                success = await configurationService.LoadConfigurationAsync(configPath, true, cancellationToken);
                if (success)
                {
                    logger.LogInformation("Created default configuration at {ConfigPath}", configPath);
                }
            }

            if (!success)
            {
                logger.LogError("Failed to create configuration file");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing configuration");
        }
    }

    private static async Task HandleShowCommand(
        ConfigurationService configurationService,
        ILogger logger,
        FileInfo? path,
        string? section,
        CancellationToken cancellationToken)
    {
        try
        {
            if (path != null)
            {
                await configurationService.LoadConfigurationAsync(path.FullName, false, cancellationToken);
            }

            var summary = configurationService.GetConfigurationSummary();
            
            if (!string.IsNullOrEmpty(section))
            {
                // Filter to specific section - simplified for now
                var lines = summary.Split('\n');
                var filteredLines = new List<string>();
                bool inSection = false;
                
                foreach (var line in lines)
                {
                    if (line.ToLowerInvariant().Contains(section.ToLowerInvariant()))
                    {
                        inSection = true;
                    }
                    
                    if (inSection)
                    {
                        filteredLines.Add(line);
                        if (string.IsNullOrEmpty(line.Trim()) && filteredLines.Count > 1)
                        {
                            break;
                        }
                    }
                }
                
                Console.WriteLine(string.Join('\n', filteredLines));
            }
            else
            {
                Console.WriteLine(summary);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error displaying configuration");
        }
    }

    private static async Task HandleValidateCommand(
        ConfigurationService configurationService,
        ILogger logger,
        FileInfo? path,
        CancellationToken cancellationToken)
    {
        try
        {
            List<string> issues;
            
            if (path != null)
            {
                issues = await configurationService.ValidateConfigurationFileAsync(path.FullName, cancellationToken);
                logger.LogInformation("Validating configuration file: {ConfigPath}", path.FullName);
            }
            else
            {
                issues = configurationService.ValidateCurrentConfiguration();
                logger.LogInformation("Validating current configuration");
            }

            if (issues.Count == 0)
            {
                logger.LogInformation("✅ Configuration is valid");
            }
            else
            {
                logger.LogWarning("❌ Found {IssueCount} validation issues:", issues.Count);
                foreach (var issue in issues)
                {
                    logger.LogWarning("  • {Issue}", issue);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating configuration");
        }
    }

    private static void HandleListTemplatesCommand(
        ConfigurationService configurationService,
        ILogger logger)
    {
        try
        {
            var templates = configurationService.GetAvailableTemplates();
            
            logger.LogInformation("Available configuration templates:");
            foreach (var template in templates)
            {
                logger.LogInformation("  • {Template}", template);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing templates");
        }
    }

    private static async Task HandleCreateTemplateCommand(
        ConfigurationService configurationService,
        ILogger logger,
        string template,
        FileInfo path,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await configurationService.CreateFromTemplateAsync(template, path.FullName, false, cancellationToken);
            
            if (success)
            {
                logger.LogInformation("Created {Template} configuration at {ConfigPath}", template, path.FullName);
            }
            else
            {
                logger.LogError("Failed to create configuration from template");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating configuration from template");
        }
    }

    private static async Task HandleCompareCommand(
        ConfigurationService configurationService,
        ILogger logger,
        FileInfo path1,
        FileInfo path2,
        CancellationToken cancellationToken)
    {
        try
        {
            var differences = await configurationService.CompareConfigurationFilesAsync(
                path1.FullName, 
                path2.FullName, 
                cancellationToken);

            if (differences.Count == 0)
            {
                logger.LogInformation("✅ Configuration files are identical");
            }
            else
            {
                logger.LogInformation("Found {DifferenceCount} differences between configuration files:", differences.Count);
                foreach (var difference in differences)
                {
                    logger.LogInformation("  • {Difference}", difference);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error comparing configuration files");
        }
    }

    private static async Task HandleUpdateCommand(
        ConfigurationService configurationService,
        ILogger logger,
        FileInfo? path,
        string section,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        try
        {
            if (path != null)
            {
                await configurationService.LoadConfigurationAsync(path.FullName, false, cancellationToken);
            }

            var updates = new Dictionary<string, object> { [key] = value };
            var success = configurationService.UpdateConfigurationSection(section, updates);

            if (success)
            {
                if (path != null)
                {
                    await configurationService.SaveConfigurationAsync(path.FullName, cancellationToken);
                }
                
                logger.LogInformation("Updated {Section}.{Key} = {Value}", section, key, value);
            }
            else
            {
                logger.LogError("Failed to update configuration");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating configuration");
        }
    }
}