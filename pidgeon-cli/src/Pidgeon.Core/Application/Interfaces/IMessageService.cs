// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Services;

/// <summary>
/// Core message processing service.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Processes a message by parsing, validating, and optionally transforming it.
    /// </summary>
    /// <param name="messageContent">The message content</param>
    /// <param name="options">Processing options</param>
    /// <returns>A result containing the processed message or an error</returns>
    Task<Result<ProcessedMessage>> ProcessMessageAsync(string messageContent, MessageProcessingOptions? options = null);

    /// <summary>
    /// Generates a new message from domain objects.
    /// </summary>
    /// <param name="domainObject">The domain object to serialize</param>
    /// <param name="standard">The target standard</param>
    /// <param name="messageType">The message type to generate</param>
    /// <param name="options">Generation options</param>
    /// <returns>A result containing the generated message or an error</returns>
    Task<Result<string>> GenerateMessageAsync(object domainObject, string standard, string messageType, Generation.GenerationOptions? options = null);
}