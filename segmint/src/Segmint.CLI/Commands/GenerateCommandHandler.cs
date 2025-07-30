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
/// Command handler for generating HL7 messages.
/// </summary>
public class GenerateCommandHandler
{
    private readonly ILogger<GenerateCommandHandler> _logger;
    private readonly IMessageGeneratorService _messageGeneratorService;
    private readonly IConfigurationService _configurationService;
    private readonly IOutputService _outputService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateCommandHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="messageGeneratorService">Message generator service.</param>
    /// <param name="configurationService">Configuration service.</param>
    /// <param name="outputService">Output service.</param>
    public GenerateCommandHandler(
        ILogger<GenerateCommandHandler> logger,
        IMessageGeneratorService messageGeneratorService,
        IConfigurationService configurationService,
        IOutputService outputService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageGeneratorService = messageGeneratorService ?? throw new ArgumentNullException(nameof(messageGeneratorService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
    }

    /// <summary>
    /// Handles the generate command.
    /// </summary>
    /// <param name="messageType">Message type to generate.</param>
    /// <param name="configPath">Configuration file path.</param>
    /// <param name="outputPath">Output directory or file path.</param>
    /// <param name="count">Number of messages to generate.</param>
    /// <param name="format">Output format.</param>
    /// <param name="batchMode">Enable batch processing mode.</param>
    /// <param name="templateNames">Template names to use.</param>
    /// <returns>Task representing the operation.</returns>
    public async Task HandleAsync(
        string messageType,
        string? configPath,
        string outputPath,
        int count,
        string? format,
        bool batchMode,
        string[]? templateNames)
    {
        try
        {
            _logger.LogInformation("Starting message generation: Type={MessageType}, Count={Count}, Output={OutputPath}", 
                messageType, count, outputPath);

            // Validate message type
            var availableTypes = _messageGeneratorService.GetAvailableMessageTypes();
            if (!availableTypes.Contains(messageType.ToUpperInvariant()))
            {
                await Console.Error.WriteLineAsync($"Error: Unsupported message type '{messageType}'");
                await Console.Error.WriteLineAsync($"Available types: {string.Join(", ", availableTypes)}");
                Environment.Exit(1);
                return;
            }

            // Validate output format if specified
            if (!string.IsNullOrEmpty(format))
            {
                var supportedFormats = _outputService.GetSupportedOutputFormats();
                if (!supportedFormats.Contains(format.ToLowerInvariant()))
                {
                    await Console.Error.WriteLineAsync($"Error: Unsupported output format '{format}'");
                    await Console.Error.WriteLineAsync($"Supported formats: {string.Join(", ", supportedFormats)}");
                    Environment.Exit(1);
                    return;
                }
            }

            // Validate templates if specified
            if (templateNames != null && templateNames.Length > 0)
            {
                var availableTemplates = _messageGeneratorService.GetAvailableTemplates(messageType);
                var invalidTemplates = templateNames.Except(availableTemplates).ToArray();
                if (invalidTemplates.Any())
                {
                    await Console.Error.WriteLineAsync($"Error: Invalid templates: {string.Join(", ", invalidTemplates)}");
                    await Console.Error.WriteLineAsync($"Available templates for {messageType}: {string.Join(", ", availableTemplates)}");
                    Environment.Exit(1);
                    return;
                }
            }

            // Load configuration if specified
            if (!string.IsNullOrEmpty(configPath))
            {
                if (!File.Exists(configPath))
                {
                    await Console.Error.WriteLineAsync($"Error: Configuration file not found: {configPath}");
                    Environment.Exit(1);
                    return;
                }

                // Validate configuration
                var validationResult = await _configurationService.ValidateConfigurationAsync(configPath);
                if (!validationResult.IsValid)
                {
                    await Console.Error.WriteLineAsync("Error: Invalid configuration file:");
                    foreach (var error in validationResult.Errors)
                    {
                        await Console.Error.WriteLineAsync($"  - {error}");
                    }
                    Environment.Exit(1);
                    return;
                }

                if (validationResult.Warnings.Any())
                {
                    await Console.Out.WriteLineAsync("Configuration warnings:");
                    foreach (var warning in validationResult.Warnings)
                    {
                        await Console.Out.WriteLineAsync($"  - {warning}");
                    }
                    await Console.Out.WriteLineAsync();
                }
            }

            // Generate progress indicator for large batches
            if (count > 10)
            {
                await Console.Out.WriteLineAsync($"Generating {count} {messageType} messages...");
            }

            // Generate messages
            var messages = await _messageGeneratorService.GenerateMessagesAsync(
                messageType,
                count,
                configPath,
                templateNames);

            if (!messages.Any())
            {
                await Console.Error.WriteLineAsync("Error: No messages were generated");
                Environment.Exit(1);
                return;
            }

            // Save messages
            await _outputService.SaveMessagesAsync(
                messages,
                outputPath,
                format,
                batchMode);

            // Display success message
            var actualCount = messages.Count();
            await Console.Out.WriteLineAsync($"✓ Successfully generated {actualCount} {messageType} message{(actualCount == 1 ? "" : "s")}");
            await Console.Out.WriteLineAsync($"✓ Output saved to: {outputPath}");

            if (!string.IsNullOrEmpty(format))
            {
                await Console.Out.WriteLineAsync($"✓ Format: {format.ToUpperInvariant()}");
            }

            if (batchMode)
            {
                await Console.Out.WriteLineAsync("✓ Batch mode enabled");
            }

            _logger.LogInformation("Message generation completed successfully: Generated={GeneratedCount}, Output={OutputPath}", 
                actualCount, outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during message generation");
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}