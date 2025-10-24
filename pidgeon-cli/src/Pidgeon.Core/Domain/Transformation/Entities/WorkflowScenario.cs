// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Domain.Transformation.Entities;

/// <summary>
/// Represents a complete workflow scenario for healthcare integration testing.
/// Orchestrates generation, validation, and configuration steps for multi-step testing.
/// </summary>
public record WorkflowScenario
{
    /// <summary>
    /// Unique identifier for this workflow scenario.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Human-readable name for this workflow scenario.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Description of what this workflow scenario accomplishes.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Healthcare standards involved in this workflow (HL7, FHIR, NCPDP).
    /// </summary>
    [JsonPropertyName("standards")]
    public List<string> Standards { get; init; } = new();
    
    /// <summary>
    /// Vendor configurations to apply during this workflow.
    /// </summary>
    [JsonPropertyName("vendorConfigurations")]
    public List<ConfigurationAddress> VendorConfigurations { get; init; } = new();
    
    /// <summary>
    /// Ordered sequence of workflow steps to execute.
    /// </summary>
    [JsonPropertyName("steps")]
    public List<WorkflowStep> Steps { get; init; } = new();
    
    /// <summary>
    /// Tags for categorizing and searching workflows.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();
    
    /// <summary>
    /// Expected total execution time for this workflow.
    /// </summary>
    [JsonPropertyName("estimatedDuration")]
    public TimeSpan EstimatedDuration { get; init; }
    
    /// <summary>
    /// When this workflow scenario was created.
    /// </summary>
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this workflow scenario was last modified.
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Version of this workflow scenario.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";
    
    /// <summary>
    /// Workflow metadata and custom properties.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; init; } = new();
}