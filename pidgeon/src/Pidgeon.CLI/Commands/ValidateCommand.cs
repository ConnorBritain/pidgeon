// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Services;
using Pidgeon.Core.Standards.Common;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Command for validating healthcare messages.
/// </summary>
public class ValidateCommand : BaseCommand
{
    private readonly IValidationService _validationService;

    public ValidateCommand(
        ILogger<ValidateCommand> logger,
        IValidationService validationService) 
        : base(logger)
    {
        _validationService = validationService;
    }

    public Command CreateCommand()
    {
        var command = new Command("validate", "Validate healthcare messages against standards");

        var fileOption = new Option<string>("--file")
        {
            Description = "Path to the message file to validate",
            Required = true
        };

        var modeOption = new Option<ValidationMode>("--mode")
        {
            Description = "Validation mode (Strict, Compatibility, Lenient)",
            DefaultValueFactory = _ => ValidationMode.Strict
        };

        var standardOption = new Option<string?>("--standard")
        {
            Description = "Specific standard to validate against (auto-detect if not specified)"
        };

        command.Add(fileOption);
        command.Add(modeOption);
        command.Add(standardOption);

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            try
            {
                var file = parseResult.GetValue(fileOption);
                var mode = parseResult.GetValue(modeOption);
                var standard = parseResult.GetValue(standardOption);

                if (!File.Exists(file))
                {
                    System.Console.Error.WriteLine($"File not found: {file}");
                    return 1;
                }

                Console.WriteLine($"Validating {file} with {mode} mode...");
                
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                var result = await _validationService.ValidateAsync(content, mode, standard);
                
                if (result.IsSuccess)
                {
                    var validation = result.Value;
                    if (validation.IsValid)
                    {
                        System.Console.WriteLine("Validation passed!");
                        if (validation.Warnings.Any())
                        {
                            Console.WriteLine($"Warnings: {validation.Warnings.Count}");
                            foreach (var warning in validation.Warnings)
                            {
                                System.Console.WriteLine($"{warning.Code}: {warning.Message}");
                            }
                        }
                    }
                    else
                    {
                        System.Console.Error.WriteLine($"Validation failed with {validation.Errors.Count} error(s):");
                        foreach (var error in validation.Errors)
                        {
                            System.Console.Error.WriteLine($"  {error.Code}: {error.Message}");
                            if (!string.IsNullOrEmpty(error.Location))
                                Console.WriteLine($"    Location: {error.Location}");
                        }
                        return 1;
                    }
                }
                
                return HandleResult(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "validation");
            }
        });

        return command;
    }
}