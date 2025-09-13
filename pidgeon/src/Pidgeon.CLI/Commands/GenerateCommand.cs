// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core;
using Pidgeon.Core.Application.Services.Generation;
using Pidgeon.Core.Application.Interfaces.Generation;
using Pidgeon.Core.Generation;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Command for generating healthcare messages and synthetic test data.
/// </summary>
public class GenerateCommand : CommandBuilderBase
{
    private readonly IMessageGenerationService _messageGenerationService;

    public GenerateCommand(
        ILogger<GenerateCommand> logger,
        IMessageGenerationService messageGenerationService) 
        : base(logger)
    {
        _messageGenerationService = messageGenerationService;
    }

    public override Command CreateCommand()
    {
        var command = new Command("generate", "Generate synthetic messages/resources with smart standard inference");

        // Positional arguments for smart inference: pidgeon generate <message-type> or pidgeon generate <standard> <message-type>
        var messageTypeArg = new Argument<string[]>("args")
        {
            Description = "Message type (e.g., \"ADT^A01\", Patient, NewRx) or explicit standard + message type",
            Arity = ArgumentArity.OneOrMore
        };
        
        // Options
        var countOption = CreateIntegerOption("--count", "Number of messages to generate", 1);
        var outputOption = CreateNullableOption("--output", "Output file path (optional, defaults to console)");
        var formatOption = CreateOptionalOption("--format", "Output format: auto|hl7|json|ndjson", "auto");
        var modeOption = CreateOptionalOption("--mode", "Generation mode: procedural|local-ai|api-ai", "procedural");
        var seedOption = CreateNullableOption("--seed", "Deterministic seed for reproducible data");
        var vendorOption = CreateNullableOption("--vendor", "Apply vendor pattern (e.g., epic, cerner, meditech)");

        command.Add(messageTypeArg);
        command.Add(countOption);
        command.Add(outputOption);
        command.Add(formatOption);
        command.Add(modeOption);
        command.Add(seedOption);
        command.Add(vendorOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                // Parse smart inference arguments
                var args = parseResult.GetValue(messageTypeArg) ?? Array.Empty<string>();
                if (args.Length == 0)
                {
                    Logger.LogError("Message type is required. Usage: pidgeon generate <message-type> or pidgeon generate <standard> <message-type>");
                    Console.WriteLine("Examples:");
                    Console.WriteLine("  pidgeon generate \"ADT^A01\"");
                    Console.WriteLine("  pidgeon generate Patient --count 10");
                    Console.WriteLine("  pidgeon generate hl7 \"ADT^A01\"");
                    return 1;
                }

                var parsingResult = SmartCommandParser.Parse(args);
                if (parsingResult.IsFailure)
                {
                    Logger.LogError(parsingResult.Error.Message);
                    Console.WriteLine($"❌ {parsingResult.Error.Message}");
                    Console.WriteLine("Examples:");
                    Console.WriteLine("  pidgeon generate \"ADT^A01\"");
                    Console.WriteLine("  pidgeon generate Patient --count 10");
                    Console.WriteLine("  pidgeon generate hl7 \"ADT^A01\"");
                    return 1;
                }
                
                var request = parsingResult.Value;

                var count = parseResult.GetValue(countOption);
                var output = parseResult.GetValue(outputOption);
                var format = parseResult.GetValue(formatOption);
                var mode = parseResult.GetValue(modeOption);
                var seedStr = parseResult.GetValue(seedOption);
                int? seed = null;
                if (!string.IsNullOrEmpty(seedStr) && int.TryParse(seedStr, out var parsedSeed))
                {
                    seed = parsedSeed;
                }
                var vendor = parseResult.GetValue(vendorOption);

                // Validate Pro features if needed
                if ((mode == "local-ai" || mode == "api-ai") && !IsProFeatureAvailable())
                {
                    Console.WriteLine("AI generation modes require Pidgeon Pro. Using procedural mode.");
                    Console.WriteLine("Upgrade at: pidgeon login --pro");
                    mode = "procedural";
                }

                Console.WriteLine($"Generating {count} {request.Standard.ToUpperInvariant()} {request.MessageType} message(s)...");
                if (request.ExplicitStandardProvided)
                {
                    Console.WriteLine($"Standard: {request.Standard} (explicit)");
                }
                else
                {
                    Console.WriteLine($"Standard: {request.Standard} (inferred from message type)");
                }
            
                var options = new GenerationOptions
                {
                    UseAI = mode != "procedural",
                    Seed = seed,
                    // TODO: Map vendor string to VendorProfile enum
                    VendorProfile = null
                };
                
                var result = await _messageGenerationService.GenerateSyntheticDataAsync(request.Standard, request.MessageType, count, options);
            
                if (result.IsSuccess)
                {
                    if (!string.IsNullOrEmpty(output))
                    {
                        // Write to file
                        await WriteToFileAsync(output, result.Value, format!);
                        Console.WriteLine($"✅ Generated {count} {request.MessageType} message(s) and saved to {output}");
                    }
                    else
                    {
                        // Write to console  
                        WriteToConsole(result.Value, format!);
                        Console.WriteLine($"✅ Generated {count} {request.MessageType} message(s)");
                    }
                    
                    return 0;
                }
                
                return HandleResult(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error during message generation");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private async Task WriteToFileAsync(string filePath, IReadOnlyList<string> messages, string format)
    {
        var content = string.Join(Environment.NewLine + Environment.NewLine, messages);
        await File.WriteAllTextAsync(filePath, content);
        
        Logger.LogInformation("Generated messages written to {FilePath}", filePath);
    }

    private void WriteToConsole(IReadOnlyList<string> messages, string format)
    {
        foreach (var message in messages)
        {
            if (format != "auto")
            {
                // Add some formatting for readability
                Console.WriteLine(new string('-', 50));
            }
            
            Console.WriteLine(message);
            
            if (format != "auto" && messages.Count > 1)
            {
                Console.WriteLine(new string('-', 50));
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// Checks if Pro features are available for the current user.
    /// TODO: Implement actual license checking logic.
    /// </summary>
    private static bool IsProFeatureAvailable()
    {
        // TODO: Replace with actual license validation
        // For now, return false to demonstrate feature gating
        return false;
    }
}