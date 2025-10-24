// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Interfaces.Configuration;

/// <summary>
/// Orchestrator for comprehensive message pattern analysis.
/// Coordinates between different analysis services without implementing analysis logic itself.
/// </summary>
public interface IMessagePatternAnalysisOrchestrator
{
    /// <summary>
    /// Performs comprehensive pattern analysis on a collection of messages.
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="standard">Healthcare standard (HL7, FHIR, etc.)</param>
    /// <param name="messageType">Specific message type within the standard</param>
    /// <returns>Comprehensive message pattern analysis result</returns>
    Task<Result<MessagePattern>> PerformComprehensiveAnalysisAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType);
}