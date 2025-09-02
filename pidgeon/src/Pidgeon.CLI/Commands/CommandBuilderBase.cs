// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Base class for CLI command builders providing common option creation and command execution patterns.
/// Eliminates duplication across command implementations.
/// </summary>
public abstract class CommandBuilderBase
{
    protected readonly ILogger Logger;

    protected CommandBuilderBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Creates the command with all options and actions configured.
    /// Must be implemented by derived classes.
    /// </summary>
    public abstract Command CreateCommand();

    /// <summary>
    /// Creates a required string option with standard validation.
    /// </summary>
    protected static Option<string> CreateRequiredOption(string name, string description)
    {
        return new Option<string>(name)
        {
            Description = description,
            Required = true
        };
    }

    /// <summary>
    /// Creates an optional string option with default value.
    /// </summary>
    protected static Option<string> CreateOptionalOption(string name, string description, string defaultValue)
    {
        return new Option<string>(name)
        {
            Description = description,
            DefaultValueFactory = _ => defaultValue
        };
    }

    /// <summary>
    /// Creates an optional nullable string option.
    /// </summary>
    protected static Option<string?> CreateNullableOption(string name, string description)
    {
        return new Option<string?>(name)
        {
            Description = description
        };
    }

    /// <summary>
    /// Creates a boolean option with default value.
    /// </summary>
    protected static Option<bool> CreateBooleanOption(string name, string description, bool defaultValue = false)
    {
        return new Option<bool>(name)
        {
            Description = description,
            DefaultValueFactory = _ => defaultValue
        };
    }

    /// <summary>
    /// Creates an integer option with default value.
    /// </summary>
    protected static Option<int> CreateIntegerOption(string name, string description, int defaultValue)
    {
        return new Option<int>(name)
        {
            Description = description,
            DefaultValueFactory = _ => defaultValue
        };
    }

    /// <summary>
    /// Validates that a file exists and returns appropriate error code.
    /// </summary>
    protected static int ValidateFileExists(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"File not found: {filePath}");
            return 1;
        }
        return 0;
    }

    /// <summary>
    /// Validates that a directory exists and returns appropriate error code.
    /// </summary>
    protected static int ValidateDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Error: Directory not found: {directoryPath}");
            return 1;
        }
        return 0;
    }

    /// <summary>
    /// Reads all files with specified extension from directory.
    /// </summary>
    protected static async Task<Result<List<string>>> ReadMessagesFromDirectoryAsync(
        string directory, 
        string extension,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var files = Directory.GetFiles(directory, $"*.{extension}");
            if (files.Length == 0)
            {
                return Result<List<string>>.Failure($"No .{extension} files found in: {directory}");
            }

            var messages = new List<string>();
            foreach (var file in files)
            {
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                messages.Add(content);
            }

            return Result<List<string>>.Success(messages);
        }
        catch (Exception ex)
        {
            return Result<List<string>>.Failure($"Error reading files: {ex.Message}");
        }
    }

    /// <summary>
    /// Standard error handling pattern for command execution.
    /// </summary>
    protected int HandleResult<T>(Result<T> result, string? successMessage = null)
    {
        if (result.IsSuccess)
        {
            if (!string.IsNullOrEmpty(successMessage))
                Console.WriteLine(successMessage);
            return 0;
        }

        Console.Error.WriteLine($"Error {result.Error.Code}: {result.Error.Message}");
        if (!string.IsNullOrEmpty(result.Error.Context))
            Console.WriteLine($"Context: {result.Error.Context}");
        
        Logger.LogError("Command failed: {ErrorCode} - {ErrorMessage}", 
            result.Error.Code, result.Error.Message);
        
        return 1;
    }

    /// <summary>
    /// Standard exception handling pattern for command execution.
    /// </summary>
    protected int HandleException(Exception ex, string context = "")
    {
        Console.Error.WriteLine($"An error occurred{(string.IsNullOrEmpty(context) ? "" : $" during {context}")}: {ex.Message}");
        Logger.LogError(ex, "Command execution failed: {Context}", context);
        return 1;
    }

    /// <summary>
    /// Creates a command with standard exception handling wrapper.
    /// </summary>
    protected static void SetCommandAction(Command command, Func<ParseResult, CancellationToken, Task<int>> action)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            try
            {
                return await action(parseResult, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
                return 1;
            }
        });
    }
}