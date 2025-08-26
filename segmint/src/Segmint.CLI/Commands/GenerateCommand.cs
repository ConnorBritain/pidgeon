// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Segmint.Core.Extensions;
using Segmint.Core.Generation;
using System.CommandLine;

namespace Segmint.CLI.Commands;

/// <summary>
/// Command for generating healthcare messages and synthetic test data.
/// </summary>
public class GenerateCommand : BaseCommand
{
    private readonly Segmint.Core.Extensions.IGenerationService _generationService;

    public GenerateCommand(
        ILogger<GenerateCommand> logger,
        Segmint.Core.Extensions.IGenerationService generationService) 
        : base(logger)
    {
        _generationService = generationService;
    }

    public Command CreateCommand()
    {
        var command = new Command("generate", "Generate healthcare messages and synthetic test data");

        // Options using beta5 API
        var typeOption = new Option<string>("--type")
        {
            Description = "Message type to generate (e.g., ADT, RDE, Patient)",
            Required = true
        };
        
        var standardOption = new Option<string>("--standard")
        {
            Description = "Healthcare standard (hl7, fhir, ncpdp)",
            DefaultValueFactory = _ => "hl7"
        };

        var countOption = new Option<int>("--count")
        {
            Description = "Number of messages to generate",
            DefaultValueFactory = _ => 1
        };

        var outputOption = new Option<string>("--output")
        {
            Description = "Output file path (optional, defaults to console)"
        };

        var formatOption = new Option<bool>("--format")
        {
            Description = "Format output for readability",
            DefaultValueFactory = _ => false
        };

        command.Add(typeOption);
        command.Add(standardOption);
        command.Add(countOption);
        command.Add(outputOption);
        command.Add(formatOption);

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            try
            {
                var type = parseResult.GetValue(typeOption);
                var standard = parseResult.GetValue(standardOption);
                var count = parseResult.GetValue(countOption);
                var output = parseResult.GetValue(outputOption);
                var format = parseResult.GetValue(formatOption);

                Console.WriteLine($"Generating {count} {standard} {type} message(s)...");
                
                var options = new GenerationOptions(); // TODO: Set options based on parameters
                var result = await _generationService.GenerateSyntheticDataAsync(standard, type, count, options);
                
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
            }
            catch (Exception ex)
            {
                return HandleException(ex, "message generation");
            }
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