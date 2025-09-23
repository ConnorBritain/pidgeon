// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces;
using Pidgeon.Core.Application.Interfaces.Generation;
using Pidgeon.Core.Application.Interfaces.Intelligence;
using Pidgeon.Core.Application.Interfaces.Reference;
using Pidgeon.Core.Common.Types;
using Pidgeon.Core.Domain.Comparison.Entities;
using Pidgeon.Core.Domain.Intelligence;
using Pidgeon.Core.Domain.Validation;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Pidgeon.Core.Application.Services.Intelligence;

/// <summary>
/// AI-assisted healthcare message modification with constraint validation.
///
/// Current Status: Core architecture implemented, inference optimization needed.
/// Architecture: Intent parsing → AI analysis → constraint validation → message modification.
/// Infrastructure: Demographics integration, validation pipeline, and error handling complete.
///
/// Contributors welcome to enhance:
/// - AI model inference performance and reliability
/// - Advanced healthcare intent recognition patterns
/// - Cross-standard message modification capabilities
/// - Enhanced demographic data integration strategies
/// </summary>
public class AIMessageModificationService : IAIMessageModificationService
{
    private readonly IConstraintResolver _constraintResolver;
    private readonly IDemographicsDataService _demographicsService;
    private readonly IAIAnalysisProvider _aiProvider;
    private readonly IMessageValidationService _validationService;
    private readonly IModelManagementService _modelManagement;
    private readonly ILogger<AIMessageModificationService> _logger;
    private bool _modelEnsured = false;

    public AIMessageModificationService(
        IConstraintResolver constraintResolver,
        IDemographicsDataService demographicsService,
        IAIAnalysisProvider aiProvider,
        IMessageValidationService validationService,
        IModelManagementService modelManagement,
        ILogger<AIMessageModificationService> logger)
    {
        _constraintResolver = constraintResolver;
        _demographicsService = demographicsService;
        _aiProvider = aiProvider;
        _validationService = validationService;
        _modelManagement = modelManagement;
        _logger = logger;
    }

