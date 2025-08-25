// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Segmint.Core.Extensions;
using System.CommandLine;

namespace Segmint.CLI.Commands;

/// <summary>
/// Command for generating healthcare messages and synthetic test data.
/// </summary>
public class GenerateCommand : BaseCommand
{
    private readonly IGenerationService _generationService;

    public GenerateCommand(
        ILogger<GenerateCommand> logger,
        IConsoleOutput console,
        IGenerationService generationService) 
        : base(logger, console)
    {
        _generationService = generationService;
    }

    public override Command Build()
    {
        var command = new Command("generate", "Generate healthcare messages and synthetic test data");

        // Options
        var typeOption = new Option<string>(
            name: "--type",
            description: "Message type to generate (e.g., ADT, RDE, Patient)")
        {
            IsRequired = true
        };
        
        var standardOption = new Option<string>(
            name: "--standard",
            description: "Healthcare standard (hl7, fhir, ncpdp)")
        {
            IsRequired = false
        };
        standardOption.SetDefaultValue("hl7");

        var countOption = new Option<int>(
            name: "--count",
            description: "Number of messages to generate")
        {
            IsRequired = false
        };
        countOption.SetDefaultValue(1);

        var outputOption = new Option<string>(
            name: "--output",
            description: "Output file path (optional, defaults to console)")
        {
            IsRequired = false
        };

        var formatOption = new Option<bool>(
            name: "--format",
            description: "Format output for readability")
        {
            IsRequired = false
        };
        formatOption.SetDefaultValue(false);

        command.AddOption(typeOption);
        command.AddOption(standardOption);
        command.AddOption(countOption);
        command.AddOption(outputOption);
        command.AddOption(formatOption);

        command.SetHandler(async (type, standard, count, output, format) =>
        {
            try
            {
                Console.WriteLine($"Generating {count} {standard} {type} message(s)...");
                
                var options = new GenerationOptions(); // TODO: Set options based on parameters
                var result = await _generationService.GenerateSyntheticDataAsync(standard, type, count, options);
                
                if (result.IsSuccess)
                {
                    if (!string.IsNullOrEmpty(output))
                    {
                        // Write to file
                        await WriteToFileAsync(output, result.Value, format);
                        Console.WriteSuccess($"Generated {count} message(s) and saved to {output}");
                    }
                    else
                    {
                        // Write to console
                        WriteToConsole(result.Value, format);
                        Console.WriteSuccess($"Generated {count} message(s)");
                    }
                    
                    return 0;
                }

                return HandleResult(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "message generation");
            }
        }, typeOption, standardOption, countOption, outputOption, formatOption);

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