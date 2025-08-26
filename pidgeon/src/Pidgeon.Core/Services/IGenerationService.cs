// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Services;

/// <summary>
/// Message generation service.
/// </summary>
public interface IGenerationService
{
    /// <summary>
    /// Generates synthetic test data for a given standard and message type.
    /// </summary>
    /// <param name="standard">The target standard</param>
    /// <param name="messageType">The message type</param>
    /// <param name="count">Number of messages to generate</param>
    /// <param name="options">Generation options</param>
    /// <returns>A result containing the generated messages or an error</returns>
    Task<Result<IReadOnlyList<string>>> GenerateSyntheticDataAsync(string standard, string messageType, int count = 1, Generation.GenerationOptions? options = null);
}