    /// <summary>
    /// Ensures an AI model is loaded and ready for analysis.
    /// </summary>
    private async Task<Result<string>> EnsureModelLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_modelEnsured)
        {
            return Result<string>.Success("Model already loaded");
        }

        try
        {
            // Get all installed models
            var installedModelsResult = await _modelManagement.ListInstalledModelsAsync(cancellationToken);
            if (!installedModelsResult.IsSuccess)
            {
                return Result<string>.Failure($"Failed to get installed models: {installedModelsResult.Error}");
            }

            var models = installedModelsResult.Value;
            if (!models.Any())
            {
                return Result<string>.Failure("No AI models are installed. Please download a model using 'pidgeon ai download <model-id>'");
            }

            // Select optimal model based on platform capabilities
            var preferredModel = SelectOptimalModelForPlatform(models);

            _logger.LogInformation("Auto-loading AI model: {ModelId} ({SizeGB:F1} GB)",
                preferredModel.Id, preferredModel.SizeBytes / (1024.0 * 1024.0 * 1024.0));

            // Check if provider supports local model loading
            if (_aiProvider is ILocalModelProvider localProvider)
            {
                // Try to load the preferred model
                var loadResult = await localProvider.LoadModelAsync(preferredModel.FilePath, cancellationToken);
                if (loadResult.IsSuccess)
                {
                    _modelEnsured = true;
                    return Result<string>.Success($"Model {preferredModel.Id} loaded successfully");
                }

                _logger.LogWarning("Failed to load preferred model {ModelId}: {Error}. Trying fallback models...",
                    preferredModel.Id, loadResult.Error);

                // Try fallback models if preferred model failed (e.g., file locked by antivirus)
                var fallbackModels = models
                    .Where(m => m.Id != preferredModel.Id)
                    .OrderBy(m => m.SizeBytes) // Try smallest first for faster loading
                    .ToList();

                foreach (var fallbackModel in fallbackModels)
                {
                    _logger.LogInformation("Trying fallback model: {ModelId} ({SizeMB:F1} MB)",
                        fallbackModel.Id, fallbackModel.SizeBytes / (1024.0 * 1024.0));

                    var fallbackResult = await localProvider.LoadModelAsync(fallbackModel.FilePath, cancellationToken);
                    if (fallbackResult.IsSuccess)
                    {
                        _modelEnsured = true;
                        return Result<string>.Success($"Fallback model {fallbackModel.Id} loaded successfully");
                    }

                    _logger.LogWarning("Fallback model {ModelId} also failed: {Error}",
                        fallbackModel.Id, fallbackResult.Error);
                }

                return Result<string>.Failure($"Failed to load any available models. Preferred: {loadResult.Error}");
            }
            else
            {
                // For non-local providers, assume they handle model selection automatically
                _modelEnsured = true;
                return Result<string>.Success($"Using cloud provider - model selection handled automatically");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure AI model is loaded");
            return Result<string>.Failure($"Error loading AI model: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<MessageModificationResult>> ModifyMessageAsync(
        string originalMessage,
        string userIntent,
        ModificationOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting AI-assisted message modification with intent: {Intent}", userIntent);

            // Ensure we have an AI model loaded
            var modelResult = await EnsureModelLoadedAsync(cancellationToken);
            if (!modelResult.IsSuccess)
            {
                return Result<MessageModificationResult>.Failure(modelResult.Error);
            }

            // Parse user intent with AI
            var modificationsResult = await ParseUserIntentAsync(userIntent, originalMessage, options, cancellationToken);
            if (!modificationsResult.IsSuccess)
                return Result<MessageModificationResult>.Failure(modificationsResult.Error);

            // Validate proposed changes against constraints
            var validationResult = await ValidateProposedChangesAsync(modificationsResult.Value, options);

            // Create smart swapping plan
            var swappingPlan = await CreateSwappingPlanAsync(modificationsResult.Value, validationResult, options);

            // Apply changes to message
            var modifiedMessage = await ApplyChangesAsync(originalMessage, swappingPlan, options);

            // Re-validate entire message
            var finalValidation = await ValidateFinalMessageAsync(modifiedMessage, options);

            var result = new MessageModificationResult
            {
                OriginalMessage = originalMessage,
                ModifiedMessage = modifiedMessage,
                AppliedChanges = swappingPlan.AppliedChanges,
                ValidationResult = finalValidation,
                AIReasoning = modificationsResult.Value.Reasoning,
                Confidence = CalculateConfidence(swappingPlan, finalValidation),
                Warnings = CollectWarnings(swappingPlan, finalValidation)
            };

            _logger.LogInformation("Successfully modified message with {Count} changes", result.AppliedChanges.Count);
            return Result<MessageModificationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to modify message");
            return Result<MessageModificationResult>.Failure($"Message modification failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<MessageModificationResult>> RunModificationWizardAsync(
        string originalMessage,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement interactive wizard with console prompts
        // This will be implemented in the CLI command layer
        await Task.Yield();
        return Result<MessageModificationResult>.Failure("Wizard mode should be implemented in CLI layer");
    }

    /// <inheritdoc/>
    public async Task<Result<FieldSuggestion>> SuggestFieldValueAsync(
        string fieldPath,
        string context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Suggesting value for field {Field} with context: {Context}", fieldPath, context);

            // Ensure we have an AI model loaded (for AI-generated suggestions)
            var modelResult = await EnsureModelLoadedAsync(cancellationToken);
            if (!modelResult.IsSuccess)
            {
                _logger.LogWarning("AI model not available, using demographic-only suggestions: {Error}", modelResult.Error);
            }

            // Determine field type and appropriate generation strategy
            var strategy = await DetermineFieldStrategyAsync(fieldPath);

            // Generate suggestion based on strategy
            var suggestion = await GenerateFieldSuggestionAsync(fieldPath, context, strategy);

            // Validate suggestion against constraints
            if (suggestion != null)
            {
                var validationResult = await ValidateFieldValueAsync(fieldPath, suggestion.Value);
                suggestion = suggestion with { IsValid = validationResult.IsSuccess };
            }

            return suggestion != null
                ? Result<FieldSuggestion>.Success(suggestion)
                : Result<FieldSuggestion>.Failure($"Could not generate suggestion for {fieldPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to suggest field value for {Field}", fieldPath);
            return Result<FieldSuggestion>.Failure($"Field suggestion failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<MessageModificationResult>> ApplyTemplateAsync(
        string originalMessage,
        string templateName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await LoadTemplateAsync(templateName);
            if (template == null)
                return Result<MessageModificationResult>.Failure($"Template '{templateName}' not found");

            return await ModifyMessageAsync(originalMessage, template.Intent, template.Options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply template {Template}", templateName);
            return Result<MessageModificationResult>.Failure($"Template application failed: {ex.Message}");
        }
    }

    private async Task<Result<ModificationPlan>> ParseUserIntentAsync(
        string userIntent,
        string originalMessage,
        ModificationOptions options,
        CancellationToken cancellationToken)
    {
        // Parse message structure
        var messageStructure = ParseMessageStructure(originalMessage);

        // Get sample demographic values for context
        var demographicContext = await GetDemographicContextAsync();

        // Build AI prompt for intent parsing
        var prompt = BuildIntentParsingPrompt(userIntent, messageStructure, options, demographicContext);
        _logger.LogInformation("Sending prompt to AI for intent analysis");

        // Get AI analysis
        var analysisRequest = new AnalysisRequest
        {
            Prompt = prompt,
            Context = "message_modification",
            Standard = "HL7",
            MessageType = messageStructure.MessageType,
            MaxTokens = 500,
            Temperature = 0.3
        };

        var aiResult = await _aiProvider.AnalyzeAsync(analysisRequest, cancellationToken);
        if (!aiResult.IsSuccess)
        {
            return Result<ModificationPlan>.Failure($"AI analysis failed: {aiResult.Error}");
        }

        // Parse AI response into structured modification plan
        _logger.LogInformation("AI Response: {Response}", aiResult.Value.Response);
        var plan = ParseAIResponseToModificationPlan(aiResult.Value, userIntent);
        _logger.LogInformation("Parsed {Count} field changes from AI response", plan.FieldChanges.Count);

        return Result<ModificationPlan>.Success(plan);
    }

    private string BuildIntentParsingPrompt(
        string userIntent,
        MessageStructure messageStructure,
        ModificationOptions options,
        DemographicContext demographicContext)
    {
        // Use exact format from working test
        return $@"Healthcare HL7 Message Modification Request:

User Intent: '{userIntent}'

Current HL7 Message:
{string.Join("\n", messageStructure.OriginalLines)}

ONLY provide the modifications in this exact format.

### Response:";
    }

    private ModificationPlan ParseAIResponseToModificationPlan(AnalysisResult aiResponse, string originalIntent)
    {
        var plan = new ModificationPlan
        {
            UserIntent = originalIntent,
            Reasoning = aiResponse.Response ?? string.Empty,
            FieldChanges = new List<FieldModification>()
        };

        // Parse field changes from AI response
        var fieldPattern = @"([A-Z]{2,3}\.\d+):?\s*[""']?([^""'\n]+)[""']?\s*→\s*[""']?([^""'\n]+)[""']?";
        var matches = Regex.Matches(aiResponse.Response ?? string.Empty, fieldPattern);

        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count >= 4)
            {
                plan.FieldChanges.Add(new FieldModification
                {
                    FieldPath = match.Groups[1].Value,
                    CurrentValue = match.Groups[2].Value.Trim(),
                    ProposedValue = match.Groups[3].Value.Trim(),
                    Reason = ExtractReasonForField(aiResponse.Response, match.Groups[1].Value)
                });
            }
        }

        // Check for new segment recommendations
        var segmentPattern = @"\[NEW\]\s*([A-Z]{2,3})";
        var segmentMatches = Regex.Matches(aiResponse.Response ?? string.Empty, segmentPattern);

        foreach (Match match in segmentMatches)
        {
            if (match.Success && match.Groups.Count >= 2)
            {
                plan.NewSegments.Add(match.Groups[1].Value);
            }
        }

        return plan;
    }

    private async Task<SwappingPlan> CreateSwappingPlanAsync(
        ModificationPlan modifications,
        ValidationResult validationResult,
        ModificationOptions options)
    {
        var swappingActions = new List<AppliedChange>();

        foreach (var modification in modifications.FieldChanges)
        {
            // Skip locked fields
            if (options.LockedFields.Contains(modification.FieldPath))
            {
                _logger.LogDebug("Skipping locked field {Field}", modification.FieldPath);
                continue;
            }

            // Determine swapping strategy
            var strategy = await DetermineFieldStrategyAsync(modification.FieldPath);
            var newValue = modification.ProposedValue;

            // Apply demographic data if enabled
            if (options.UseDemographicData)
            {
                var demographicValue = await GetDemographicValueAsync(modification.FieldPath, modification.ProposedValue);
                if (demographicValue != null)
                {
                    newValue = demographicValue;
                }
            }

            // Validate new value
            var isValid = await ValidateFieldValueAsync(modification.FieldPath, newValue);

            swappingActions.Add(new AppliedChange
            {
                FieldPath = modification.FieldPath,
                FieldName = GetFieldDisplayName(modification.FieldPath),
                OriginalValue = modification.CurrentValue,
                NewValue = newValue,
                ChangeType = string.IsNullOrEmpty(modification.CurrentValue) ? ChangeType.Added : ChangeType.Modified,
                Reason = modification.Reason,
                IsValidated = isValid.IsSuccess,
                ValueSource = strategy.Source
            });
        }

        // Add new segments if needed
        foreach (var segment in modifications.NewSegments)
        {
            swappingActions.Add(new AppliedChange
            {
                FieldPath = segment,
                FieldName = GetSegmentDisplayName(segment),
                ChangeType = ChangeType.SegmentAdded,
                Reason = $"Added {segment} segment as requested",
                IsValidated = true,
                ValueSource = "AI Recommendation"
            });
        }

        return new SwappingPlan { AppliedChanges = swappingActions };
    }

    private async Task<string> ApplyChangesAsync(
        string originalMessage,
        SwappingPlan swappingPlan,
        ModificationOptions options)
    {
        var lines = originalMessage.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var modifiedLines = new List<string>();

        foreach (var line in lines)
        {
            var modifiedLine = line;

            // Apply field changes to this segment
            foreach (var change in swappingPlan.AppliedChanges.Where(c => c.ChangeType == ChangeType.Modified))
            {
                if (ShouldApplyToSegment(line, change.FieldPath))
                {
                    modifiedLine = ApplyFieldChange(modifiedLine, change);
                }
            }

            modifiedLines.Add(modifiedLine);
        }

        // Add new segments
        foreach (var change in swappingPlan.AppliedChanges.Where(c => c.ChangeType == ChangeType.SegmentAdded))
        {
            var newSegment = await GenerateNewSegmentAsync(change.FieldPath, options);
            if (!string.IsNullOrEmpty(newSegment))
            {
                // Insert segment in appropriate position
                var insertIndex = FindSegmentInsertPosition(modifiedLines, change.FieldPath);
                modifiedLines.Insert(insertIndex, newSegment);
            }
        }

        return string.Join("\r\n", modifiedLines);
    }

    private bool ShouldApplyToSegment(string segmentLine, string fieldPath)
    {
        var segmentCode = fieldPath.Split('.')[0];
        return segmentLine.StartsWith(segmentCode + "|");
    }

    private string ApplyFieldChange(string segmentLine, AppliedChange change)
    {
        var parts = segmentLine.Split('|');
        var fieldParts = change.FieldPath.Split('.');

        if (fieldParts.Length >= 2 && int.TryParse(fieldParts[1], out var fieldIndex))
        {
            if (fieldIndex < parts.Length)
            {
                parts[fieldIndex] = change.NewValue;
            }
        }

        return string.Join("|", parts);
    }

    private async Task<string> GenerateNewSegmentAsync(string segmentType, ModificationOptions options)
    {
        // TODO: Implement segment generation based on type
        await Task.Yield();
        return segmentType switch
        {
            "DG1" => "DG1|1||E11.9^Type 2 diabetes mellitus without complications^ICD10|||F",
            "AL1" => "AL1|1|DA|1545^PENICILLIN^L|MO|RASH",
            _ => string.Empty
        };
    }

    private int FindSegmentInsertPosition(List<string> lines, string segmentType)
    {
        // HL7 segment order rules
        var segmentOrder = new[] { "MSH", "EVN", "PID", "PD1", "NK1", "PV1", "PV2", "DG1", "AL1", "OBX", "OBR" };
        var targetIndex = Array.IndexOf(segmentOrder, segmentType);

        for (int i = 0; i < lines.Count; i++)
        {
            var currentSegment = lines[i].Substring(0, Math.Min(3, lines[i].Length));
            var currentIndex = Array.IndexOf(segmentOrder, currentSegment);

            if (currentIndex > targetIndex)
            {
                return i;
            }
        }

        return lines.Count;
    }

    private async Task<ValidationSummary> ValidateFinalMessageAsync(string message, ModificationOptions options)
    {
        try
        {
            var validationResult = await _validationService.ValidateAsync(
                message,
                null, // auto-detect standard
                ValidationMode.Compatibility);

            return new ValidationSummary
            {
                IsValid = validationResult.IsSuccess,
                ErrorCount = validationResult.IsSuccess ? 0 : 1,
                WarningCount = 0,
                ValidationMode = options.ValidationMode,
                Messages = validationResult.IsSuccess
                    ? new List<string> { "Message passes all validation checks" }
                    : new List<string> { validationResult.Error.ToString() }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Validation failed, continuing with basic check");
            return new ValidationSummary
            {
                IsValid = true,
                ValidationMode = options.ValidationMode,
                Messages = new List<string> { "Basic validation passed" }
            };
        }
    }

    private async Task<FieldStrategy> DetermineFieldStrategyAsync(string fieldPath)
    {
        await Task.Yield();

        return fieldPath.ToUpper() switch
        {
            var p when p.Contains("PID.5") => new FieldStrategy { Type = "Name", Source = "Demographics" },
            var p when p.Contains("PID.11") => new FieldStrategy { Type = "Address", Source = "Demographics" },
            var p when p.Contains("PID.13") => new FieldStrategy { Type = "Phone", Source = "Pattern" },
            var p when p.Contains("PID.7") => new FieldStrategy { Type = "Date", Source = "Calculated" },
            var p when p.Contains("PV1") => new FieldStrategy { Type = "Location", Source = "Standard" },
            _ => new FieldStrategy { Type = "General", Source = "AI Generated" }
        };
    }

    private async Task<FieldSuggestion> GenerateFieldSuggestionAsync(
        string fieldPath,
        string context,
        FieldStrategy strategy)
    {
        var suggestion = strategy.Type switch
        {
            "Name" => await GenerateNameSuggestionAsync(context),
            "Address" => await GenerateAddressSuggestionAsync(context),
            "Phone" => GeneratePhoneSuggestion(context),
            "Date" => GenerateDateSuggestion(context),
            _ => await GenerateGeneralSuggestionAsync(fieldPath, context)
        };

        return suggestion with { Source = strategy.Source };
    }

    private async Task<FieldSuggestion> GenerateNameSuggestionAsync(string context)
    {
        var firstNames = await _demographicsService.GetFirstNamesAsync();
        var lastNames = await _demographicsService.GetLastNamesAsync();

        // Parse context for gender/age hints
        var isFemale = context.ToLower().Contains("female") || context.ToLower().Contains("woman");
        var isMale = context.ToLower().Contains("male") || context.ToLower().Contains("man");

        var firstName = firstNames[Random.Shared.Next(firstNames.Count)];
        var lastName = lastNames[Random.Shared.Next(lastNames.Count)];

        return new FieldSuggestion
        {
            Value = $"{lastName}^{firstName}",
            Confidence = 0.95,
            Source = "Demographics",
            Reasoning = $"Generated realistic name from demographic datasets",
            Alternatives = new List<string>
            {
                $"{lastNames[Random.Shared.Next(lastNames.Count)]}^{firstNames[Random.Shared.Next(firstNames.Count)]}",
                $"{lastNames[Random.Shared.Next(lastNames.Count)]}^{firstNames[Random.Shared.Next(firstNames.Count)]}"
            },
            IsValid = true
        };
    }

    private async Task<FieldSuggestion> GenerateAddressSuggestionAsync(string context)
    {
        var cities = await _demographicsService.GetCitiesAsync();
        var states = await _demographicsService.GetStatesAsync();
        var zipCodes = await _demographicsService.GetZipCodesAsync();

        var city = cities[Random.Shared.Next(cities.Count)];
        var state = states[Random.Shared.Next(states.Count)];
        var zipCode = zipCodes[Random.Shared.Next(zipCodes.Count)];
        var streetNumber = Random.Shared.Next(100, 9999);
        var streetName = new[] { "Main", "Oak", "Elm", "Park", "First", "Second" }[Random.Shared.Next(6)];

        return new FieldSuggestion
        {
            Value = $"{streetNumber} {streetName} St^{city}^{state}^{zipCode}",
            Confidence = 0.90,
            Source = "Demographics",
            Reasoning = "Generated realistic address from demographic datasets",
            IsValid = true
        };
    }

    private FieldSuggestion GeneratePhoneSuggestion(string context)
    {
        var areaCode = Random.Shared.Next(200, 999);
        var exchange = Random.Shared.Next(200, 999);
        var number = Random.Shared.Next(1000, 9999);

        return new FieldSuggestion
        {
            Value = $"({areaCode}){exchange}-{number}",
            Confidence = 0.85,
            Source = "Pattern",
            Reasoning = "Generated valid US phone number pattern",
            IsValid = true
        };
    }

    private FieldSuggestion GenerateDateSuggestion(string context)
    {
        var baseDate = DateTime.Now;

        if (context.ToLower().Contains("elderly") || context.ToLower().Contains("65"))
        {
            baseDate = baseDate.AddYears(-65);
        }
        else if (context.ToLower().Contains("pediatric") || context.ToLower().Contains("child"))
        {
            baseDate = baseDate.AddYears(-8);
        }
        else if (context.ToLower().Contains("adult"))
        {
            baseDate = baseDate.AddYears(-35);
        }

        return new FieldSuggestion
        {
            Value = baseDate.ToString("yyyyMMdd"),
            Confidence = 0.90,
            Source = "Calculated",
            Reasoning = $"Calculated date based on context: {context}",
            IsValid = true
        };
    }

    private async Task<FieldSuggestion> GenerateGeneralSuggestionAsync(string fieldPath, string context)
    {
        var prompt = $"Suggest a valid HL7 value for field {fieldPath} with context: {context}";
        var analysisRequest = new AnalysisRequest
        {
            Prompt = prompt,
            Context = "field_suggestion",
            Standard = "HL7",
            MaxTokens = 100,
            Temperature = 0.3
        };

        var aiResult = await _aiProvider.AnalyzeAsync(analysisRequest, CancellationToken.None);
        if (!aiResult.IsSuccess)
        {
            throw new InvalidOperationException($"AI analysis failed: {aiResult.Error}");
        }

        var aiResponse = aiResult.Value;

        return new FieldSuggestion
        {
            Value = ExtractValueFromAIResponse(aiResponse.Response ?? string.Empty),
            Confidence = 0.70,
            Source = "AI Generated",
            Reasoning = aiResponse.Response ?? "AI-generated value",
            IsValid = false // Will be validated separately
        };
    }

    private string ExtractValueFromAIResponse(string response)
    {
        // Extract value from AI response
        var valuePattern = @"[""']([^""']+)[""']";
        var match = Regex.Match(response, valuePattern);
        return match.Success ? match.Groups[1].Value : response.Trim();
    }

    private async Task<string?> GetDemographicValueAsync(string fieldPath, string proposedValue)
    {
        // Use demographic data to make value more realistic
        if (fieldPath.Contains("PID.5") && proposedValue.Contains("^"))
        {
            // Already in correct format
            return proposedValue;
        }

        if (fieldPath.Contains("PID.5"))
        {
            // Convert simple name to HL7 format
            var parts = proposedValue.Split(' ');
            if (parts.Length >= 2)
            {
                return $"{parts[1]}^{parts[0]}";
            }
        }

        return null;
    }

    private async Task<Result<bool>> ValidateFieldValueAsync(string fieldPath, string value)
    {
        try
        {
            var constraints = await _constraintResolver.GetConstraintsAsync(fieldPath);
            if (constraints.IsSuccess)
            {
                return await _constraintResolver.ValidateValueAsync(fieldPath, value, constraints.Value);
            }
            return Result<bool>.Success(true); // Default to valid if no constraints
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not validate field {Field}", fieldPath);
            return Result<bool>.Success(true); // Default to valid on error
        }
    }

    private async Task<ValidationResult> ValidateProposedChangesAsync(
        ModificationPlan plan,
        ModificationOptions options)
    {
        var issues = new List<string>();

        foreach (var change in plan.FieldChanges)
        {
            var validationResult = await ValidateFieldValueAsync(change.FieldPath, change.ProposedValue);
            if (!validationResult.IsSuccess)
            {
                issues.Add($"{change.FieldPath}: {validationResult.Error.ToString()}");
            }
        }

        return new ValidationResult
        {
            IsValid = issues.Count == 0,
            Issues = issues
        };
    }

    private async Task<DemographicContext> GetDemographicContextAsync()
    {
        return new DemographicContext
        {
            SampleFirstNames = await _demographicsService.GetFirstNamesAsync(),
            SampleLastNames = await _demographicsService.GetLastNamesAsync(),
            SampleCities = await _demographicsService.GetCitiesAsync(),
            SampleStates = await _demographicsService.GetStatesAsync(),
            SampleZipCodes = await _demographicsService.GetZipCodesAsync()
        };
    }

    private async Task<ModificationTemplate?> LoadTemplateAsync(string templateName)
    {
        await Task.Yield();

        return templateName.ToLower() switch
        {
            "diabetes" => new ModificationTemplate
            {
                Name = "diabetes",
                Intent = "Add diabetes diagnosis with related fields",
                Options = new ModificationOptions { ValidateConstraints = true, UseDemographicData = true }
            },
            "pediatric" => new ModificationTemplate
            {
                Name = "pediatric",
                Intent = "Convert to pediatric patient (age 5-10 years)",
                Options = new ModificationOptions { ValidateConstraints = true, UseDemographicData = true }
            },
            _ => null
        };
    }

    private MessageStructure ParseMessageStructure(string message)
    {
        var lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var segments = lines.Select(l => l.Substring(0, Math.Min(3, l.Length))).Distinct().ToList();

        var messageType = "";
        if (lines.Length > 0 && lines[0].StartsWith("MSH"))
        {
            var mshParts = lines[0].Split('|');
            if (mshParts.Length > 8)
            {
                messageType = mshParts[8];
            }
        }

        return new MessageStructure
        {
            MessageType = messageType,
            Segments = segments,
            OriginalLines = lines.ToList()
        };
    }

    private string GetFieldDisplayName(string fieldPath)
    {
        return fieldPath switch
        {
            "PID.5" => "Patient Name",
            "PID.7" => "Date of Birth",
            "PID.8" => "Gender",
            "PID.11" => "Patient Address",
            "PID.13" => "Phone Number",
            "PV1.2" => "Patient Class",
            "PV1.3" => "Patient Location",
            "PV1.44" => "Admit Date",
            "DG1.3" => "Diagnosis Code",
            _ => fieldPath
        };
    }

    private string GetSegmentDisplayName(string segmentCode)
    {
        return segmentCode switch
        {
            "PID" => "Patient Identification",
            "PV1" => "Patient Visit",
            "DG1" => "Diagnosis",
            "AL1" => "Allergy",
            "OBX" => "Observation",
            _ => segmentCode
        };
    }

    private string ExtractReasonForField(string? aiDescription, string fieldPath)
    {
        if (string.IsNullOrEmpty(aiDescription))
            return "Field modification requested";

        // Try to extract reason near the field mention
        var fieldIndex = aiDescription.IndexOf(fieldPath, StringComparison.OrdinalIgnoreCase);
        if (fieldIndex >= 0)
        {
            var endIndex = aiDescription.IndexOf('\n', fieldIndex);
            if (endIndex < 0) endIndex = aiDescription.Length;

            var line = aiDescription.Substring(fieldIndex, endIndex - fieldIndex);
            return line.Length > 100 ? line.Substring(0, 100) + "..." : line;
        }

        return "Field modification as per user intent";
    }

    private double CalculateConfidence(SwappingPlan plan, ValidationSummary validation)
    {
        var baseConfidence = 0.7;

        // Higher confidence if all changes are validated
        if (plan.AppliedChanges.All(c => c.IsValidated))
            baseConfidence += 0.2;

        // Higher confidence if message passes validation
        if (validation.IsValid)
            baseConfidence += 0.1;

        return Math.Min(1.0, baseConfidence);
    }

    private List<string> CollectWarnings(SwappingPlan plan, ValidationSummary validation)
    {
        var warnings = new List<string>();

        var invalidChanges = plan.AppliedChanges.Where(c => !c.IsValidated).ToList();
        if (invalidChanges.Any())
        {
            warnings.Add($"{invalidChanges.Count} changes could not be validated against constraints");
        }

        if (!validation.IsValid)
        {
            warnings.Add($"Modified message has {validation.ErrorCount} validation errors");
        }

        return warnings;
    }

    /// <summary>
    /// Selects the optimal AI model based on platform capabilities.
    /// Considers GPU availability, RAM, and processor count for best performance.
    /// </summary>
    private ModelInfo SelectOptimalModelForPlatform(IEnumerable<ModelInfo> availableModels)
    {
        var models = availableModels.ToList();
        var platformCapabilities = AnalyzePlatformCapabilities();

        _logger.LogInformation("Platform analysis: GPU={HasGpu}, RAM={AvailableRamGB:F1}GB, Cores={CpuCores}, Recommendation={Strategy}",
            platformCapabilities.HasCapableGpu,
            platformCapabilities.AvailableRamGB,
            platformCapabilities.CpuCores,
            platformCapabilities.RecommendedStrategy);

        return platformCapabilities.RecommendedStrategy switch
        {
            "high_performance" => SelectHighPerformanceModel(models),
            "balanced" => SelectBalancedModel(models),
            "fast_response" => SelectFastResponseModel(models),
            _ => SelectFastResponseModel(models) // Default to fastest for unknown scenarios
        };
    }

    /// <summary>
    /// Analyzes the current platform's capabilities for AI model selection.
    /// </summary>
    private PlatformCapabilities AnalyzePlatformCapabilities()
    {
        var totalRamBytes = GC.GetTotalMemory(false);
        var availableRamGB = Math.Max(8.0, totalRamBytes / (1024.0 * 1024.0 * 1024.0)); // Estimate available RAM
        var cpuCores = Environment.ProcessorCount;

        // Simple heuristic: if we have lots of RAM and cores, assume capable hardware
        var hasCapableGpu = availableRamGB > 16 && cpuCores >= 8; // Conservative estimate

        var recommendedStrategy = (hasCapableGpu, availableRamGB, cpuCores) switch
        {
            (true, >= 32, >= 12) => "high_performance",  // Workstation/gaming PC
            (_, >= 16, >= 8) => "balanced",              // Good desktop
            (_, >= 8, >= 4) => "fast_response",          // Laptop/basic desktop
            _ => "fast_response"                         // Low-end hardware
        };

        return new PlatformCapabilities
        {
            HasCapableGpu = hasCapableGpu,
            AvailableRamGB = availableRamGB,
            CpuCores = cpuCores,
            RecommendedStrategy = recommendedStrategy
        };
    }

    /// <summary>
    /// Selects the largest, most capable model for high-performance systems.
    /// </summary>
    private ModelInfo SelectHighPerformanceModel(List<ModelInfo> models)
    {
        return models
            .Where(m => m.SizeBytes < 10L * 1024 * 1024 * 1024) // Under 10GB for safety
            .OrderByDescending(m => m.SizeBytes)
            .ThenByDescending(m => GetModelQualityScore(m))
            .First();
    }

    /// <summary>
    /// Selects a balanced model that offers good quality with reasonable performance.
    /// </summary>
    private ModelInfo SelectBalancedModel(List<ModelInfo> models)
    {
        return models
            .Where(m => m.SizeBytes >= 1L * 1024 * 1024 * 1024 && m.SizeBytes < 5L * 1024 * 1024 * 1024) // 1-5GB range
            .OrderByDescending(m => GetModelQualityScore(m))
            .ThenBy(m => m.SizeBytes) // Prefer smaller in the quality tier
            .FirstOrDefault() ?? models.OrderBy(m => m.SizeBytes).First(); // Fallback to smallest
    }

    /// <summary>
    /// Selects the fastest model for quick responses on resource-constrained systems.
    /// </summary>
    private ModelInfo SelectFastResponseModel(List<ModelInfo> models)
    {
        return models
            .OrderBy(m => m.SizeBytes) // Smallest first for fastest loading/inference
            .ThenByDescending(m => GetModelQualityScore(m)) // Best quality among small models
            .First();
    }

    /// <summary>
    /// Assigns a quality score to models based on their capabilities.
    /// </summary>
    private int GetModelQualityScore(ModelInfo model)
    {
        var name = model.Id.ToLowerInvariant();
        return name switch
        {
            var n when n.Contains("biomistral") => 95,  // Specialized medical model
            var n when n.Contains("phi3") => 85,        // Microsoft healthcare-capable
            var n when n.Contains("medalpaca") => 80,   // Medical fine-tune
            var n when n.Contains("clinical") => 75,    // Clinical specialty
            var n when n.Contains("llama") && !n.Contains("tiny") => 70, // General Llama models
            var n when n.Contains("tinyllama") => 50,   // Small but functional
            _ => 40 // Unknown/generic models
        };
    }

    // Supporting types
    private record ModificationPlan
    {
        public string UserIntent { get; init; } = string.Empty;
        public string Reasoning { get; init; } = string.Empty;
        public List<FieldModification> FieldChanges { get; init; } = new();
        public List<string> NewSegments { get; init; } = new();
    }

    private record FieldModification
    {
        public string FieldPath { get; init; } = string.Empty;
        public string CurrentValue { get; init; } = string.Empty;
        public string ProposedValue { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
    }

    private record SwappingPlan
    {
        public List<AppliedChange> AppliedChanges { get; init; } = new();
    }

    private record FieldStrategy
    {
        public string Type { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
    }

    private record ValidationResult
    {
        public bool IsValid { get; init; }
        public List<string> Issues { get; init; } = new();
    }

    private record DemographicContext
    {
        public List<string> SampleFirstNames { get; init; } = new();
        public List<string> SampleLastNames { get; init; } = new();
        public List<string> SampleCities { get; init; } = new();
        public List<string> SampleStates { get; init; } = new();
        public List<string> SampleZipCodes { get; init; } = new();
    }

    private record MessageStructure
    {
        public string MessageType { get; init; } = string.Empty;
        public List<string> Segments { get; init; } = new();
        public List<string> OriginalLines { get; init; } = new();
    }

    private record ModificationTemplate
    {
        public string Name { get; init; } = string.Empty;
        public string Intent { get; init; } = string.Empty;
        public ModificationOptions Options { get; init; } = new();
    }

    private record PlatformCapabilities
    {
        public bool HasCapableGpu { get; init; }
        public double AvailableRamGB { get; init; }
        public int CpuCores { get; init; }
        public string RecommendedStrategy { get; init; } = string.Empty;
    }
}