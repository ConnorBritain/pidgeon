// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Segmint.Core.Standards.HL7.v23.Messages;

namespace Segmint.CLI.Services;

/// <summary>
/// Service for generating HL7 messages.
/// </summary>
public interface IMessageGeneratorService
{
    /// <summary>
    /// Generates HL7 messages of the specified type.
    /// </summary>
    /// <param name="messageType">The type of message to generate (RDE, ADT, ACK, etc.).</param>
    /// <param name="count">The number of messages to generate.</param>
    /// <param name="configurationPath">Optional path to configuration file.</param>
    /// <param name="templateNames">Optional template names to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of generated messages.</returns>
    Task<IEnumerable<HL7Message>> GenerateMessagesAsync(
        string messageType,
        int count = 1,
        string? configurationPath = null,
        string[]? templateNames = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available message types.
    /// </summary>
    /// <returns>Collection of supported message types.</returns>
    IEnumerable<string> GetAvailableMessageTypes();

    /// <summary>
    /// Gets available templates for a message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>Collection of available templates.</returns>
    IEnumerable<string> GetAvailableTemplates(string messageType);
}