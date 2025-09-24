// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core;
using Pidgeon.Core.Application.Interfaces.Comparison;
using Pidgeon.Core.Application.Interfaces;
using Pidgeon.Core.Application.Interfaces.Intelligence;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Domain.Comparison.Entities;
using Pidgeon.Core.Domain.Intelligence;
using System.Text;

namespace Pidgeon.Core.Application.Services.Comparison;

/// <summary>
/// Service for comparing healthcare messages using algorithmic field-level analysis.
/// Enhanced with local AI models for deeper insights when available.
/// </summary>
public class MessageDiffService : IMessageDiffService
{
    private readonly ILogger<MessageDiffService> _logger;
    private readonly IMessageValidationService _validationService;
    private readonly IGenerationService _generationService;
    private readonly IAIAnalysisProvider? _aiProvider;
    private readonly IProceduralAnalysisEngine? _proceduralEngine;

    public MessageDiffService(
        ILogger<MessageDiffService> logger,
        IMessageValidationService validationService,
        IGenerationService generationService,
        IAIAnalysisProvider? aiProvider = null,
        IProceduralAnalysisEngine? proceduralEngine = null)
    {
        _logger = logger;
        _validationService = validationService;
        _aiProvider = aiProvider;
        _generationService = generationService;
        _proceduralEngine = proceduralEngine;
    }

    public async Task<Result<MessageDiff>> CompareMessagesAsync(
        string leftMessage,
        string rightMessage,
        DiffContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting message comparison: {LeftSource} vs {RightSource}", 
                context.LeftSource.Identifier, context.RightSource.Identifier);

            // Validate inputs
            var validationResult = await ValidateComparisonAsync(leftMessage, rightMessage, context);
            if (validationResult.IsFailure)
            {
                return Result<MessageDiff>.Failure($"Comparison validation failed: {validationResult.Error}");
            }

            // Detect message standards if not specified
            var leftStandard = await DetectMessageStandardAsync(leftMessage);
            var rightStandard = await DetectMessageStandardAsync(rightMessage);

            if (leftStandard.IsFailure || rightStandard.IsFailure)
            {
                return Result<MessageDiff>.Failure("Could not detect message standards for comparison");
            }

            // Create diff result
            var messageDiff = new MessageDiff
            {
                Context = context with
                {
                    LeftSource = context.LeftSource with { Standard = leftStandard.Value.Standard },
                    RightSource = context.RightSource with { Standard = rightStandard.Value.Standard }
                }
            };

            // Perform field-level comparison based on detected standard
            var fieldDifferences = await CompareMessageFieldsAsync(leftMessage, rightMessage, leftStandard.Value.Standard);
            if (fieldDifferences.IsFailure)
            {
                return Result<MessageDiff>.Failure($"Field comparison failed: {fieldDifferences.Error}");
            }

            // Calculate similarity score
            var similarityScore = CalculateSimilarityScore(fieldDifferences.Value);

            // Generate summary
            var summary = GenerateDiffSummary(fieldDifferences.Value);

            // Generate algorithmic insights
            var insights = await GenerateAlgorithmicInsightsAsync(fieldDifferences.Value, context);

            // Add procedural analysis insights if engine is available
            if (_proceduralEngine != null && fieldDifferences.Value.Any())
            {
                var proceduralResult = await _proceduralEngine.AnalyzeAsync(fieldDifferences.Value, context, cancellationToken);
                if (proceduralResult.IsSuccess)
                {
                    // Convert procedural analysis to insights
                    var proceduralInsights = ConvertProceduralAnalysisToInsights(proceduralResult.Value);
                    insights.AddRange(proceduralInsights);

                    _logger.LogInformation("Added procedural analysis with score {Score:F2} and {Count} recommendations",
                        proceduralResult.Value.ProceduralScore, proceduralResult.Value.Recommendations.Count);
                }
                else
                {
                    _logger.LogWarning("Procedural analysis failed: {Error}", proceduralResult.Error);
                }
            }
            
