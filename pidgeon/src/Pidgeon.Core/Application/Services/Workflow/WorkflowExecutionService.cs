// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core;
using Pidgeon.Core.Application.Services.DeIdentification;
using Pidgeon.Core.Application.Interfaces.Generation;
using Pidgeon.Core.Application.Interfaces;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Application.Interfaces.Workflow;
using Pidgeon.Core.Domain.Transformation.Entities;
using Pidgeon.Core.Domain.DeIdentification;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Workflow;

/// <summary>
/// Service for executing workflow scenarios by orchestrating multiple Pidgeon engines.
/// Acts as the "circulatory system" connecting all existing engine "organs".
/// </summary>
internal class WorkflowExecutionService : IWorkflowExecutionService
{
    private readonly IMessageGenerationService _generationService;
    private readonly IMessageValidationService _validationService;
    private readonly IDeIdentificationEngine _deIdentificationEngine;
    private readonly IMultiStandardVendorDetectionService _vendorDetectionService;
    private readonly ILogger<WorkflowExecutionService> _logger;
    
    // Track running executions
    private readonly Dictionary<string, WorkflowExecution> _runningExecutions = new();

    public WorkflowExecutionService(
        IMessageGenerationService generationService,
        IMessageValidationService validationService,
        IDeIdentificationEngine deIdentificationEngine,
        IMultiStandardVendorDetectionService vendorDetectionService,
        ILogger<WorkflowExecutionService> logger)
    {
        _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _deIdentificationEngine = deIdentificationEngine ?? throw new ArgumentNullException(nameof(deIdentificationEngine));
        _vendorDetectionService = vendorDetectionService ?? throw new ArgumentNullException(nameof(vendorDetectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<WorkflowExecution>> ExecuteWorkflowAsync(WorkflowScenario scenario, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting workflow execution: {WorkflowName}", scenario.Name);
        
        // Create execution tracking
        var execution = new WorkflowExecution
        {
            ScenarioId = scenario.Id,
            Status = WorkflowExecutionStatus.Running,
            StartTime = DateTime.UtcNow
        };
        
        _runningExecutions[execution.Id] = execution;
        
        try
        {
            // Validate workflow before execution
            var validationResult = await ValidateWorkflowAsync(scenario);
            if (validationResult.IsFailure)
            {
                return await CompleteExecutionWithFailure(execution, validationResult.Error.Message);
            }
            
            if (!validationResult.Value.CanExecute)
            {
                var errors = string.Join("; ", validationResult.Value.BlockingErrors);
                return await CompleteExecutionWithFailure(execution, $"Workflow validation failed: {errors}");
            }
            
            // Create execution context
            var context = new WorkflowExecutionContext
            {
                ExecutionId = execution.Id,
                WorkingDirectory = Path.Combine(Path.GetTempPath(), "pidgeon_workflow", execution.Id),
                VendorConfigurations = scenario.VendorConfigurations
            };
            
            // Ensure working directory exists
            Directory.CreateDirectory(context.WorkingDirectory);
            
            // Execute steps in order
            var stepResults = new List<WorkflowStepResult>();
            var orderedSteps = scenario.Steps.OrderBy(s => s.Order).ToList();
            
            foreach (var step in orderedSteps)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return await CompleteExecutionWithCancellation(execution);
                }
                
                _logger.LogInformation("Executing workflow step: {StepName} ({StepType})", step.Name, step.StepType);
                
                var stepResult = await ExecuteStepAsync(step, context, cancellationToken);
                if (stepResult.IsFailure)
                {
                    _logger.LogError("Workflow step failed: {StepName} - {Error}", step.Name, stepResult.Error);
                    
                    if (step.Required)
                    {
                        return await CompleteExecutionWithFailure(execution, $"Required step '{step.Name}' failed: {stepResult.Error.Message}");
                    }
                    else
                    {
                        // Skip optional failed steps
                        var skippedResult = new WorkflowStepResult
                        {
                            StepId = step.Id,
                            StepName = step.Name,
                            Status = WorkflowStepStatus.Failed,
                            ErrorMessage = stepResult.Error.Message,
                            StartTime = DateTime.UtcNow,
                            EndTime = DateTime.UtcNow
                        };
                        stepResults.Add(skippedResult);
                        continue;
                    }
                }
                
                var result = stepResult.Value;
                stepResults.Add(result);
                
                // Store step outputs for subsequent steps
                context.StepOutputs[step.Id] = result.Output;
                
                _logger.LogInformation("Workflow step completed: {StepName}", step.Name);
            }
            
            // Complete execution successfully
            var completedExecution = execution with
            {
                Status = WorkflowExecutionStatus.Completed,
                EndTime = DateTime.UtcNow,
                StepResults = stepResults,
                SuccessRate = CalculateSuccessRate(stepResults)
            };
            
            _runningExecutions[execution.Id] = completedExecution;
            
            _logger.LogInformation("Workflow execution completed: {WorkflowName} (Success Rate: {SuccessRate:P1})", 
                scenario.Name, completedExecution.SuccessRate);
            
            return Result<WorkflowExecution>.Success(completedExecution);
        }
        catch (OperationCanceledException)
        {
            return await CompleteExecutionWithCancellation(execution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow execution failed: {WorkflowName}", scenario.Name);
            return await CompleteExecutionWithFailure(execution, $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<WorkflowStepResult>> ExecuteStepAsync(WorkflowStep step, WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var stepResult = new WorkflowStepResult
        {
            StepId = step.Id,
            StepName = step.Name,
            Status = WorkflowStepStatus.Running,
            StartTime = DateTime.UtcNow
        };

        try
        {
            switch (step.StepType)
            {
                case WorkflowStepType.Generate:
                    return await ExecuteGenerateStepAsync(step, context, stepResult, cancellationToken);
                    
                case WorkflowStepType.Validate:
                    return await ExecuteValidateStepAsync(step, context, stepResult, cancellationToken);
                    
                case WorkflowStepType.DeIdentify:
                    return await ExecuteDeIdentifyStepAsync(step, context, stepResult, cancellationToken);
                    
                case WorkflowStepType.ConfigureVendor:
                    return await ExecuteConfigureVendorStepAsync(step, context, stepResult, cancellationToken);
                    
                case WorkflowStepType.Compare:
                    return await ExecuteCompareStepAsync(step, context, stepResult, cancellationToken);
                    
                case WorkflowStepType.Transform:
                    return await ExecuteTransformStepAsync(step, context, stepResult, cancellationToken);
                    
                case WorkflowStepType.Custom:
                    return await ExecuteCustomStepAsync(step, context, stepResult, cancellationToken);
                    
                default:
                    return Result<WorkflowStepResult>.Failure($"Unknown step type: {step.StepType}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Step execution failed: {StepName}", step.Name);
            return Result<WorkflowStepResult>.Failure($"Step execution error: {ex.Message}");
        }
    }

    private async Task<Result<WorkflowStepResult>> ExecuteGenerateStepAsync(
        WorkflowStep step, 
        WorkflowExecutionContext context, 
        WorkflowStepResult stepResult, 
        CancellationToken cancellationToken)
    {
        // Extract parameters
        var messageType = step.Parameters.GetValueOrDefault("messageType", "ADT^A01")?.ToString() ?? "ADT^A01";
        var standard = step.Parameters.GetValueOrDefault("standard", "hl7")?.ToString() ?? "hl7"; // Use standard names from generation service
        var count = int.Parse(step.Parameters.GetValueOrDefault("count", 1)?.ToString() ?? "1");
        
        // Generate messages using the actual service interface
        var result = await _generationService.GenerateSyntheticDataAsync(standard, messageType, count, null);
        if (result.IsFailure)
        {
            return Result<WorkflowStepResult>.Failure($"Message generation failed: {result.Error}");
        }
        
        // Save generated messages to working directory
        var outputDir = Path.Combine(context.WorkingDirectory, "generated");
        Directory.CreateDirectory(outputDir);
        
        var generatedFiles = new List<string>();
        for (int i = 0; i < result.Value.Count; i++)
        {
            var fileName = $"{messageType.Replace("^", "_")}_{i + 1}.{GetFileExtension(standard)}";
            var filePath = Path.Combine(outputDir, fileName);
            await File.WriteAllTextAsync(filePath, result.Value[i], cancellationToken);
            generatedFiles.Add(filePath);
        }
        
        return Result<WorkflowStepResult>.Success(stepResult with
        {
            Status = WorkflowStepStatus.Success,
            EndTime = DateTime.UtcNow,
            Output = new Dictionary<string, object>
            {
                ["messageCount"] = result.Value.Count,
                ["outputDirectory"] = outputDir,
                ["generatedFiles"] = generatedFiles,
                ["messageType"] = messageType,
                ["standard"] = standard
            }
        });
    }

    private async Task<Result<WorkflowStepResult>> ExecuteValidateStepAsync(
        WorkflowStep step, 
        WorkflowExecutionContext context, 
        WorkflowStepResult stepResult, 
        CancellationToken cancellationToken)
    {
        // Get input files from previous steps or parameters
        var inputFiles = GetInputFiles(step, context);
        if (!inputFiles.Any())
        {
            return Result<WorkflowStepResult>.Failure("No input files found for validation");
        }
        
        var validationResults = new List<Core.Domain.Validation.ValidationResult>();
        
        foreach (var filePath in inputFiles)
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            
            var standard = step.Parameters.GetValueOrDefault("standard", "hl7")?.ToString() ?? "hl7";
            var mode = Enum.Parse<Core.Domain.Validation.ValidationMode>(
                step.Parameters.GetValueOrDefault("mode", "Strict")?.ToString() ?? "Strict");
            
            var result = await _validationService.ValidateAsync(content, standard, mode);
            if (result.IsFailure)
            {
                return Result<WorkflowStepResult>.Failure($"Validation failed for file {Path.GetFileName(filePath)}: {result.Error.Message}");
            }
            
            validationResults.Add(result.Value);
        }
        
        var overallSuccess = validationResults.All(r => r.IsValid);
        var totalErrors = validationResults.SelectMany(r => r.Issues.Where(i => i.Severity == Core.Domain.Validation.ValidationSeverity.Error).Select(i => i.Message)).ToList();
        var totalWarnings = validationResults.SelectMany(r => r.Issues.Where(i => i.Severity == Core.Domain.Validation.ValidationSeverity.Warning).Select(i => i.Message)).ToList();
        
        return Result<WorkflowStepResult>.Success(stepResult with
        {
            Status = overallSuccess ? WorkflowStepStatus.Success : WorkflowStepStatus.Failed,
            EndTime = DateTime.UtcNow,
            Output = new Dictionary<string, object>
            {
                ["validationResults"] = validationResults,
                ["filesValidated"] = inputFiles.Count,
                ["overallSuccess"] = overallSuccess,
                ["totalErrors"] = totalErrors.Count,
                ["totalWarnings"] = totalWarnings.Count,
                ["errors"] = totalErrors,
                ["warnings"] = totalWarnings
            }
        });
    }

    private async Task<Result<WorkflowStepResult>> ExecuteDeIdentifyStepAsync(
        WorkflowStep step, 
        WorkflowExecutionContext context, 
        WorkflowStepResult stepResult, 
        CancellationToken cancellationToken)
    {
        // Get input files from previous steps or parameters
        var inputFiles = GetInputFiles(step, context);
        if (!inputFiles.Any())
        {
            return Result<WorkflowStepResult>.Failure("No input files found for de-identification");
        }
        
        // Create output directory
        var outputDir = Path.Combine(context.WorkingDirectory, "deidentified");
        Directory.CreateDirectory(outputDir);
        
        // Extract de-identification options
        var options = new DeIdentificationOptions
        {
            DateShift = TimeSpan.FromDays(30), // Default
            Salt = step.Parameters.GetValueOrDefault("salt", "workflow")?.ToString() ?? "workflow"
        };
        
        var deidentifiedFiles = new List<string>();
        var processedCount = 0;
        
        foreach (var inputFile in inputFiles)
        {
            var outputFile = Path.Combine(outputDir, $"deident_{Path.GetFileName(inputFile)}");
            
            var result = await _deIdentificationEngine.ProcessFileAsync(inputFile, outputFile, options);
            if (result.IsSuccess)
            {
                deidentifiedFiles.Add(outputFile);
                processedCount++;
            }
        }
        
        return Result<WorkflowStepResult>.Success(stepResult with
        {
            Status = WorkflowStepStatus.Success,
            EndTime = DateTime.UtcNow,
            Output = new Dictionary<string, object>
            {
                ["inputFiles"] = inputFiles.Count,
                ["processedFiles"] = processedCount,
                ["outputDirectory"] = outputDir,
                ["deidentifiedFiles"] = deidentifiedFiles
            }
        });
    }

    private async Task<Result<WorkflowStepResult>> ExecuteConfigureVendorStepAsync(
        WorkflowStep step, 
        WorkflowExecutionContext context, 
        WorkflowStepResult stepResult, 
        CancellationToken cancellationToken)
    {
        // Get input files from previous steps or parameters
        var inputFiles = GetInputFiles(step, context);
        if (!inputFiles.Any())
        {
            return Result<WorkflowStepResult>.Failure("No input files found for vendor analysis");
        }
        
        // Load messages for analysis
        var messages = new List<string>();
        foreach (var filePath in inputFiles)
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            messages.Add(content);
        }
        
        // Analyze vendor patterns
        var options = new InferenceOptions
        {
            MinimumConfidence = 0.6
        };
        
        var result = await _vendorDetectionService.AnalyzeVendorPatternsAsync(messages, options);
        if (result.IsFailure)
        {
            return Result<WorkflowStepResult>.Failure($"Vendor analysis failed: {result.Error}");
        }
        
        var vendorConfig = result.Value;
        
        // Save configuration
        var configFile = Path.Combine(context.WorkingDirectory, "vendor_config.json");
        var configJson = System.Text.Json.JsonSerializer.Serialize(vendorConfig, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(configFile, configJson, cancellationToken);
        
        return Result<WorkflowStepResult>.Success(stepResult with
        {
            Status = WorkflowStepStatus.Success,
            EndTime = DateTime.UtcNow,
            Output = new Dictionary<string, object>
            {
                ["vendorConfiguration"] = vendorConfig,
                ["configurationFile"] = configFile,
                ["messagesAnalyzed"] = messages.Count,
                ["detectedVendor"] = vendorConfig.Address.Vendor,
                ["confidence"] = vendorConfig.Metadata.Confidence
            }
        });
    }

    private async Task<Result<WorkflowStepResult>> ExecuteCompareStepAsync(
        WorkflowStep step, 
        WorkflowExecutionContext context, 
        WorkflowStepResult stepResult, 
        CancellationToken cancellationToken)
    {
        // Extract comparison parameters
        var leftPath = step.Parameters.GetValueOrDefault("leftPath")?.ToString();
        var rightPath = step.Parameters.GetValueOrDefault("rightPath")?.ToString();
        
        if (string.IsNullOrEmpty(leftPath) || string.IsNullOrEmpty(rightPath))
        {
            return Result<WorkflowStepResult>.Failure("Compare step requires leftPath and rightPath parameters");
        }
        
        // Resolve paths relative to working directory if needed
        if (!Path.IsPathFullyQualified(leftPath))
            leftPath = Path.Combine(context.WorkingDirectory, leftPath);
        if (!Path.IsPathFullyQualified(rightPath))
            rightPath = Path.Combine(context.WorkingDirectory, rightPath);
        
        // Read content for comparison
        var leftContent = await File.ReadAllTextAsync(leftPath, cancellationToken);
        var rightContent = await File.ReadAllTextAsync(rightPath, cancellationToken);
        
        // Perform basic line-by-line comparison
        var areEqual = string.Equals(leftContent, rightContent, StringComparison.Ordinal);
        var differences = new List<string>();
        
        if (!areEqual)
        {
            var leftLines = leftContent.Split('\n');
            var rightLines = rightContent.Split('\n');
            var maxLines = Math.Max(leftLines.Length, rightLines.Length);
            
            for (int i = 0; i < maxLines; i++)
            {
                var leftLine = i < leftLines.Length ? leftLines[i] : "";
                var rightLine = i < rightLines.Length ? rightLines[i] : "";
                
                if (!string.Equals(leftLine, rightLine, StringComparison.Ordinal))
                {
                    differences.Add($"Line {i + 1}: '{leftLine.Trim()}' vs '{rightLine.Trim()}'");
                }
            }
        }
        
        return Result<WorkflowStepResult>.Success(stepResult with
        {
            Status = WorkflowStepStatus.Success,
            EndTime = DateTime.UtcNow,
            Output = new Dictionary<string, object>
            {
                ["leftFile"] = leftPath,
                ["rightFile"] = rightPath,
                ["areEqual"] = areEqual,
                ["differenceCount"] = differences.Count,
                ["differences"] = differences.Take(10).ToList()
            }
        });
    }

    private async Task<Result<WorkflowStepResult>> ExecuteTransformStepAsync(
        WorkflowStep step, 
        WorkflowExecutionContext context, 
        WorkflowStepResult stepResult, 
        CancellationToken cancellationToken)
    {
        // TODO: Implement transformation logic
        await Task.Delay(100, cancellationToken); // Simulate work
        
        return Result<WorkflowStepResult>.Success(stepResult with
        {
            Status = WorkflowStepStatus.Success,
            EndTime = DateTime.UtcNow,
            Output = new Dictionary<string, object>
            {
                ["message"] = "Transform step completed (placeholder implementation)"
            }
        });
    }

    private async Task<Result<WorkflowStepResult>> ExecuteCustomStepAsync(
        WorkflowStep step, 
        WorkflowExecutionContext context, 
        WorkflowStepResult stepResult, 
        CancellationToken cancellationToken)
    {
        // TODO: Implement custom step execution
        await Task.Delay(100, cancellationToken); // Simulate work
        
        return Result<WorkflowStepResult>.Success(stepResult with
        {
            Status = WorkflowStepStatus.Success,
            EndTime = DateTime.UtcNow,
            Output = new Dictionary<string, object>
            {
                ["message"] = "Custom step completed (placeholder implementation)"
            }
        });
    }

    public async Task<Result<WorkflowValidationSummary>> ValidateWorkflowAsync(WorkflowScenario scenario)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var missingDependencies = new List<string>();
        
        // Validate basic scenario structure
        if (string.IsNullOrWhiteSpace(scenario.Name))
        {
            errors.Add("Workflow name is required");
        }
        
        if (!scenario.Steps.Any())
        {
            errors.Add("Workflow must have at least one step");
        }
        
        // Validate step dependencies
        var stepIds = scenario.Steps.Select(s => s.Id).ToHashSet();
        foreach (var step in scenario.Steps)
        {
            foreach (var dependency in step.InputDependencies)
            {
                if (!stepIds.Contains(dependency))
                {
                    errors.Add($"Step '{step.Name}' references unknown dependency: {dependency}");
                }
            }
        }
        
        // Check for circular dependencies
        if (HasCircularDependencies(scenario.Steps))
        {
            errors.Add("Workflow contains circular dependencies");
        }
        
        var canExecute = !errors.Any();
        var estimatedDuration = scenario.Steps.Sum(s => s.EstimatedDuration.TotalMinutes);
        
        await Task.CompletedTask; // Make async
        
        return Result<WorkflowValidationSummary>.Success(new WorkflowValidationSummary
        {
            CanExecute = canExecute,
            BlockingErrors = errors,
            Warnings = warnings,
            MissingDependencies = missingDependencies,
            EstimatedDuration = TimeSpan.FromMinutes(estimatedDuration)
        });
    }

    public async Task<Result<WorkflowExecution>> GetExecutionStatusAsync(string executionId)
    {
        await Task.CompletedTask; // Make async
        
        if (!_runningExecutions.TryGetValue(executionId, out var execution))
        {
            return Result<WorkflowExecution>.Failure($"Execution not found: {executionId}");
        }
        
        return Result<WorkflowExecution>.Success(execution);
    }

    public async Task<Result<bool>> CancelExecutionAsync(string executionId)
    {
        await Task.CompletedTask; // Make async
        
        if (!_runningExecutions.TryGetValue(executionId, out var execution))
        {
            return Result<bool>.Failure($"Execution not found: {executionId}");
        }
        
        if (execution.Status != WorkflowExecutionStatus.Running)
        {
            return Result<bool>.Failure($"Execution is not running: {execution.Status}");
        }
        
        // Update status to cancelled
        var cancelledExecution = execution with
        {
            Status = WorkflowExecutionStatus.Cancelled,
            EndTime = DateTime.UtcNow
        };
        
        _runningExecutions[executionId] = cancelledExecution;
        
        return Result<bool>.Success(true);
    }

    private List<string> GetInputFiles(WorkflowStep step, WorkflowExecutionContext context)
    {
        var inputFiles = new List<string>();
        
        // Level 1: Explicit input parameter (highest priority)
        if (step.Parameters.TryGetValue("inputFiles", out var inputParam))
        {
            if (inputParam is List<string> files)
            {
                inputFiles.AddRange(files);
            }
            else if (inputParam is string singleFile)
            {
                inputFiles.Add(singleFile);
            }
        }
        
        // Level 2: Explicit dependency outputs (when properly configured)
        foreach (var dependency in step.InputDependencies)
        {
            if (context.StepOutputs.TryGetValue(dependency, out var outputs))
            {
                AddFilesFromStepOutputs(inputFiles, outputs);
            }
        }
        
        // Level 3: Order-based fallback (80% of workflows - sequential steps)
        if (!inputFiles.Any() && step.Order > 1)
        {
            var previousStepOutputs = GetPreviousStepOutputsByOrder(step, context);
            if (previousStepOutputs != null)
            {
                AddFilesFromStepOutputs(inputFiles, previousStepOutputs);
                _logger.LogInformation("Using order-based dependency fallback for step '{StepName}' (order {Order})", 
                    step.Name, step.Order);
            }
        }
        
        // Level 4: Semantic dependencies (complex scenarios)
        if (!inputFiles.Any())
        {
            var semanticOutputs = GetSemanticDependencyOutputs(step, context);
            if (semanticOutputs != null)
            {
                AddFilesFromStepOutputs(inputFiles, semanticOutputs);
                _logger.LogInformation("Using semantic dependency fallback for step '{StepName}' (type {StepType})", 
                    step.Name, step.StepType);
            }
        }
        
        return inputFiles.Distinct().Where(File.Exists).ToList();
    }
    
    private void AddFilesFromStepOutputs(List<string> inputFiles, Dictionary<string, object> outputs)
    {
        if (outputs.TryGetValue("generatedFiles", out var generatedFiles) && generatedFiles is List<string> files)
        {
            inputFiles.AddRange(files);
        }
        if (outputs.TryGetValue("deidentifiedFiles", out var deidentFiles) && deidentFiles is List<string> deidentifiedFiles)
        {
            inputFiles.AddRange(deidentifiedFiles);
        }
        if (outputs.TryGetValue("outputDirectory", out var outputDir) && outputDir is string directory)
        {
            if (Directory.Exists(directory))
            {
                inputFiles.AddRange(Directory.GetFiles(directory, "*", SearchOption.AllDirectories));
            }
        }
    }
    
    private Dictionary<string, object>? GetPreviousStepOutputsByOrder(WorkflowStep currentStep, WorkflowExecutionContext context)
    {
        // Since context only has StepOutputs, we assume any step with outputs has completed
        // Find the most recent output from a step with lower order (based on step execution order)
        var availableOutputs = context.StepOutputs
            .Where(kvp => !string.IsNullOrEmpty(kvp.Key) && kvp.Value.Any())
            .ToList();
            
        // Simple fallback: return the most recently added output (last in execution)
        // This works for sequential workflows where previous step = most recent output
        return availableOutputs.LastOrDefault().Value;
    }
    
    private Dictionary<string, object>? GetSemanticDependencyOutputs(WorkflowStep currentStep, WorkflowExecutionContext context)
    {
        // For semantic fallback, look for any available outputs that make sense
        // This is a simple implementation - could be enhanced with step type matching
        var availableOutputs = context.StepOutputs
            .Where(kvp => !string.IsNullOrEmpty(kvp.Key) && kvp.Value.Any())
            .ToList();
            
        // Return any available output as fallback
        return availableOutputs.FirstOrDefault().Value;
    }

    private static string GetFileExtension(string standard)
    {
        return standard.ToLowerInvariant() switch
        {
            "hl7v2" => "hl7",
            "fhir" => "json",
            "ncpdp" => "txt",
            _ => "txt"
        };
    }

    private static bool HasCircularDependencies(List<WorkflowStep> steps)
    {
        // Simple cycle detection using DFS
        var graph = steps.ToDictionary(s => s.Id, s => s.InputDependencies);
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        
        foreach (var stepId in graph.Keys)
        {
            if (HasCycle(stepId, graph, visited, recursionStack))
            {
                return true;
            }
        }
        
        return false;
    }

    private static bool HasCycle(string stepId, Dictionary<string, List<string>> graph, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(stepId))
        {
            return true;
        }
        
        if (visited.Contains(stepId))
        {
            return false;
        }
        
        visited.Add(stepId);
        recursionStack.Add(stepId);
        
        if (graph.TryGetValue(stepId, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                if (HasCycle(dependency, graph, visited, recursionStack))
                {
                    return true;
                }
            }
        }
        
        recursionStack.Remove(stepId);
        return false;
    }

    private static double CalculateSuccessRate(List<WorkflowStepResult> stepResults)
    {
        if (!stepResults.Any()) return 0.0;
        
        var successCount = stepResults.Count(r => r.Status == WorkflowStepStatus.Success);
        return (double)successCount / stepResults.Count;
    }

    private async Task<Result<WorkflowExecution>> CompleteExecutionWithFailure(WorkflowExecution execution, string error)
    {
        var failedExecution = execution with
        {
            Status = WorkflowExecutionStatus.Failed,
            EndTime = DateTime.UtcNow,
            Errors = new List<string> { error },
            SuccessRate = 0.0
        };
        
        _runningExecutions[execution.Id] = failedExecution;
        
        await Task.CompletedTask;
        return Result<WorkflowExecution>.Failure(error);
    }

    private async Task<Result<WorkflowExecution>> CompleteExecutionWithCancellation(WorkflowExecution execution)
    {
        var cancelledExecution = execution with
        {
            Status = WorkflowExecutionStatus.Cancelled,
            EndTime = DateTime.UtcNow
        };
        
        _runningExecutions[execution.Id] = cancelledExecution;
        
        await Task.CompletedTask;
        return Result<WorkflowExecution>.Failure("Workflow execution was cancelled");
    }
}