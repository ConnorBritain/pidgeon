// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.CLI.Output;

/// <summary>
/// Implementation of console output operations.
/// </summary>
public class ConsoleOutput : IConsoleOutput
{
    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    public void WriteLine(string message, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    public void WriteError(string message)
    {
        WriteLine($"ERROR: {message}", ConsoleColor.Red);
    }

    public void WriteWarning(string message)
    {
        WriteLine($"WARNING: {message}", ConsoleColor.Yellow);
    }

    public void WriteSuccess(string message)
    {
        WriteLine($"SUCCESS: {message}", ConsoleColor.Green);
    }
}