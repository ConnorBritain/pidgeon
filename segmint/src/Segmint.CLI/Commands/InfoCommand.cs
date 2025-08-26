// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Segmint.Core;
using System.CommandLine;

namespace Segmint.CLI.Commands;

/// <summary>
/// Command for displaying information about Segmint and supported standards.
/// </summary>
public class InfoCommand : BaseCommand
{
    public InfoCommand(ILogger<InfoCommand> logger) : base(logger)
    {
    }

    public Command CreateCommand()
    {
        var command = new Command("info", "Display information about Segmint and supported standards");

        command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            Console.WriteLine("Segmint Healthcare Interoperability Platform");
            Console.WriteLine("Version: 1.0.0-dev");
            Console.WriteLine();
            Console.WriteLine("Supported Standards:");
            Console.WriteLine("- HL7 v2.3 (ADT messages)");
            Console.WriteLine();
            Console.WriteLine("For more information, visit: https://github.com/your-repo/segmint");
            return Task.FromResult(0);
        });

        return command;
    }
}