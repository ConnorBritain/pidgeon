// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Segmint.Core.Extensions;
using Segmint.Core.Standards.Common;
using System.CommandLine;

namespace Segmint.CLI.Commands;

/// <summary>
/// Command for validating healthcare messages.
/// </summary>
public class ValidateCommand : BaseCommand
{
    private readonly IValidationService _validationService;

    public ValidateCommand(
        ILogger<ValidateCommand> logger,
        IConsoleOutput console,
        IValidationService validationService) 
        : base(logger, console)
    {
        _validationService = validationService;
    }

    public override Command Build()
    {
        var command = new Command("validate", "Validate healthcare messages against standards");

        var fileOption = new Option<string>(
            name: "--file",
            description: "Path to the message file to validate")
        {
            IsRequired = true
        };

        var modeOption = new Option<ValidationMode>(
            name: "--mode",
            description: "Validation mode (Strict, Compatibility, Lenient)")
        {
            IsRequired = false
        };
        modeOption.SetDefaultValue(ValidationMode.Strict);

        var standardOption = new Option<string?>(
            name: "--standard",
            description: "Specific standard to validate against (auto-detect if not specified)")
        {
            IsRequired = false
        };

        command.AddOption(fileOption);
        command.AddOption(modeOption);
        command.AddOption(standardOption);

        command.SetHandler(async (file, mode, standard) =>
        {
            try
            {
                if (!File.Exists(file))
                {
                    Console.WriteError($"File not found: {file}");
                    return 1;
                }

                Console.WriteLine($"Validating {file} with {mode} mode...");
                
                var content = await File.ReadAllTextAsync(file);
                var result = await _validationService.ValidateAsync(content, mode, standard);
                
                if (result.IsSuccess)
                {
                    var validation = result.Value;
                    if (validation.IsValid)
                    {
                        Console.WriteSuccess("Validation passed!");
                        if (validation.Warnings.Any())
                        {
                            Console.WriteLine($"Warnings: {validation.Warnings.Count}");
                            foreach (var warning in validation.Warnings)
                            {
                                Console.WriteWarning($"{warning.Code}: {warning.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteError($"Validation failed with {validation.Errors.Count} error(s):");
                        foreach (var error in validation.Errors)
                        {
                            Console.WriteError($"  {error.Code}: {error.Message}");
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
        }, fileOption, modeOption, standardOption);

        return command;
    }
}