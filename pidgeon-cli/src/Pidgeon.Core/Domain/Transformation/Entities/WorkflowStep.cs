// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Transformation.Entities;

/// <summary>
/// Represents a single step in a workflow scenario.
/// Each step corresponds to a specific engine operation (generate, validate, deident, etc.).
/// </summary>
public record WorkflowStep
{
    /// <summary>
    /// Unique identifier for this workflow step.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Human-readable name for this step.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Description of what this step accomplishes.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of operation this step performs.
    /// </summary>
    [JsonPropertyName("stepType")]
    public WorkflowStepType StepType { get; init; }
    
    /// <summary>
    /// Order of execution within the workflow scenario.
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; init; }
    
    /// <summary>
    /// Whether this step is required for workflow completion.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; init; } = true;
    
    /// <summary>
    /// Configuration parameters for this step.
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; init; } = new();
    
    /// <summary>
    /// Input dependencies from previous steps.
    /// </summary>
    [JsonPropertyName("inputDependencies")]
    public List<string> InputDependencies { get; init; } = new();
    
    /// <summary>
    /// Output artifacts produced by this step.
    /// </summary>
    [JsonPropertyName("outputs")]
    public List<string> Outputs { get; init; } = new();
    
    /// <summary>
    /// Expected execution time for this step.
    /// </summary>
    [JsonPropertyName("estimatedDuration")]
    public TimeSpan EstimatedDuration { get; init; }
    
    /// <summary>
    /// Validation criteria for this step.
    /// </summary>
    [JsonPropertyName("validationCriteria")]
    public List<WorkflowValidationRule> ValidationCriteria { get; init; } = new();
    
    /// <summary>
    /// Step metadata and custom properties.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Types of workflow steps corresponding to Pidgeon engine operations.
/// </summary>
public enum WorkflowStepType
{
    /// <summary>
    /// Generate healthcare messages using message generation engine.
    /// </summary>
    Generate,
    
    /// <summary>
    /// Validate messages against standards and vendor configurations.
    /// </summary>
    Validate,
    
    /// <summary>
    /// De-identify messages for safe testing and development.
    /// </summary>
    DeIdentify,
    
    /// <summary>
    /// Analyze vendor patterns and create configurations.
    /// </summary>
    ConfigureVendor,
    
    /// <summary>
    /// Compare messages or configurations for differences.
    /// </summary>
    Compare,
    
    /// <summary>
    /// Transform messages between standards or formats.
    /// </summary>
    Transform,
    
    /// <summary>
    /// Custom step with user-defined operations.
    /// </summary>
    Custom
}

/// <summary>
/// Validation rule for workflow step execution.
/// </summary>
public record WorkflowValidationRule
{
    /// <summary>
    /// Name of the validation rule.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Description of what this rule validates.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of validation to perform.
    /// </summary>
    [JsonPropertyName("validationType")]
    public string ValidationType { get; init; } = string.Empty;
    
    /// <summary>
    /// Expected value or pattern for validation.
    /// </summary>
    [JsonPropertyName("expectedValue")]
    public string ExpectedValue { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether this validation rule is required for step success.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; init; } = true;
}