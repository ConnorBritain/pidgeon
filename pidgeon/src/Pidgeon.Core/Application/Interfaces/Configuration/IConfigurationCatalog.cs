// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Configuration catalog service for managing vendor configurations.
/// Supports hierarchical addressing, incremental building, and cross-standard queries.
/// </summary>
public interface IConfigurationCatalog
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

    // === Configuration Retrieval ===

    /// <summary>
    /// Gets a configuration by exact address.
    /// </summary>
    /// <param name="address">Configuration address</param>
    /// <returns>Vendor configuration if found</returns>
    Task<Result<VendorConfiguration?>> GetConfigurationAsync(ConfigurationAddress address);

    /// <summary>
    /// Gets all configurations for a specific vendor.
    /// </summary>
    /// <param name="vendor">Vendor name</param>
    /// <returns>List of vendor configurations</returns>
    Task<Result<IReadOnlyList<VendorConfiguration>>> GetByVendorAsync(string vendor);

    /// <summary>
    /// Gets all configurations for a specific standard.
    /// </summary>
    /// <param name="standard">Standard name</param>
    /// <returns>List of vendor configurations</returns>
    Task<Result<IReadOnlyList<VendorConfiguration>>> GetByStandardAsync(string standard);

    /// <summary>
    /// Gets all configurations for a specific message type.
    /// </summary>
    /// <param name="messageType">Message type</param>
    /// <returns>List of vendor configurations</returns>
    Task<Result<IReadOnlyList<VendorConfiguration>>> GetByMessageTypeAsync(string messageType);

    /// <summary>
    /// Lists all available configurations.
    /// </summary>
    /// <returns>List of all vendor configurations</returns>
    Task<Result<IReadOnlyList<VendorConfiguration>>> ListAllAsync();

    // === Configuration Comparison ===

    /// <summary>
    /// Compares two configurations and returns differences.
    /// </summary>
    /// <param name="fromAddress">Source configuration address</param>
    /// <param name="toAddress">Target configuration address</param>
    /// <returns>Configuration comparison result</returns>
    Task<Result<ConfigurationComparison>> CompareConfigurationsAsync(
        ConfigurationAddress fromAddress,
        ConfigurationAddress toAddress);

    /// <summary>
    /// Finds configurations similar to the provided reference.
    /// </summary>
    /// <param name="reference">Reference configuration</param>
    /// <param name="threshold">Similarity threshold (0.0 to 1.0)</param>
    /// <returns>List of similar configurations</returns>
    Task<Result<IReadOnlyList<VendorConfiguration>>> FindSimilarAsync(
        VendorConfiguration reference,
        double threshold = 0.7);

    // === Configuration Management ===

    /// <summary>
    /// Stores or updates a configuration.
    /// </summary>
    /// <param name="configuration">Configuration to store</param>
    /// <returns>Success or failure result</returns>
    Task<Result> StoreConfigurationAsync(VendorConfiguration configuration);

    /// <summary>
    /// Removes a configuration by address.
    /// </summary>
    /// <param name="address">Configuration address to remove</param>
    /// <returns>Success or failure result</returns>
    Task<Result> RemoveConfigurationAsync(ConfigurationAddress address);

    /// <summary>
    /// Gets configuration change history for evolution tracking.
    /// </summary>
    /// <param name="address">Configuration address</param>
    /// <param name="timeWindow">Time window to look back</param>
    /// <returns>List of configuration changes</returns>
    Task<Result<IReadOnlyList<ConfigurationChange>>> GetChangeHistoryAsync(
        ConfigurationAddress address,
        TimeSpan? timeWindow = null);

    // === Analytics ===

    /// <summary>
    /// Gets statistics about the configuration catalog.
    /// </summary>
    /// <returns>Catalog statistics</returns>
    Task<Result<ConfigurationCatalogStats>> GetStatisticsAsync();

    /// <summary>
    /// Validates a message against an existing configuration.
    /// </summary>
    /// <param name="message">Message to validate</param>
    /// <param name="address">Configuration to validate against</param>
    /// <returns>Validation result</returns>
    Task<Result<ConfigurationValidationResult>> ValidateMessageAsync(
        string message,
        ConfigurationAddress address);
}

/// <summary>
/// Results of comparing two configurations.
/// </summary>
public record ConfigurationComparison
{
    /// <summary>
    /// Source configuration address.
    /// </summary>
    public ConfigurationAddress FromAddress { get; init; } = default!;

    /// <summary>
    /// Target configuration address.
    /// </summary>
    public ConfigurationAddress ToAddress { get; init; } = default!;

    /// <summary>
    /// Similarity score (0.0 = completely different, 1.0 = identical).
    /// </summary>
    public double SimilarityScore { get; init; }

    /// <summary>
    /// List of differences between configurations.
    /// </summary>
    public List<ConfigurationDifference> Differences { get; init; } = new();

    /// <summary>
    /// Summary of the comparison.
    /// </summary>
    public string Summary { get; init; } = default!;
}

/// <summary>
/// Represents a difference between two configurations.
/// </summary>
public record ConfigurationDifference
{
    /// <summary>
    /// Type of difference (e.g., "FieldAdded", "PatternChanged").
    /// </summary>
    public string DifferenceType { get; init; } = default!;

    /// <summary>
    /// Description of the difference.
    /// </summary>
    public string Description { get; init; } = default!;

    /// <summary>
    /// Impact level of this difference.
    /// </summary>
    public DifferenceImpact Impact { get; init; }

    /// <summary>
    /// Additional details about the difference.
    /// </summary>
    public Dictionary<string, object> Details { get; init; } = new();
}

/// <summary>
/// Impact level of a configuration difference.
/// </summary>
public enum DifferenceImpact
{
    /// <summary>Minor cosmetic difference.</summary>
    Minor,
    /// <summary>Moderate difference that may affect some use cases.</summary>
    Moderate,
    /// <summary>Major difference that significantly changes behavior.</summary>
    Major,
    /// <summary>Breaking change that makes configurations incompatible.</summary>
    Breaking
}

/// <summary>
/// Statistics about the configuration catalog.
/// </summary>
public record ConfigurationCatalogStats
{
    /// <summary>
    /// Total number of configurations.
    /// </summary>
    public int TotalConfigurations { get; init; }

    /// <summary>
    /// Number of unique vendors.
    /// </summary>
    public int UniqueVendors { get; init; }

    /// <summary>
    /// Number of unique standards.
    /// </summary>
    public int UniqueStandards { get; init; }

    /// <summary>
    /// Number of unique message types.
    /// </summary>
    public int UniqueMessageTypes { get; init; }

    /// <summary>
    /// Total number of messages analyzed.
    /// </summary>
    public int TotalMessagesAnalyzed { get; init; }

    /// <summary>
    /// Average confidence score across all configurations.
    /// </summary>
    public double AverageConfidence { get; init; }

    /// <summary>
    /// When the catalog was last updated.
    /// </summary>
    public DateTime LastUpdated { get; init; }

    /// <summary>
    /// Breakdown by vendor.
    /// </summary>
    public Dictionary<string, int> VendorCounts { get; init; } = new();

    /// <summary>
    /// Breakdown by standard.
    /// </summary>
    public Dictionary<string, int> StandardCounts { get; init; } = new();
}