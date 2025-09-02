// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Generation;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Command for generating healthcare messages and synthetic test data.
/// </summary>
public class GenerateCommand : CommandBuilderBase
{
    private readonly Pidgeon.Core.Services.IGenerationService _generationService;

    public GenerateCommand(
        ILogger<GenerateCommand> logger,
        Pidgeon.Core.Services.IGenerationService generationService) 
        : base(logger)
    {
        _generationService = generationService;
    }

    public override Command CreateCommand()
    {
        var command = new Command("generate", "Generate healthcare messages and synthetic test data");

        var typeOption = CreateRequiredOption("--type", "Message type to generate (e.g., ADT, RDE, Patient)");
        var standardOption = CreateOptionalOption("--standard", "Healthcare standard (hl7, fhir, ncpdp)", "hl7");
        var countOption = CreateIntegerOption("--count", "Number of messages to generate", 1);
        var outputOption = CreateNullableOption("--output", "Output file path (optional, defaults to console)");
        var formatOption = CreateBooleanOption("--format", "Format output for readability");

        command.Add(typeOption);
        command.Add(standardOption);
        command.Add(countOption);
        command.Add(outputOption);
        command.Add(formatOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var type = parseResult.GetValue(typeOption);
            var standard = parseResult.GetValue(standardOption);
            var count = parseResult.GetValue(countOption);
            var output = parseResult.GetValue(outputOption);
            var format = parseResult.GetValue(formatOption);

            // Validate required parameters (defensive programming)
            if (string.IsNullOrWhiteSpace(type))
            {
                Logger.LogError("Message type is required but was not provided");
                return 1;
            }
            
            if (string.IsNullOrWhiteSpace(standard))
            {
                standard = "hl7"; // Default value is already set, but be explicit
            }

            Console.WriteLine($"Generating {count} {standard} {type} message(s)...");
            
            var options = new GenerationOptions(); // TODO: Set options based on parameters
            var result = await _generationService.GenerateSyntheticDataAsync(standard!, type!, count, options);
            
            if (result.IsSuccess)
            {
                if (!string.IsNullOrEmpty(output))
                {
                    // Write to file
                    await WriteToFileAsync(output, result.Value, format);
                    System.Console.WriteLine($"Generated {count} message(s) and saved to {output}");
                }
                else
                {
                    // Write to console
                    WriteToConsole(result.Value, format);
                    System.Console.WriteLine($"Generated {count} message(s)");
                }
                
                return 0;
            }

            return HandleResult(result);
        });

        return command;
    }

    private async Task WriteToFileAsync(string filePath, IReadOnlyList<string> messages, bool format)
    {
        var content = string.Join(Environment.NewLine + Environment.NewLine, messages);
        await File.WriteAllTextAsync(filePath, content);
        
        Logger.LogInformation("Generated messages written to {FilePath}", filePath);
    }

    private void WriteToConsole(IReadOnlyList<string> messages, bool format)
    {
        foreach (var message in messages)
        {
            if (format)
            {
                // Add some formatting for readability
                Console.WriteLine(new string('-', 50));
            }
            
            Console.WriteLine(message);
            
            if (format && messages.Count > 1)
            {
                Console.WriteLine(new string('-', 50));
                Console.WriteLine();
            }
        }
    }
}