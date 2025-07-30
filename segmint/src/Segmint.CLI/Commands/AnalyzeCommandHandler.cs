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
/// Command handler for analyzing HL7 messages and inferring configurations.
/// </summary>
public class AnalyzeCommandHandler
{
    private readonly ILogger<AnalyzeCommandHandler> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly IValidationService _validationService;
    private readonly IOutputService _outputService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeCommandHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configurationService">Configuration service.</param>
    /// <param name="validationService">Validation service.</param>
    /// <param name="outputService">Output service.</param>
    public AnalyzeCommandHandler(
        ILogger<AnalyzeCommandHandler> logger,
        IConfigurationService configurationService,
        IValidationService validationService,
        IOutputService outputService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
    }

    /// <summary>
    /// Handles the analyze command.
    /// </summary>
    /// <param name="inputPath">Input file or directory containing HL7 messages.</param>
    /// <param name="outputPath">Output configuration file path.</param>
    /// <param name="configurationName">Configuration name.</param>
    /// <param name="sampleSize">Maximum number of messages to analyze.</param>
    /// <param name="includeStats">Include statistical analysis in output.</param>
    /// <param name="segments">Specific segments to analyze.</param>
    /// <returns>Task representing the operation.</returns>
    public async Task HandleAsync(
        string inputPath,
        string outputPath,
        string? configurationName,
        int sampleSize,
        bool includeStats,
        string[]? segments)
    {
        try
        {
            _logger.LogInformation("Starting analysis: Input={InputPath}, Output={OutputPath}, SampleSize={SampleSize}", 
                inputPath, outputPath, sampleSize);

            // Validate input path exists
            if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
            {
                await Console.Error.WriteLineAsync($"Error: Input path not found: {inputPath}");
                Environment.Exit(1);
                return;
            }

            // Validate sample size
            if (sampleSize <= 0)
            {
                await Console.Error.WriteLineAsync("Error: Sample size must be greater than 0");
                Environment.Exit(1);
                return;
            }

            // Ensure output directory exists
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Check if output file already exists
            if (File.Exists(outputPath))
            {
                await Console.Out.WriteAsync($"Output file '{outputPath}' already exists. Overwrite? (y/N): ");
                var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (response != "y" && response != "yes")
                {
                    await Console.Out.WriteLineAsync("Analysis cancelled.");
                    return;
                }
            }

            // Display analysis start message
            await Console.Out.WriteLineAsync($"Analyzing HL7 messages from: {inputPath}");
            await Console.Out.WriteLineAsync($"Sample size: {sampleSize} messages");
            if (segments != null && segments.Length > 0)
            {
                await Console.Out.WriteLineAsync($"Analyzing segments: {string.Join(", ", segments)}");
            }
            if (includeStats)
            {
                await Console.Out.WriteLineAsync("Statistical analysis: Enabled");
            }
            await Console.Out.WriteLineAsync();

            // Perform pre-validation to ensure messages are valid
            await Console.Out.WriteLineAsync("Step 1: Pre-validating messages...");
            var validationSummary = await _validationService.ValidateAsync(
                inputPath,
                new[] { "syntax" }, // Basic syntax validation only
                strictMode: false);

            if (validationSummary.InvalidMessages > 0)
            {
                var invalidPercentage = (double)validationSummary.InvalidMessages / validationSummary.TotalMessages * 100;
                await Console.Out.WriteLineAsync($"⚠️  Warning: {validationSummary.InvalidMessages} of {validationSummary.TotalMessages} messages ({invalidPercentage:F1}%) have syntax errors");
                
                if (invalidPercentage > 50)
                {
                    await Console.Error.WriteLineAsync("Error: Too many invalid messages for reliable analysis");
                    Environment.Exit(1);
                    return;
                }
                
                await Console.Out.WriteLineAsync("Continuing with valid messages only...");
            }
            else
            {
                await Console.Out.WriteLineAsync($"✓ All {validationSummary.TotalMessages} messages passed syntax validation");
            }

            await Console.Out.WriteLineAsync();

            // Perform analysis and configuration inference
            await Console.Out.WriteLineAsync("Step 2: Analyzing message structure and inferring configuration...");
            
            var configuration = await _configurationService.AnalyzeAndInferConfigurationAsync(
                inputPath,
                configurationName,
                sampleSize,
                includeStats,
                segments);

            // Save configuration
            await Console.Out.WriteLineAsync("Step 3: Saving inferred configuration...");
            
            await _configurationService.SaveConfigurationAsync(configuration, outputPath);

            // Validate the generated configuration
            await Console.Out.WriteLineAsync("Step 4: Validating generated configuration...");
            
            var configValidation = await _configurationService.ValidateConfigurationAsync(outputPath, false);
            
            if (!configValidation.IsValid)
            {
                await Console.Error.WriteLineAsync("Error: Generated configuration is invalid:");
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

            // Display success message with details
            await Console.Out.WriteLineAsync("═══════════════════════════════════════════════════════════════");
            await Console.Out.WriteLineAsync("                    ANALYSIS COMPLETE");
            await Console.Out.WriteLineAsync("═══════════════════════════════════════════════════════════════");
            await Console.Out.WriteLineAsync();
            
            await Console.Out.WriteLineAsync($"✓ Configuration inferred successfully");
            await Console.Out.WriteLineAsync($"✓ Configuration name: {configuration.Interface.Name}");
            await Console.Out.WriteLineAsync($"✓ Messages analyzed: {Math.Min(sampleSize, validationSummary.ValidMessages)}");
            await Console.Out.WriteLineAsync($"✓ Configuration saved to: {outputPath}");
            
            if (configuration.Interface.SupportedMessageTypes?.Count > 0)
            {
                await Console.Out.WriteLineAsync($"✓ Message types detected: {configuration.Interface.SupportedMessageTypes.Count}");
            }
            
            if (configuration.HL7.CustomSegments?.Count > 0)
            {
                await Console.Out.WriteLineAsync($"✓ Segments identified: {configuration.HL7.CustomSegments.Count}");
            }

            await Console.Out.WriteLineAsync();
            await Console.Out.WriteLineAsync("Next steps:");
            await Console.Out.WriteLineAsync($"  1. Review the generated configuration: {outputPath}");
            await Console.Out.WriteLineAsync($"  2. Test message generation: segmint generate --config \"{outputPath}\" --type <message-type> --output test.hl7");
            await Console.Out.WriteLineAsync($"  3. Validate against original messages: segmint validate \"{inputPath}\" --config \"{outputPath}\"");

            _logger.LogInformation("Analysis completed successfully: Configuration={ConfigurationName}, Output={OutputPath}", 
                configuration.Interface.Name, outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during analysis");
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}