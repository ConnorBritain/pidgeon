// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.CLI.Output;

/// <summary>
/// Interface for console output operations.
/// </summary>
public interface IConsoleOutput
{
    /// <summary>
    /// Writes a line to the console.
    /// </summary>
    /// <param name="message">The message to write</param>
    void WriteLine(string message);

    /// <summary>
    /// Writes a line to the console with the specified color.
    /// </summary>
    /// <param name="message">The message to write</param>
    /// <param name="color">The console color to use</param>
    void WriteLine(string message, ConsoleColor color);

    /// <summary>
    /// Writes an error message to the console.
    /// </summary>
    /// <param name="message">The error message</param>
    void WriteError(string message);

    /// <summary>
    /// Writes a warning message to the console.
    /// </summary>
    /// <param name="message">The warning message</param>
    void WriteWarning(string message);

    /// <summary>
    /// Writes a success message to the console.
    /// </summary>
    /// <param name="message">The success message</param>
    void WriteSuccess(string message);
}