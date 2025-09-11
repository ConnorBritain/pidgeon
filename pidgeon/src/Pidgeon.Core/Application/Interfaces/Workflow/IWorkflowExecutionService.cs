// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Domain.Transformation.Entities;

namespace Pidgeon.Core.Application.Interfaces.Workflow;

/// <summary>
/// Service for executing workflow scenarios by orchestrating multiple Pidgeon engines.
/// Coordinates generation, validation, de-identification, and configuration operations.
/// </summary>
public interface IWorkflowExecutionService
{
    /// <summary>
    /// Executes a complete workflow scenario.
    /// </summary>
    /// <param name="scenario">The workflow scenario to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Workflow execution result with step outcomes</returns>
    Task<Result<WorkflowExecution>> ExecuteWorkflowAsync(WorkflowScenario scenario, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a single workflow step.
    /// </summary>
    /// <param name="step">The workflow step to execute</param>
    /// <param name="context">Execution context with previous step outputs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Step execution result</returns>
    Task<Result<WorkflowStepResult>> ExecuteStepAsync(WorkflowStep step, WorkflowExecutionContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates that a workflow scenario can be executed.
    /// </summary>
    /// <param name="scenario">The workflow scenario to validate</param>
    /// <returns>Validation result with any blocking issues</returns>
    Task<Result<WorkflowValidationSummary>> ValidateWorkflowAsync(WorkflowScenario scenario);
    
    /// <summary>
    /// Gets the status of a running workflow execution.
    /// </summary>
    /// <param name="executionId">The execution ID</param>
    /// <returns>Current execution status</returns>
    Task<Result<WorkflowExecution>> GetExecutionStatusAsync(string executionId);
    
    /// <summary>
    /// Cancels a running workflow execution.
    /// </summary>
    /// <param name="executionId">The execution ID</param>
    /// <returns>Cancellation result</returns>
    Task<Result<bool>> CancelExecutionAsync(string executionId);
}

/// <summary>
/// Context for workflow execution containing shared state and outputs.
/// </summary>
public record WorkflowExecutionContext
{
    /// <summary>
    /// Unique identifier for this execution context.
    /// </summary>
    public string ExecutionId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Working directory for this workflow execution.
    /// </summary>
    public string WorkingDirectory { get; init; } = string.Empty;
    
    /// <summary>
    /// Outputs from previous workflow steps, keyed by step ID.
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> StepOutputs { get; init; } = new();
    
    /// <summary>
    /// Shared variables accessible across all workflow steps.
    /// </summary>
    public Dictionary<string, object> Variables { get; init; } = new();
    
    /// <summary>
    /// Configuration addresses to use during execution.
    /// </summary>
    public List<Core.Domain.Configuration.Entities.ConfigurationAddress> VendorConfigurations { get; init; } = new();
    
    /// <summary>
    /// Execution metadata and custom properties.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Summary of workflow validation results.
/// </summary>
public record WorkflowValidationSummary
{
    /// <summary>
    /// Whether the workflow can be executed.
    /// </summary>
    public bool CanExecute { get; init; }
    
    /// <summary>
    /// Blocking errors that prevent execution.
    /// </summary>
    public List<string> BlockingErrors { get; init; } = new();
    
    /// <summary>
    /// Warning messages about potential issues.
    /// </summary>
    public List<string> Warnings { get; init; } = new();
    
    /// <summary>
    /// Missing dependencies or requirements.
    /// </summary>
    public List<string> MissingDependencies { get; init; } = new();
    
    /// <summary>
    /// Estimated execution time for the workflow.
    /// </summary>
    public TimeSpan EstimatedDuration { get; init; }
}