            // Enhance with AI insights if provider is available and enabled
            _logger.LogInformation("AI Analysis Check - EnableAIAnalysis: {EnableAI}, _aiProvider: {HasProvider}", 
                context.EnableAIAnalysis, _aiProvider != null);
                
            if (context.EnableAIAnalysis && _aiProvider != null)
            {
                _logger.LogInformation("Attempting AI insight generation with provider {ProviderId}", _aiProvider.ProviderId);
                
                var aiInsights = await GenerateAIEnhancedInsightsAsync(
                    leftMessage, 
                    rightMessage, 
                    fieldDifferences.Value, 
                    context);
                    
                if (aiInsights.IsSuccess)
                {
                    insights.AddRange(aiInsights.Value);
                    _logger.LogInformation("Added {Count} AI-generated insights", aiInsights.Value.Count);
                }
                else
                {
                    _logger.LogWarning("AI insight generation failed: {Error}", aiInsights.Error);
                }
            }
            else if (context.EnableAIAnalysis && _aiProvider == null)
            {
                _logger.LogWarning("AI analysis requested but no AI provider available - check service registration");
            }

            // Complete the diff result
            messageDiff = messageDiff with
            {
                FieldDifferences = fieldDifferences.Value,
                SimilarityScore = similarityScore,
                Summary = summary,
                Insights = insights
            };

            _logger.LogInformation("Message comparison completed: {DiffCount} differences found, {SimilarityScore:P} similarity", 
                fieldDifferences.Value.Count, similarityScore);

