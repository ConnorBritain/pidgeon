// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Interfaces.Configuration;

/// <summary>
/// Service responsible for analyzing messages and creating vendor configurations.
/// Single responsibility: Message analysis and configuration inference.
/// </summary>
public interface IConfigurationAnalyzer
{
    /// <summary>
    /// Analyzes messages and creates or updates a vendor configuration.
    /// Supports incremental configuration building.
    /// </summary>
    /// <param name="messages">Messages to analyze</param>
    /// <param name="address">Configuration address</param>
    /// <param name="options">Analysis options</param>
    /// <returns>Result containing the vendor configuration</returns>
    Task<Result<VendorConfiguration>> AnalyzeMessagesAsync(
        IEnumerable<string> messages,
        ConfigurationAddress address,
        InferenceOptions? options = null);
}