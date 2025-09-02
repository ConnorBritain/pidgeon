// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Service responsible for validating messages against vendor configurations.
/// Provides both strict standard compliance and compatibility-mode validation.
/// Single responsibility: "Does this message match the expected vendor patterns?"
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validates a message against a vendor configuration using the specified validation mode.
    /// </summary>
    /// <param name="message">Message to validate</param>
    /// <param name="configuration">Vendor configuration to validate against</param>
    /// <param name="mode">Validation mode (Strict or Compatibility)</param>
    /// <returns>Result containing validation results with detailed feedback</returns>
    Task<Result<ConfigurationValidationResult>> ValidateAsync(
        string message,
        VendorConfiguration configuration,
        ValidationMode mode = ValidationMode.Compatibility);

    /// <summary>
    /// Validates message structure against standard specification.
    /// Used for strict compliance validation.
    /// </summary>
    /// <param name="message">Message to validate</param>
    /// <param name="standard">Healthcare standard (HL7v23, FHIRv4, etc.)</param>
    /// <param name="messageType">Specific message type</param>
    /// <returns>Result containing structural validation results</returns>
    Task<Result<StructuralValidationResult>> ValidateStructureAsync(
        string message,
        string standard,
        string messageType);

    /// <summary>
    /// Validates message content against vendor-specific patterns.
    /// Used for compatibility-mode validation with vendor tolerance.
    /// </summary>
    /// <param name="message">Message to validate</param>
    /// <param name="configuration">Vendor configuration with expected patterns</param>
    /// <returns>Result containing content validation results</returns>
    Task<Result<ContentValidationResult>> ValidateContentAsync(
        string message,
        VendorConfiguration configuration);

    /// <summary>
    /// Detects configuration drift by comparing message patterns against baseline configuration.
    /// Identifies when vendor implementations change over time.
    /// </summary>
    /// <param name="messages">Recent messages to analyze for drift</param>
    /// <param name="baselineConfiguration">Baseline vendor configuration</param>
    /// <returns>Result containing drift analysis with change indicators</returns>
    Task<Result<ConfigurationDriftAnalysis>> DetectConfigurationDriftAsync(
        IEnumerable<string> messages,
        VendorConfiguration baselineConfiguration);

    /// <summary>
    /// Compares two vendor configurations to identify differences.
    /// Useful for version comparison and migration analysis.
    /// </summary>
    /// <param name="fromConfiguration">Original configuration</param>
    /// <param name="toConfiguration">Target configuration</param>
    /// <returns>Result containing detailed configuration comparison</returns>
    Task<Result<ConfigurationComparison>> CompareConfigurationsAsync(
        VendorConfiguration fromConfiguration,
        VendorConfiguration toConfiguration);
}

/// <summary>
/// Enumeration of validation modes for message validation.
/// </summary>
public enum ValidationMode
{
    /// <summary>
    /// Strict validation against healthcare standard specification.
    /// Zero tolerance for deviations from the standard.
    /// Used for: Standards testing, compliance verification.
    /// </summary>
    Strict,

    /// <summary>
    /// Compatibility validation using vendor configuration patterns.
    /// Liberal acceptance with warnings for deviations.
    /// Used for: Real-world interface validation, production systems.
    /// </summary>
    Compatibility
}

/// <summary>
/// Result of structural validation against standard specification.
/// </summary>
public record StructuralValidationResult
{
    public bool IsValid { get; init; }
    public List<string> StructuralErrors { get; init; } = new();
    public List<string> StructuralWarnings { get; init; } = new();
    public string Standard { get; init; } = string.Empty;
    public string MessageType { get; init; } = string.Empty;
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Result of content validation against vendor configuration.
/// </summary>
public record ContentValidationResult
{
    public bool IsValid { get; init; }
    public List<string> ContentErrors { get; init; } = new();
    public List<string> ContentWarnings { get; init; } = new();
    public double PatternMatchConfidence { get; init; }
    public string VendorName { get; init; } = string.Empty;
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Analysis of configuration drift over time.
/// </summary>
public record ConfigurationDriftAnalysis
{
    public bool DriftDetected { get; init; }
    public double DriftConfidence { get; init; }
    public List<string> DriftIndicators { get; init; } = new();
    public Dictionary<string, double> FieldPatternChanges { get; init; } = new();
    public DateTime BaselineDate { get; init; }
    public DateTime AnalysisDate { get; init; } = DateTime.UtcNow;
}