            return Result<MessageDiff>.Success(messageDiff);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during message comparison");
            return Result<MessageDiff>.Failure($"Message comparison error: {ex.Message}");
        }
    }

    public async Task<Result<MessageDiff>> CompareMessageFilesAsync(
        string leftFilePath,
        string rightFilePath,
        DiffContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Read file contents
            if (!File.Exists(leftFilePath))
            {
                return Result<MessageDiff>.Failure($"Left file not found: {leftFilePath}");
            }

            if (!File.Exists(rightFilePath))
            {
                return Result<MessageDiff>.Failure($"Right file not found: {rightFilePath}");
            }

            var leftContent = await File.ReadAllTextAsync(leftFilePath, cancellationToken);
            var rightContent = await File.ReadAllTextAsync(rightFilePath, cancellationToken);

            // Update context with file information
            var fileContext = context with
            {
                LeftSource = context.LeftSource with
                {
                    Identifier = leftFilePath,
                    SourceType = "file",
                    Size = new FileInfo(leftFilePath).Length,
                    LastModified = File.GetLastWriteTime(leftFilePath)
                },
                RightSource = context.RightSource with
                {
                    Identifier = rightFilePath,
                    SourceType = "file",
                    Size = new FileInfo(rightFilePath).Length,
                    LastModified = File.GetLastWriteTime(rightFilePath)
                }
            };

            return await CompareMessagesAsync(leftContent, rightContent, fileContext, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing message files: {LeftFile} vs {RightFile}", leftFilePath, rightFilePath);
            return Result<MessageDiff>.Failure($"File comparison error: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<MessageDiff>>> CompareDirectoriesAsync(
        string leftDirectory,
        string rightDirectory,
        DiffContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement directory comparison
        // This will be a future enhancement for comparing multiple files
        await Task.Yield();
        return Result<IEnumerable<MessageDiff>>.Failure("Directory comparison not yet implemented");
    }

    public async Task<Result<bool>> ValidateComparisonAsync(
        string leftMessage,
        string rightMessage,
        DiffContext context)
    {
        await Task.Yield();

        if (string.IsNullOrWhiteSpace(leftMessage))
        {
            return Result<bool>.Failure("Left message is empty or null");
        }

        if (string.IsNullOrWhiteSpace(rightMessage))
        {
            return Result<bool>.Failure("Right message is empty or null");
        }

        // TODO: Add more sophisticated validation
        // - Check if messages are in comparable formats
        // - Validate against known standards
        // - Check for minimum required fields

        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<string>>> GetSupportedStandardsAsync()
    {
        await Task.Yield();
        
        var supportedStandards = new List<string>
        {
            "HL7v2",
            "FHIR",
            "NCPDP"
        };

        return Result<IEnumerable<string>>.Success(supportedStandards);
    }

    public async Task<Result<(string Standard, string Version)>> DetectMessageStandardAsync(string messageContent)
    {
        await Task.Yield();

        if (string.IsNullOrWhiteSpace(messageContent))
        {
            return Result<(string, string)>.Failure("Message content is empty");
        }

        // Basic standard detection logic
        if (messageContent.StartsWith("MSH|"))
        {
            // HL7 v2.x detection
            var mshSegment = messageContent.Split('\r', '\n')[0];
            var fields = mshSegment.Split('|');
            if (fields.Length > 12)
            {
                var version = fields[12]?.Trim() ?? "2.3";
                return Result<(string, string)>.Success(("HL7v2", version));
            }
            return Result<(string, string)>.Success(("HL7v2", "2.3"));
        }
        
        if (messageContent.TrimStart().StartsWith("{"))
        {
            // Likely FHIR JSON
            if (messageContent.Contains("\"resourceType\""))
            {
                return Result<(string, string)>.Success(("FHIR", "R4"));
            }
        }

        if (messageContent.Contains("NCPDP"))
        {
            return Result<(string, string)>.Success(("NCPDP", "SCRIPT"));
        }

        return Result<(string, string)>.Failure("Could not detect message standard");
    }

    private async Task<Result<List<FieldDifference>>> CompareMessageFieldsAsync(
        string leftMessage, 
        string rightMessage, 
        string standard)
    {
        var differences = new List<FieldDifference>();

        try
        {
            if (standard == "HL7v2")
            {
                differences.AddRange(await CompareHL7FieldsAsync(leftMessage, rightMessage));
            }
            else if (standard == "FHIR")
            {
                differences.AddRange(await CompareFHIRFieldsAsync(leftMessage, rightMessage));
            }
            else
            {
                // Generic comparison for unknown standards
                differences.AddRange(await CompareGenericFieldsAsync(leftMessage, rightMessage));
            }

            return Result<List<FieldDifference>>.Success(differences);
        }
        catch (Exception ex)
        {
            return Result<List<FieldDifference>>.Failure($"Field comparison error: {ex.Message}");
        }
    }

    private async Task<List<FieldDifference>> CompareHL7FieldsAsync(string leftMessage, string rightMessage)
    {
        await Task.Yield();
        var differences = new List<FieldDifference>();

        // Parse HL7 messages into segments
        var leftSegments = ParseHL7Segments(leftMessage);
        var rightSegments = ParseHL7Segments(rightMessage);

        // Compare each segment type
        var allSegmentTypes = leftSegments.Keys.Union(rightSegments.Keys).ToList();

        foreach (var segmentType in allSegmentTypes)
        {
            var leftSegment = leftSegments.GetValueOrDefault(segmentType, new List<string>());
            var rightSegment = rightSegments.GetValueOrDefault(segmentType, new List<string>());

            differences.AddRange(CompareHL7Segment(segmentType, leftSegment, rightSegment));
        }

        return differences;
    }

    private Dictionary<string, List<string>> ParseHL7Segments(string message)
    {
        var segments = new Dictionary<string, List<string>>();
        var lines = message.Split('\r', '\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Length >= 3)
            {
                var segmentType = line.Substring(0, 3);
                if (!segments.ContainsKey(segmentType))
                {
                    segments[segmentType] = new List<string>();
                }
                segments[segmentType].Add(line);
            }
        }

        return segments;
    }

    private List<FieldDifference> CompareHL7Segment(string segmentType, List<string> leftSegments, List<string> rightSegments)
    {
        var differences = new List<FieldDifference>();

        if (!leftSegments.Any() && rightSegments.Any())
        {
            differences.Add(new FieldDifference
            {
                FieldPath = segmentType,
                FieldDescription = $"{segmentType} segment",
                LeftValue = null,
                RightValue = string.Join("|", rightSegments.First().Split('|').Take(5)) + "...",
                DifferenceType = DifferenceType.MissingInLeft,
                Severity = DifferenceSeverity.Warning,
                Standard = "HL7v2"
            });
        }
        else if (leftSegments.Any() && !rightSegments.Any())
        {
            differences.Add(new FieldDifference
            {
                FieldPath = segmentType,
                FieldDescription = $"{segmentType} segment", 
                LeftValue = string.Join("|", leftSegments.First().Split('|').Take(5)) + "...",
                RightValue = null,
                DifferenceType = DifferenceType.MissingInRight,
                Severity = DifferenceSeverity.Warning,
                Standard = "HL7v2"
            });
        }
        else if (leftSegments.Any() && rightSegments.Any())
        {
            // Compare field by field for the first occurrence
            var leftFields = leftSegments.First().Split('|');
            var rightFields = rightSegments.First().Split('|');
            var maxFields = Math.Max(leftFields.Length, rightFields.Length);

            for (int i = 0; i < maxFields; i++)
            {
                var leftValue = i < leftFields.Length ? leftFields[i] : null;
                var rightValue = i < rightFields.Length ? rightFields[i] : null;

                if (leftValue != rightValue)
                {
                    differences.Add(new FieldDifference
                    {
                        FieldPath = $"{segmentType}.{i + 1}",
                        FieldDescription = GetHL7FieldDescription(segmentType, i + 1),
                        LeftValue = leftValue,
                        RightValue = rightValue,
                        DifferenceType = leftValue == null ? DifferenceType.MissingInLeft :
                                       rightValue == null ? DifferenceType.MissingInRight :
                                       DifferenceType.ValueDifference,
                        Severity = GetHL7FieldSeverity(segmentType, i + 1),
                        Standard = "HL7v2",
                        IsRequired = IsHL7FieldRequired(segmentType, i + 1)
                    });
                }
            }
        }

        return differences;
    }

    private async Task<List<FieldDifference>> CompareFHIRFieldsAsync(string leftMessage, string rightMessage)
    {
        await Task.Yield();
        var differences = new List<FieldDifference>();

        // TODO: Implement FHIR-specific field comparison
        // This will parse JSON and compare FHIR resource fields
        
        differences.Add(new FieldDifference
        {
            FieldPath = "FHIR.comparison",
            FieldDescription = "FHIR comparison placeholder",
            LeftValue = "FHIR left",
            RightValue = "FHIR right", 
            DifferenceType = DifferenceType.ValueDifference,
            Severity = DifferenceSeverity.Info,
            Standard = "FHIR",
            AnalysisContext = "FHIR comparison not yet fully implemented"
        });

        return differences;
    }

    private async Task<List<FieldDifference>> CompareGenericFieldsAsync(string leftMessage, string rightMessage)
    {
        await Task.Yield();
        var differences = new List<FieldDifference>();

        if (leftMessage != rightMessage)
        {
            differences.Add(new FieldDifference
            {
                FieldPath = "message.content",
                FieldDescription = "Message content",
                LeftValue = leftMessage.Length > 100 ? leftMessage.Substring(0, 100) + "..." : leftMessage,
                RightValue = rightMessage.Length > 100 ? rightMessage.Substring(0, 100) + "..." : rightMessage,
                DifferenceType = DifferenceType.ValueDifference,
                Severity = DifferenceSeverity.Warning,
                Standard = "Generic"
            });
        }

        return differences;
    }

    private double CalculateSimilarityScore(List<FieldDifference> differences)
    {
        if (!differences.Any())
        {
            return 1.0; // Identical messages
        }

        // Simple similarity calculation - can be enhanced
        // Weight differences by severity
        var totalWeight = differences.Count * 1.0;
        var penaltyWeight = differences.Sum(d => d.Severity switch
        {
            DifferenceSeverity.Critical => 1.0,
            DifferenceSeverity.Warning => 0.7,
            DifferenceSeverity.Info => 0.3,
            _ => 0.5
        });

        return Math.Max(0.0, 1.0 - (penaltyWeight / Math.Max(totalWeight, 1.0)));
    }

    private DiffSummary GenerateDiffSummary(List<FieldDifference> differences)
    {
        return new DiffSummary
        {
            TotalDifferences = differences.Count,
            CriticalDifferences = differences.Count(d => d.Severity == DifferenceSeverity.Critical),
            WarningDifferences = differences.Count(d => d.Severity == DifferenceSeverity.Warning),
            InformationalDifferences = differences.Count(d => d.Severity == DifferenceSeverity.Info),
            FieldsOnlyInLeft = differences.Count(d => d.DifferenceType == DifferenceType.MissingInRight),
            FieldsOnlyInRight = differences.Count(d => d.DifferenceType == DifferenceType.MissingInLeft),
            FieldsWithDifferentValues = differences.Count(d => d.DifferenceType == DifferenceType.ValueDifference)
        };
    }

    private async Task<Result<List<AnalysisInsight>>> GenerateAIEnhancedInsightsAsync(
        string leftMessage,
        string rightMessage,
        List<FieldDifference> differences,
        DiffContext context)
    {
        if (_aiProvider == null)
        {
            return Result<List<AnalysisInsight>>.Failure("AI provider not available");
        }
        
        try
        {
            // Check if AI provider is available
            var isAvailable = await _aiProvider.IsAvailableAsync();
            if (!isAvailable)
            {
                return Result<List<AnalysisInsight>>.Failure("AI provider is not currently available");
            }
            
            // If this is a local model provider, ensure model is loaded
            if (_aiProvider is Core.Application.Interfaces.Intelligence.ILocalModelProvider localProvider)
            {
                var modelInfo = await localProvider.GetModelInfoAsync();
                if (modelInfo.IsFailure)
                {
                    // Try to load the first available model
                    var modelsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pidgeon", "models");
                    if (Directory.Exists(modelsPath))
                    {
                        var ggufFiles = Directory.GetFiles(modelsPath, "*.gguf");
                        if (ggufFiles.Length > 0)
                        {
                            _logger.LogInformation("Auto-loading AI model: {ModelPath}", ggufFiles[0]);
                            var loadResult = await localProvider.LoadModelAsync(ggufFiles[0]);
                            if (loadResult.IsFailure)
                            {
                                return Result<List<AnalysisInsight>>.Failure($"Failed to load AI model: {loadResult.Error}");
                            }
                        }
                        else
                        {
                            return Result<List<AnalysisInsight>>.Failure("No GGUF models found in ~/.pidgeon/models/");
                        }
                    }
                    else
                    {
                        return Result<List<AnalysisInsight>>.Failure("Models directory not found: ~/.pidgeon/models/");
                    }
                }
            }
            
            // Build comprehensive prompt for AI analysis
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("You are analyzing differences between two healthcare messages.");
            promptBuilder.AppendLine($"Standard: {context.Standard ?? "Unknown"}");
            promptBuilder.AppendLine($"Message Types: {context.LeftSource.MessageType} vs {context.RightSource.MessageType}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("FIELD DIFFERENCES:");
            
            foreach (var diff in differences.Take(10)) // Limit to top 10 for token efficiency
            {
                promptBuilder.AppendLine($"- {diff.FieldPath}: '{diff.LeftValue}' → '{diff.RightValue}' [{diff.Severity}]");
            }
            
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("TASK: Provide root cause analysis and recommendations for these differences.");
            promptBuilder.AppendLine("Focus on: 1) Why these differences might occur, 2) Clinical/operational impact, 3) How to resolve");
            
            // Create AI analysis request
            var analysisRequest = new AnalysisRequest
            {
                Prompt = promptBuilder.ToString(),
                Context = "healthcare_diff_analysis",
                MaxTokens = 500,
                Temperature = 0.3,
                Standard = context.Standard,
                MessageType = context.LeftSource.MessageType,
                Metadata = new Dictionary<string, object>
                {
                    ["difference_count"] = differences.Count,
                    ["critical_count"] = differences.Count(d => d.Severity == DifferenceSeverity.Critical),
                    ["vendor_left"] = context.LeftSource.VendorConfiguration?.ToString() ?? "Unknown",
                    ["vendor_right"] = context.RightSource.VendorConfiguration?.ToString() ?? "Unknown"
                }
            };
            
            // Get AI analysis
            var analysisResult = await _aiProvider.AnalyzeAsync(analysisRequest);
            if (analysisResult.IsFailure)
            {
                return Result<List<AnalysisInsight>>.Failure($"AI analysis failed: {analysisResult.Error}");
            }
            
            // Parse AI response into insights
            var aiInsights = ParseAIResponseToInsights(analysisResult.Value, differences);
            
            _logger.LogInformation("Generated {Count} AI insights using provider {ProviderId}", 
                aiInsights.Count, _aiProvider.ProviderId);
            
            return Result<List<AnalysisInsight>>.Success(aiInsights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI-enhanced insights");
            return Result<List<AnalysisInsight>>.Failure($"AI insight generation error: {ex.Message}");
        }
    }
    
    private List<AnalysisInsight> ParseAIResponseToInsights(AnalysisResult analysisResult, List<FieldDifference> differences)
    {
        var insights = new List<AnalysisInsight>();
        
        // Parse AI response into structured insights
        var aiText = analysisResult.Response;
        
        // Create a comprehensive root cause insight from AI analysis
        insights.Add(new AnalysisInsight
        {
            InsightType = InsightType.RootCauseAnalysis,
            Title = "AI Root Cause Analysis",
            Description = aiText,
            RelatedFields = differences.Select(d => d.FieldPath).Take(5).ToList(),
            Confidence = analysisResult.Confidence,
            Severity = DetermineAISeverity(differences),
            HealthcareCategory = HealthcareCategory.ClinicalData,
            RecommendedAction = ExtractRecommendations(aiText),
            AnalysisSource = new AnalysisSource
            {
                SourceType = AnalysisSourceType.LocalAI,
                EngineName = analysisResult.ModelId ?? "Unknown Model",
                Version = "1.0",
                IsLocal = true,
                ProcessingTime = analysisResult.ProcessingTime
            }
        });
        
        return insights;
    }
    
    private InsightSeverity DetermineAISeverity(List<FieldDifference> differences)
    {
        if (differences.Any(d => d.Severity == DifferenceSeverity.Critical))
            return InsightSeverity.High;
        if (differences.Any(d => d.Severity == DifferenceSeverity.Warning))
            return InsightSeverity.Medium;
        return InsightSeverity.Low;
    }
    
    private string ExtractRecommendations(string aiText)
    {
        // Simple extraction of recommendations from AI text
        var lines = aiText.Split('\n');
        var recommendations = lines
            .Where(l => l.Contains("recommend", StringComparison.OrdinalIgnoreCase) ||
                       l.Contains("should", StringComparison.OrdinalIgnoreCase) ||
                       l.Contains("suggest", StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();
            
        return recommendations.Any() 
            ? string.Join("; ", recommendations) 
            : "Review field differences and update vendor configuration as needed";
    }
    
    private async Task<List<AnalysisInsight>> GenerateAlgorithmicInsightsAsync(
        List<FieldDifference> differences, 
        DiffContext context)
    {
        await Task.Yield();
        var insights = new List<AnalysisInsight>();

        // Generate pattern-based insights
        if (differences.Any(d => d.FieldPath.Contains("MSH.7")))
        {
            insights.Add(new AnalysisInsight
            {
                InsightType = InsightType.PatternRecognition,
                Title = "Timestamp Difference Detected",
                Description = "MSH.7 (Date/Time of Message) differs between messages. This is common and usually not problematic unless messages should be identical.",
                RelatedFields = differences.Where(d => d.FieldPath.Contains("MSH.7")).Select(d => d.FieldPath).ToList(),
                Confidence = 0.95,
                Severity = InsightSeverity.Informational,
                HealthcareCategory = HealthcareCategory.Administrative,
                RecommendedAction = "Verify if timestamp differences are expected for this comparison",
                AnalysisSource = new AnalysisSource
                {
                    SourceType = AnalysisSourceType.Algorithmic,
                    EngineName = "Pidgeon Pattern Analysis",
                    Version = "1.0",
                    IsLocal = true
                }
            });
        }

        // Generate severity-based insights
        var criticalDiffs = differences.Where(d => d.Severity == DifferenceSeverity.Critical).ToList();
        if (criticalDiffs.Any())
        {
            insights.Add(new AnalysisInsight
            {
                InsightType = InsightType.RootCauseAnalysis,
                Title = "Critical Field Differences Found",
                Description = $"Found {criticalDiffs.Count} critical differences that may cause message processing failures.",
                RelatedFields = criticalDiffs.Select(d => d.FieldPath).ToList(),
                Confidence = 0.9,
                Severity = InsightSeverity.High,
                RecommendedAction = "Review and address critical field differences before deploying",
                AnalysisSource = new AnalysisSource
                {
                    SourceType = AnalysisSourceType.Algorithmic,
                    EngineName = "Pidgeon Severity Analysis",
                    Version = "1.0",
                    IsLocal = true
                }
            });
        }

        return insights;
    }

    private string GetHL7FieldDescription(string segmentType, int fieldNumber)
    {
        // TODO: Implement comprehensive HL7 field descriptions
        return segmentType switch
        {
            "MSH" when fieldNumber == 7 => "Date/Time of Message",
            "MSH" when fieldNumber == 9 => "Message Type",
            "MSH" when fieldNumber == 10 => "Message Control ID",
            "PID" when fieldNumber == 5 => "Patient Name",
            "PID" when fieldNumber == 7 => "Date/Time of Birth",
            "PV1" when fieldNumber == 2 => "Patient Class",
            _ => $"{segmentType} Field {fieldNumber}"
        };
    }

    private DifferenceSeverity GetHL7FieldSeverity(string segmentType, int fieldNumber)
    {
        // TODO: Implement field-specific severity rules
        return (segmentType, fieldNumber) switch
        {
            ("MSH", 7) => DifferenceSeverity.Info, // Timestamp usually not critical
            ("MSH", 9) => DifferenceSeverity.Critical, // Message type is critical
            ("PID", 5) => DifferenceSeverity.Warning, // Patient name important but not always critical
            _ => DifferenceSeverity.Warning
        };
    }

    private bool IsHL7FieldRequired(string segmentType, int fieldNumber)
    {
        // TODO: Implement comprehensive HL7 required field rules
        return (segmentType, fieldNumber) switch
        {
            ("MSH", 1) => true, // Field separator
            ("MSH", 2) => true, // Encoding characters
            ("MSH", 7) => true, // Date/time of message
            ("MSH", 9) => true, // Message type
            ("MSH", 10) => true, // Message control ID
            ("PID", 1) => false, // Set ID is optional in most cases
            _ => false
        };
    }

    /// <summary>
    /// Converts procedural analysis results into analysis insights for consistent display.
    /// </summary>
    private List<AnalysisInsight> ConvertProceduralAnalysisToInsights(ProceduralAnalysisResult proceduralResult)
    {
        var insights = new List<AnalysisInsight>();

        // Add constraint validation insights if there are violations
        if (proceduralResult.ConstraintValidation.ViolationCount > 0)
        {
            insights.Add(new AnalysisInsight
            {
                InsightType = InsightType.ComplianceIssue,
                Title = "Constraint Validation Issues",
                Description = $"Found {proceduralResult.ConstraintValidation.ViolationCount} constraint violations with {proceduralResult.ConstraintValidation.ComplianceScore:P1} compliance score",
                RelatedFields = proceduralResult.ConstraintValidation.Violations.Select(v => v.FieldPath).ToList(),
                Confidence = proceduralResult.ConstraintValidation.ComplianceScore,
                Severity = proceduralResult.ConstraintValidation.ViolationCount > 5 ? InsightSeverity.High : InsightSeverity.Medium,
                HealthcareCategory = HealthcareCategory.Administrative,
                RecommendedAction = "Review field values against standard constraints",
                AnalysisSource = new AnalysisSource
                {
                    SourceType = AnalysisSourceType.Algorithmic,
                    EngineName = "Pidgeon Procedural Analysis Engine",
                    Version = proceduralResult.Metadata.EngineVersion,
                    IsLocal = true,
                    ProcessingTime = proceduralResult.Metadata.ExecutionTime
                }
            });
        }

        // Add demographic analysis insights if there are quality issues
        if (proceduralResult.DemographicAnalysis.QualityScore < 1.0 && proceduralResult.DemographicAnalysis.Issues.Any())
        {
            insights.Add(new AnalysisInsight
            {
                InsightType = InsightType.DataQuality,
                Title = "Demographic Data Quality Issues",
                Description = $"Demographic data quality score: {proceduralResult.DemographicAnalysis.QualityScore:P1} ({proceduralResult.DemographicAnalysis.Issues.Count} issues found)",
                RelatedFields = proceduralResult.DemographicAnalysis.Issues.Select(i => i.FieldPath).Distinct().ToList(),
                Confidence = 1.0 - proceduralResult.DemographicAnalysis.QualityScore,
                Severity = proceduralResult.DemographicAnalysis.QualityScore < 0.7 ? InsightSeverity.Medium : InsightSeverity.Low,
                HealthcareCategory = HealthcareCategory.PatientIdentification,
                RecommendedAction = "Validate demographic fields against standard formats",
                AnalysisSource = new AnalysisSource
                {
                    SourceType = AnalysisSourceType.Algorithmic,
                    EngineName = "Pidgeon Procedural Analysis Engine",
                    Version = proceduralResult.Metadata.EngineVersion,
                    IsLocal = true,
                    ProcessingTime = proceduralResult.Metadata.ExecutionTime
                }
            });
        }

        // Add clinical impact insights if impact level is significant
        if (proceduralResult.ClinicalImpact.ImpactLevel > ClinicalImpactLevel.None)
        {
            var severity = proceduralResult.ClinicalImpact.ImpactLevel switch
            {
                ClinicalImpactLevel.Critical => InsightSeverity.High,
                ClinicalImpactLevel.High => InsightSeverity.High,
                ClinicalImpactLevel.Moderate => InsightSeverity.Medium,
                _ => InsightSeverity.Low
            };

            insights.Add(new AnalysisInsight
            {
                InsightType = InsightType.WorkflowInsight,
                Title = $"Clinical Impact Assessment: {proceduralResult.ClinicalImpact.ImpactLevel}",
                Description = $"Changes may affect patient care (Safety: {proceduralResult.ClinicalImpact.PatientSafetyScore:P0}, Quality: {proceduralResult.ClinicalImpact.CareQualityScore:P0}, Operations: {proceduralResult.ClinicalImpact.OperationalScore:P0})",
                RelatedFields = proceduralResult.ClinicalImpact.ImpactAreas.Select(a => a.Area).ToList(),
                Confidence = Math.Max(proceduralResult.ClinicalImpact.PatientSafetyScore,
                    Math.Max(proceduralResult.ClinicalImpact.CareQualityScore, proceduralResult.ClinicalImpact.OperationalScore)),
                Severity = severity,
                HealthcareCategory = HealthcareCategory.ClinicalData,
                RecommendedAction = proceduralResult.ClinicalImpact.ClinicalRecommendations.FirstOrDefault() ?? "Review clinical impact with healthcare staff",
                AnalysisSource = new AnalysisSource
                {
                    SourceType = AnalysisSourceType.Algorithmic,
                    EngineName = "Pidgeon Procedural Analysis Engine",
                    Version = proceduralResult.Metadata.EngineVersion,
                    IsLocal = true,
                    ProcessingTime = proceduralResult.Metadata.ExecutionTime
                }
            });
        }

        return insights;
    }
}