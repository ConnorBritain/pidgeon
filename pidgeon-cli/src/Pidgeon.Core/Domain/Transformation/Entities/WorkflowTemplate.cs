// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Transformation.Entities;

/// <summary>
/// Represents a reusable workflow template for common healthcare testing scenarios.
/// Templates can be instantiated into WorkflowScenarios with specific parameters.
/// </summary>
public record WorkflowTemplate
{
    /// <summary>
    /// Unique identifier for this workflow template.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Human-readable name for this workflow template.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Description of what this workflow template accomplishes.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Category of workflow template (Integration, Testing, Validation, etc.).
    /// </summary>
    [JsonPropertyName("category")]
    public WorkflowTemplateCategory Category { get; init; }
    
    /// <summary>
    /// Healthcare standards this template is designed for.
    /// </summary>
    [JsonPropertyName("supportedStandards")]
    public List<string> SupportedStandards { get; init; } = new();
    
    /// <summary>
    /// Common vendor configurations this template works with.
    /// </summary>
    [JsonPropertyName("supportedVendors")]
    public List<string> SupportedVendors { get; init; } = new();
    
    /// <summary>
    /// Template for workflow steps with parameter placeholders.
    /// </summary>
    [JsonPropertyName("stepTemplates")]
    public List<WorkflowStepTemplate> StepTemplates { get; init; } = new();
    
    /// <summary>
    /// Parameters that can be customized when using this template.
    /// </summary>
    [JsonPropertyName("parameters")]
    public List<WorkflowTemplateParameter> Parameters { get; init; } = new();
    
    /// <summary>
    /// Tags for categorizing and searching templates.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();
    
    /// <summary>
    /// Difficulty level for using this template.
    /// </summary>
    [JsonPropertyName("difficultyLevel")]
    public WorkflowDifficultyLevel DifficultyLevel { get; init; }
    
    /// <summary>
    /// Estimated time to complete workflows based on this template.
    /// </summary>
    [JsonPropertyName("estimatedDuration")]
    public TimeSpan EstimatedDuration { get; init; }
    
    /// <summary>
    /// When this template was created.
    /// </summary>
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Version of this workflow template.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";
    
    /// <summary>
    /// Template metadata and custom properties.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Categories of workflow templates for organization and discovery.
/// </summary>
public enum WorkflowTemplateCategory
{
    /// <summary>
    /// Templates for basic integration testing scenarios.
    /// </summary>
    Integration,
    
    /// <summary>
    /// Templates for comprehensive validation testing.
    /// </summary>
    Validation,
    
    /// <summary>
    /// Templates for vendor-specific configuration scenarios.
    /// </summary>
    VendorSpecific,
    
    /// <summary>
    /// Templates for data de-identification workflows.
    /// </summary>
    DeIdentification,
    
    /// <summary>
    /// Templates for performance and load testing.
    /// </summary>
    Performance,
    
    /// <summary>
    /// Templates for debugging and troubleshooting.
    /// </summary>
    Debugging,
    
    /// <summary>
    /// Custom user-defined templates.
    /// </summary>
    Custom
}

/// <summary>
/// Difficulty levels for workflow templates.
/// </summary>
public enum WorkflowDifficultyLevel
{
    /// <summary>
    /// Basic templates suitable for beginners.
    /// </summary>
    Beginner,
    
    /// <summary>
    /// Intermediate templates requiring some healthcare knowledge.
    /// </summary>
    Intermediate,
    
    /// <summary>
    /// Advanced templates for experienced users.
    /// </summary>
    Advanced,
    
    /// <summary>
    /// Expert-level templates for complex scenarios.
    /// </summary>
    Expert
}

/// <summary>
/// Template for a workflow step with parameter placeholders.
/// </summary>
public record WorkflowStepTemplate
{
    /// <summary>
    /// Template name for this step.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Template description with parameter placeholders.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of workflow step.
    /// </summary>
    [JsonPropertyName("stepType")]
    public WorkflowStepType StepType { get; init; }
    
    /// <summary>
    /// Order of execution within the template.
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; init; }
    
    /// <summary>
    /// Parameter template with placeholders.
    /// </summary>
    [JsonPropertyName("parameterTemplate")]
    public Dictionary<string, string> ParameterTemplate { get; init; } = new();
}

/// <summary>
/// Parameter definition for workflow templates.
/// </summary>
public record WorkflowTemplateParameter
{
    /// <summary>
    /// Parameter name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Parameter description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Parameter data type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether this parameter is required.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; init; } = true;
    
    /// <summary>
    /// Default value for this parameter.
    /// </summary>
    [JsonPropertyName("defaultValue")]
    public string DefaultValue { get; init; } = string.Empty;
    
    /// <summary>
    /// Valid values for this parameter (if restricted).
    /// </summary>
    [JsonPropertyName("validValues")]
    public List<string> ValidValues { get; init; } = new();
}