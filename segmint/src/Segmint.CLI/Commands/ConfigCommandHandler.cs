// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Segmint.CLI.Services;

namespace Segmint.CLI.Commands;

/// <summary>
/// Command handler for configuration management operations.
/// </summary>
public class ConfigCommandHandler
{
    private readonly ILogger<ConfigCommandHandler> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly IOutputService _outputService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigCommandHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configurationService">Configuration service.</param>
    /// <param name="outputService">Output service.</param>
    public ConfigCommandHandler(
        ILogger<ConfigCommandHandler> logger,
        IConfigurationService configurationService,
        IOutputService outputService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
    }

    /// <summary>
    /// Handles the config validate subcommand.
    /// </summary>
    /// <param name="configPath">Configuration file path.</param>
    /// <param name="strictMode">Enable strict validation.</param>
    /// <returns>Task representing the operation.</returns>
    public async Task HandleValidateAsync(string configPath, bool strictMode)
    {
        try
        {
            _logger.LogInformation("Validating configuration: {ConfigPath}, Strict={StrictMode}", configPath, strictMode);

            // Validate file exists
            if (!File.Exists(configPath))
            {
                await Console.Error.WriteLineAsync($"Error: Configuration file not found: {configPath}");
                Environment.Exit(1);
                return;
            }

            await Console.Out.WriteLineAsync($"Validating configuration: {configPath}");
            if (strictMode)
            {
                await Console.Out.WriteLineAsync("Strict mode: Enabled");
            }
            await Console.Out.WriteLineAsync();

            // Perform validation
            var result = await _configurationService.ValidateConfigurationAsync(configPath, strictMode);

            // Display results
            await Console.Out.WriteLineAsync("═══════════════════════════════════════════════════════════════");
            await Console.Out.WriteLineAsync("                CONFIGURATION VALIDATION RESULT");
            await Console.Out.WriteLineAsync("═══════════════════════════════════════════════════════════════");
            await Console.Out.WriteLineAsync();

            var status = result.IsValid ? "✓ VALID" : "✗ INVALID";
            var statusColor = result.IsValid ? ConsoleColor.Green : ConsoleColor.Red;
            
            Console.ForegroundColor = statusColor;
            await Console.Out.WriteLineAsync($"Status: {status}");
            Console.ResetColor();
            await Console.Out.WriteLineAsync();

            // Display errors
            if (result.Errors.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                await Console.Out.WriteLineAsync($"ERRORS ({result.Errors.Count}):");
                Console.ResetColor();
                foreach (var error in result.Errors)
                {
                    await Console.Out.WriteLineAsync($"  ✗ {error}");
                }
                await Console.Out.WriteLineAsync();
            }

            // Display warnings
            if (result.Warnings.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                await Console.Out.WriteLineAsync($"WARNINGS ({result.Warnings.Count}):");
                Console.ResetColor();
                foreach (var warning in result.Warnings)
                {
                    await Console.Out.WriteLineAsync($"  ⚠️  {warning}");
                }
                await Console.Out.WriteLineAsync();
            }

            // Display information
            if (result.Information.Any())
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                await Console.Out.WriteLineAsync($"INFORMATION ({result.Information.Count}):");
                Console.ResetColor();
                foreach (var info in result.Information)
                {
                    await Console.Out.WriteLineAsync($"  ℹ️  {info}");
                }
                await Console.Out.WriteLineAsync();
            }

            // Set exit code
            if (!result.IsValid)
            {
                _logger.LogWarning("Configuration validation failed: {ConfigPath}", configPath);
                Environment.Exit(1);
            }
            else
            {
                await Console.Out.WriteLineAsync("✓ Configuration is valid and ready for use");
                _logger.LogInformation("Configuration validation passed: {ConfigPath}", configPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration");
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Handles the config compare subcommand.
    /// </summary>
    /// <param name="config1Path">First configuration file.</param>
    /// <param name="config2Path">Second configuration file.</param>
    /// <param name="outputPath">Output diff file.</param>
    /// <param name="summaryOnly">Show summary only.</param>
    /// <returns>Task representing the operation.</returns>
    public async Task HandleCompareAsync(
        string config1Path,
        string config2Path,
        string? outputPath,
        bool summaryOnly)
    {
        try
        {
            _logger.LogInformation("Comparing configurations: {Config1Path} vs {Config2Path}", config1Path, config2Path);

            // Validate files exist
            if (!File.Exists(config1Path))
            {
                await Console.Error.WriteLineAsync($"Error: Configuration file not found: {config1Path}");
                Environment.Exit(1);
                return;
            }

            if (!File.Exists(config2Path))
            {
                await Console.Error.WriteLineAsync($"Error: Configuration file not found: {config2Path}");
                Environment.Exit(1);
                return;
            }

            await Console.Out.WriteLineAsync($"Comparing configurations:");
            await Console.Out.WriteLineAsync($"  File 1: {config1Path}");
            await Console.Out.WriteLineAsync($"  File 2: {config2Path}");
            await Console.Out.WriteLineAsync();

            // Perform comparison
            var result = await _configurationService.CompareConfigurationsAsync(config1Path, config2Path);

            // Display results to console
            await _outputService.DisplayComparisonResultAsync(result, summaryOnly);

            // Save detailed report if requested
            if (!string.IsNullOrEmpty(outputPath))
            {
                var reportFormat = Path.GetExtension(outputPath).TrimStart('.').ToLowerInvariant();
                if (string.IsNullOrEmpty(reportFormat))
                {
                    reportFormat = "text";
                }

                var supportedFormats = _outputService.GetSupportedReportFormats();
                if (!supportedFormats.Contains(reportFormat))
                {
                    await Console.Error.WriteLineAsync($"Error: Unsupported report format '{reportFormat}'");
                    await Console.Error.WriteLineAsync($"Supported formats: {string.Join(", ", supportedFormats)}");
                    Environment.Exit(1);
                    return;
                }

                // Ensure output directory exists
                var outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                await _outputService.SaveComparisonReportAsync(result, outputPath, reportFormat);
                await Console.Out.WriteLineAsync($"✓ Detailed comparison report saved to: {outputPath}");
            }

            // Set exit code based on whether configurations are identical
            if (!result.AreIdentical)
            {
                _logger.LogInformation("Configurations are different: {DifferenceCount} differences found", result.Differences.Count);
                Environment.Exit(1); // Exit with error code for differences found
            }
            else
            {
                _logger.LogInformation("Configurations are identical");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing configurations");
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Handles the config list subcommand.
    /// </summary>
    /// <param name="detailed">Show detailed information.</param>
    /// <returns>Task representing the operation.</returns>
    public async Task HandleListAsync(bool detailed)
    {
        try
        {
            _logger.LogInformation("Listing configuration templates");

            var templates = _configurationService.GetAvailableTemplates();

            if (!templates.Any())
            {
                await Console.Out.WriteLineAsync("No configuration templates available.");
                return;
            }

            await _outputService.DisplayTemplatesAsync(templates, detailed);

            await Console.Out.WriteLineAsync($"Found {templates.Count()} available template{(templates.Count() == 1 ? "" : "s")}");
            await Console.Out.WriteLineAsync();
            await Console.Out.WriteLineAsync("To use a template:");
            await Console.Out.WriteLineAsync("  segmint generate --template <template-name> --type <message-type> --output <output-path>");

            _logger.LogInformation("Listed {TemplateCount} configuration templates", templates.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing configuration templates");
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}