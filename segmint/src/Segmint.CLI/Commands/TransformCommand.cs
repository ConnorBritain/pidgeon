// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Segmint.Core;
using System.CommandLine;

namespace Segmint.CLI.Commands;

/// <summary>
/// Command for transforming messages between different healthcare standards.
/// </summary>
public class TransformCommand : BaseCommand
{
    public TransformCommand(ILogger<TransformCommand> logger) : base(logger)
    {
    }

    public Command CreateCommand()
    {
        var command = new Command("transform", "Transform messages between healthcare standards")
        {
            // TODO: Add options for source/target standards, input/output files
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            Logger.LogInformation("Transform command not yet implemented");
            Console.WriteLine("Transform functionality coming soon!");
            return Task.FromResult(0);
        });

        return command;
    }
}