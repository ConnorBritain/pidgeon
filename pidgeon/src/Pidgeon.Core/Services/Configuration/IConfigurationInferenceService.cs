// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Types;

namespace Pidgeon.Core.Services.Configuration;

/// <summary>
/// Configuration inference service for analyzing messages and inferring configurations.
/// </summary>
public interface IConfigurationInferenceService
{
    /// <summary>
    /// Analyzes sample messages and infers configuration patterns.
    /// </summary>
    /// <param name="sampleMessages">Sample messages to analyze</param>
    /// <param name="options">Analysis options</param>
    /// <returns>A result containing the inferred configuration</returns>
    Task<Result<InferredConfiguration>> InferConfigurationAsync(IEnumerable<string> sampleMessages, InferenceOptions? options = null);
}