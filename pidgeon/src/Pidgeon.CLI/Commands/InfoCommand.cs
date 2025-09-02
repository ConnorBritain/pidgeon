// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Command for displaying information about Pidgeon and supported standards.
/// </summary>
public class InfoCommand : CommandBuilderBase
{
    public InfoCommand(ILogger<InfoCommand> logger) : base(logger)
    {
    }

    public override Command CreateCommand()
    {
        var command = new Command("info", "Display information about Pidgeon and supported standards");

        command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            Console.WriteLine("Pidgeon Healthcare Interoperability Platform");
            Console.WriteLine("Version: 1.0.0-dev");
            Console.WriteLine();
            Console.WriteLine("Supported Standards:");
            Console.WriteLine("- HL7 v2.3 (ADT messages)");
            Console.WriteLine();
            Console.WriteLine("For more information, visit: https://github.com/your-repo/Pidgeon");
            return Task.FromResult(0);
        });

        return command;
    }
}