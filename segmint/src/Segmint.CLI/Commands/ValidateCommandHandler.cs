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
/// Command handler for validating HL7 messages.
/// </summary>
public class ValidateCommandHandler
{
    private readonly ILogger<ValidateCommandHandler> _logger;
    private readonly IValidationService _validationService;
    private readonly IConfigurationService _configurationService;
    private readonly IOutputService _outputService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateCommandHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="validationService">Validation service.</param>
    /// <param name="configurationService">Configuration service.</param>
    /// <param name="outputService">Output service.</param>
    public ValidateCommandHandler(
        ILogger<ValidateCommandHandler> logger,
        IValidationService validationService,
        IConfigurationService configurationService,
        IOutputService outputService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
    }

    /// <summary>
    /// Handles the validate command.
    /// </summary>
    /// <param name="inputPath">Input file or directory path.</param>
    /// <param name="validationLevels">Validation levels to perform.</param>
    /// <param name="configPath">Configuration file for validation rules.</param>
    /// <param name="reportPath">Generate validation report file.</param>
    /// <param name="strictMode">Enable strict validation mode.</param>
    /// <param name="summaryOnly">Show validation summary only.</param>
    /// <returns>Task representing the operation.</returns>
    public async Task HandleAsync(
        string inputPath,
        string[] validationLevels,
        string? configPath,
        string? reportPath,
        bool strictMode,
        bool summaryOnly)
    {
        try
        {
            _logger.LogInformation("Starting validation: Input={InputPath}, Levels={ValidationLevels}, Strict={StrictMode}", 
                inputPath, string.Join(",", validationLevels), strictMode);

            // Validate input path exists
            if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
            {
                await Console.Error.WriteLineAsync($"Error: Input path not found: {inputPath}");
                Environment.Exit(1);
                return;
            }

            // Validate validation levels
            var availableLevels = _validationService.GetAvailableValidationLevels();
            var invalidLevels = validationLevels.Except(availableLevels, StringComparer.OrdinalIgnoreCase).ToArray();
            if (invalidLevels.Any())
            {
                await Console.Error.WriteLineAsync($"Error: Invalid validation levels: {string.Join(", ", invalidLevels)}");
                await Console.Error.WriteLineAsync($"Available levels: {string.Join(", ", availableLevels)}");
                Environment.Exit(1);
                return;
            }

            // Validate configuration file if specified
            if (!string.IsNullOrEmpty(configPath))
            {
                if (!File.Exists(configPath))
                {
                    await Console.Error.WriteLineAsync($"Error: Configuration file not found: {configPath}");
                    Environment.Exit(1);
                    return;
                }

                var configValidation = await _configurationService.ValidateConfigurationAsync(configPath, strictMode);
                if (!configValidation.IsValid)
                {
                    await Console.Error.WriteLineAsync("Error: Invalid configuration file:");
                    foreach (var error in configValidation.Errors)
                    {
                        await Console.Error.WriteLineAsync($"  - {error}");
                    }
                    Environment.Exit(1);
                    return;
                }

                if (configValidation.Warnings.Any())
                {
                    await Console.Out.WriteLineAsync("Configuration warnings:");
                    foreach (var warning in configValidation.Warnings)
                    {
                        await Console.Out.WriteLineAsync($"  - {warning}");
                    }
                    await Console.Out.WriteLineAsync();
                }
            }

            // Validate report format if specified
            if (!string.IsNullOrEmpty(reportPath))
            {
                var reportFormat = Path.GetExtension(reportPath).TrimStart('.').ToLowerInvariant();
                if (string.IsNullOrEmpty(reportFormat))
                {
                    reportFormat = "text"; // Default format
                }

                var supportedFormats = _outputService.GetSupportedReportFormats();
                if (!supportedFormats.Contains(reportFormat))
                {
                    await Console.Error.WriteLineAsync($"Error: Unsupported report format '{reportFormat}'");
                    await Console.Error.WriteLineAsync($"Supported formats: {string.Join(", ", supportedFormats)}");
                    Environment.Exit(1);
                    return;
                }
            }

            // Display validation start message
            await Console.Out.WriteLineAsync($"Validating HL7 messages from: {inputPath}");
            await Console.Out.WriteLineAsync($"Validation levels: {string.Join(", ", validationLevels)}");
            if (strictMode)
            {
                await Console.Out.WriteLineAsync("Strict mode: Enabled");
            }
            await Console.Out.WriteLineAsync();

            // Perform validation
            var validationSummary = await _validationService.ValidateAsync(
                inputPath,
                validationLevels,
                configPath,
                strictMode);

            // Display results to console
            await _outputService.DisplayValidationSummaryAsync(validationSummary, summaryOnly);

            // Save report if requested
            if (!string.IsNullOrEmpty(reportPath))
            {
                var reportFormat = Path.GetExtension(reportPath).TrimStart('.').ToLowerInvariant();
                if (string.IsNullOrEmpty(reportFormat))
                {
                    reportFormat = "text";
                }

                await _outputService.SaveValidationReportAsync(validationSummary, reportPath, reportFormat);
                await Console.Out.WriteLineAsync($"✓ Validation report saved to: {reportPath}");
            }

            // Set exit code based on validation results
            if (validationSummary.InvalidMessages > 0)
            {
                _logger.LogWarning("Validation completed with {InvalidMessages} invalid messages", validationSummary.InvalidMessages);
                
                if (strictMode)
                {
                    await Console.Error.WriteLineAsync("Validation failed in strict mode");
                    Environment.Exit(1);
                }
                else
                {
                    await Console.Out.WriteLineAsync("⚠️  Validation completed with errors (see details above)");
                }
            }
            else
            {
                await Console.Out.WriteLineAsync("✓ All messages validated successfully");
                _logger.LogInformation("All {TotalMessages} messages validated successfully", validationSummary.TotalMessages);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during validation");
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}