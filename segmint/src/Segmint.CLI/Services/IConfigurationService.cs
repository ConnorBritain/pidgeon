// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Segmint.Core.Configuration;

namespace Segmint.CLI.Services;

/// <summary>
/// Service for managing configurations.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Loads a configuration from file.
    /// </summary>
    /// <param name="configurationPath">Path to configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loaded configuration.</returns>
    Task<SegmintConfiguration> LoadConfigurationAsync(
        string configurationPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a configuration to file.
    /// </summary>
    /// <param name="configuration">Configuration to save.</param>
    /// <param name="outputPath">Output file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveConfigurationAsync(
        SegmintConfiguration configuration,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a configuration file.
    /// </summary>
    /// <param name="configurationPath">Path to configuration file.</param>
    /// <param name="strictMode">Enable strict validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration validation result.</returns>
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(
        string configurationPath,
        bool strictMode = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two configuration files.
    /// </summary>
    /// <param name="config1Path">First configuration file path.</param>
    /// <param name="config2Path">Second configuration file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration comparison result.</returns>
    Task<ConfigurationComparisonResult> CompareConfigurationsAsync(
        string config1Path,
        string config2Path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes HL7 messages and infers a configuration.
    /// </summary>
    /// <param name="inputPath">Path to HL7 messages file or directory.</param>
    /// <param name="configurationName">Name for the inferred configuration.</param>
    /// <param name="sampleSize">Maximum number of messages to analyze.</param>
    /// <param name="includeStats">Include statistical analysis.</param>
    /// <param name="segments">Specific segments to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Inferred configuration.</returns>
    Task<SegmintConfiguration> AnalyzeAndInferConfigurationAsync(
        string inputPath,
        string? configurationName = null,
        int sampleSize = 100,
        bool includeStats = false,
        string[]? segments = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available configuration templates.
    /// </summary>
    /// <returns>Collection of available templates.</returns>
    IEnumerable<ConfigurationTemplate> GetAvailableTemplates();

    /// <summary>
    /// Loads a configuration template by name.
    /// </summary>
    /// <param name="templateName">Name of the template.</param>
    /// <returns>Configuration template.</returns>
    Task<SegmintConfiguration> LoadTemplateAsync(string templateName);
}

/// <summary>
/// Result of configuration validation.
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// Gets or sets whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets validation warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets validation information messages.
    /// </summary>
    public List<string> Information { get; set; } = new();
}

/// <summary>
/// Result of configuration comparison.
/// </summary>
public class ConfigurationComparisonResult
{
    /// <summary>
    /// Gets or sets whether the configurations are identical.
    /// </summary>
    public bool AreIdentical { get; set; }

    /// <summary>
    /// Gets or sets differences found between configurations.
    /// </summary>
    public List<ConfigurationDifference> Differences { get; set; } = new();

    /// <summary>
    /// Gets or sets the comparison summary.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Represents a difference between two configurations.
/// </summary>
public class ConfigurationDifference
{
    /// <summary>
    /// Gets or sets the path to the different property.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of difference.
    /// </summary>
    public DifferenceType Type { get; set; }

    /// <summary>
    /// Gets or sets the value in the first configuration.
    /// </summary>
    public string? Value1 { get; set; }

    /// <summary>
    /// Gets or sets the value in the second configuration.
    /// </summary>
    public string? Value2 { get; set; }

    /// <summary>
    /// Gets or sets the description of the difference.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Types of configuration differences.
/// </summary>
public enum DifferenceType
{
    /// <summary>
    /// Property exists in first config but not second.
    /// </summary>
    Added,

    /// <summary>
    /// Property exists in second config but not first.
    /// </summary>
    Removed,

    /// <summary>
    /// Property value changed between configurations.
    /// </summary>
    Modified
}

/// <summary>
/// Represents a configuration template.
/// </summary>
public class ConfigurationTemplate
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets supported message types.
    /// </summary>
    public List<string> MessageTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the template version.
    /// </summary>
    public string Version { get; set; } = string.Empty;
}