// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Generation.Types;

namespace Pidgeon.Core.Generation;

/// <summary>
/// Configuration options for healthcare data generation.
/// Controls both algorithmic generation (free tier) and AI enhancement (subscription tiers).
/// </summary>
public record GenerationOptions
{
    /// <summary>
    /// Gets the type of patient to generate, affecting age ranges and clinical patterns.
    /// </summary>
    public PatientType Type { get; init; } = PatientType.General;

    /// <summary>
    /// Gets the seed for deterministic generation. When specified, identical options
    /// will produce identical results for reproducible testing.
    /// </summary>
    public int? Seed { get; init; }

    /// <summary>
    /// Gets the vendor profile for EHR-specific formatting patterns.
    /// Basic patterns available in free tier, specific templates in subscription tiers.
    /// </summary>
    public VendorProfile? VendorProfile { get; init; }

    /// <summary>
    /// Gets whether to use AI enhancement for generation.
    /// Available in Professional+ tiers with BYOK or Enterprise with unlimited usage.
    /// </summary>
    public bool UseAI { get; init; } = false;

    /// <summary>
    /// Gets the API key for AI providers (BYOK model for Professional tier).
    /// Not required for Enterprise tier with unlimited AI.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Gets the AI provider to use for enhanced generation.
    /// </summary>
    public AIProvider Provider { get; init; } = AIProvider.None;

    /// <summary>
    /// Gets the AI generation mode for contextual enhancement.
    /// </summary>
    public AIGenerationMode Mode { get; init; } = AIGenerationMode.Enhanced;

    /// <summary>
    /// Gets additional context for generation (diagnosis, specialty, facility type).
    /// Used by both algorithmic and AI generation for appropriate correlations.
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();

    /// <summary>
    /// Creates default generation options for algorithmic generation.
    /// </summary>
    public static GenerationOptions Default => new();

    /// <summary>
    /// Creates options for deterministic testing with specified seed.
    /// </summary>
    public static GenerationOptions ForTesting(int seed) => new() { Seed = seed };

    /// <summary>
    /// Creates options for AI-enhanced generation with BYOK.
    /// </summary>
    public static GenerationOptions WithAI(AIProvider provider, string apiKey) => new()
    {
        UseAI = true,
        Provider = provider,
        ApiKey = apiKey
    };
}