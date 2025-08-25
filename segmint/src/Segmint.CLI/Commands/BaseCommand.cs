// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace Segmint.CLI.Commands;

/// <summary>
/// Base class for all CLI commands providing common functionality.
/// </summary>
public abstract class BaseCommand
{
    protected readonly ILogger Logger;
    protected readonly IConsoleOutput Console;

    protected BaseCommand(ILogger logger, IConsoleOutput console)
    {
        Logger = logger;
        Console = console;
    }

    /// <summary>
    /// Builds and returns the command for the CLI system.
    /// </summary>
    /// <returns>The configured command</returns>
    public abstract Command Build();

    /// <summary>
    /// Handles the execution result and provides appropriate console output.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <param name="result">The execution result</param>
    /// <param name="successMessage">Message to display on success</param>
    /// <returns>Exit code (0 for success, 1 for failure)</returns>
    protected int HandleResult<T>(Result<T> result, string? successMessage = null)
    {
        if (result.IsSuccess)
        {
            if (!string.IsNullOrEmpty(successMessage))
                Console.WriteSuccess(successMessage);
            return 0;
        }

        Console.WriteError($"{result.Error.Code}: {result.Error.Message}");
        if (!string.IsNullOrEmpty(result.Error.Context))
            Console.WriteLine($"Context: {result.Error.Context}");
        
        Logger.LogError("Command failed: {ErrorCode} - {ErrorMessage}", 
            result.Error.Code, result.Error.Message);
        
        return 1;
    }

    /// <summary>
    /// Handles exceptions that occur during command execution.
    /// </summary>
    /// <param name="ex">The exception</param>
    /// <param name="context">Additional context about the operation</param>
    /// <returns>Exit code (always 1 for errors)</returns>
    protected int HandleException(Exception ex, string context = "")
    {
        Console.WriteError($"An error occurred{(string.IsNullOrEmpty(context) ? "" : $" during {context}")}: {ex.Message}");
        Logger.LogError(ex, "Command execution failed: {Context}", context);
        return 1;
    }
}