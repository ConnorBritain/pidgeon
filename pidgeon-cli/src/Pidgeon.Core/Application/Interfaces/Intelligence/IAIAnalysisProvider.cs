// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Intelligence;

namespace Pidgeon.Core.Application.Interfaces.Intelligence;

/// <summary>
/// Base interface for AI analysis providers supporting local and cloud models.
/// Enables healthcare-compliant AI integration without PHI exposure.
/// </summary>
public interface IAIAnalysisProvider
{
    /// <summary>
    /// Unique identifier for this provider (e.g., "ollama", "llama-cpp", "openai")
    /// </summary>
    string ProviderId { get; }
    
    /// <summary>
    /// Whether this provider runs models locally (true) or requires external API calls (false)
    /// </summary>
    bool IsLocal { get; }
    
    /// <summary>
    /// Whether this provider requires an API key for operation
    /// </summary>
    bool RequiresApiKey { get; }
    
    /// <summary>
    /// Check if the provider is currently available and ready for inference
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Perform AI analysis on healthcare data with appropriate context
    /// </summary>
    Task<Result<AnalysisResult>> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken = default);
}