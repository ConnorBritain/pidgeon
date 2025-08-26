// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Segmint.Core;
using System.CommandLine;

namespace Segmint.CLI.Commands;

/// <summary>
/// Command for managing Segmint configuration.
/// </summary>
public class ConfigCommand : BaseCommand
{
    public ConfigCommand(ILogger<ConfigCommand> logger) : base(logger)
    {
    }

    public Command CreateCommand()
    {
        var command = new Command("config", "Manage Segmint configuration")
        {
            // TODO: Add subcommands for get, set, list, etc.
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            Logger.LogInformation("Config command not yet implemented");
            Console.WriteLine("Configuration management coming soon!");
            return Task.FromResult(0);
        });

        return command;
    }
}