// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.DeIdentification;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Application.Services.DeIdentification;

/// <summary>
/// Application service for orchestrating de-identification file operations.
/// Delegates to specialized services for compliance, reporting, and estimation.
/// </summary>
internal class DeIdentificationEngine : IDeIdentificationEngine
{
    private readonly IPhiDetector _phiDetector;
    private readonly IDeIdentificationService _deIdentificationService;
    private readonly ComplianceValidationService _complianceService;
    private readonly AuditReportService _auditService;
    private readonly ResourceEstimationService _estimationService;
    
    public DeIdentificationEngine(
        IPhiDetector phiDetector,
        IDeIdentificationService deIdentificationService,
        ComplianceValidationService complianceService,
        AuditReportService auditService,
        ResourceEstimationService estimationService)
    {
        _phiDetector = phiDetector ?? throw new ArgumentNullException(nameof(phiDetector));
        _deIdentificationService = deIdentificationService ?? throw new ArgumentNullException(nameof(deIdentificationService));
        _complianceService = complianceService ?? throw new ArgumentNullException(nameof(complianceService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _estimationService = estimationService ?? throw new ArgumentNullException(nameof(estimationService));
    }

    /// <summary>
    /// De-identifies a single file containing healthcare messages.
    /// </summary>
    public async Task<Result<DeIdentificationResult>> ProcessFileAsync(
        string inputPath, 
        string outputPath, 
        DeIdentificationOptions options)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                return Result<DeIdentificationResult>.Failure("Input path cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(outputPath))
                return Result<DeIdentificationResult>.Failure("Output path cannot be null or empty");
            
            if (!File.Exists(inputPath))
                return Result<DeIdentificationResult>.Failure($"Input file not found: {inputPath}");

            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Read and process file content
            var inputContent = await File.ReadAllTextAsync(inputPath);
            if (string.IsNullOrWhiteSpace(inputContent))
                return Result<DeIdentificationResult>.Failure("Input file is empty or contains no readable content");

            // Process using domain service
            var contextResult = _deIdentificationService.CreateContext(options);
            if (contextResult.IsFailure)
                return Result<DeIdentificationResult>.Failure(contextResult.Error);

            var messages = new[] { inputContent };
            var result = await _deIdentificationService.DeIdentifyMessagesAsync(messages, options);
            if (result.IsFailure)
                return Result<DeIdentificationResult>.Failure(result.Error);

            // Write de-identified content to output file
            if (result.Value.DeIdentifiedMessages.Any())
            {
                await File.WriteAllTextAsync(outputPath, result.Value.DeIdentifiedMessages.First());
            }

            return Result<DeIdentificationResult>.Success(result.Value);
        }
        catch (IOException ex)
        {
            return Result<DeIdentificationResult>.Failure($"File I/O error: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result<DeIdentificationResult>.Failure($"Access denied: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<DeIdentificationResult>.Failure($"File processing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// De-identifies all healthcare message files in a directory.
    /// </summary>
    public async Task<Result<BatchDeIdentificationResult>> ProcessDirectoryAsync(
        string inputDirectory, 
        string outputDirectory, 
        DeIdentificationOptions options)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(inputDirectory))
                return Result<BatchDeIdentificationResult>.Failure("Input directory cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(outputDirectory))
                return Result<BatchDeIdentificationResult>.Failure("Output directory cannot be null or empty");
            
            if (!Directory.Exists(inputDirectory))
                return Result<BatchDeIdentificationResult>.Failure($"Input directory not found: {inputDirectory}");

            // Ensure output directory exists
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var files = Directory.GetFiles(inputDirectory, "*", SearchOption.AllDirectories);
            var fileResults = new List<FileProcessingResult>();
            var allMessages = new List<string>();

            // Process each file
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(inputDirectory, file);
                var outputPath = Path.Combine(outputDirectory, relativePath);
                var outputDir = Path.GetDirectoryName(outputPath);
                
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var startTime = DateTime.UtcNow;
                var fileResult = await ProcessFileAsync(file, outputPath, options);
                var processingTime = DateTime.UtcNow - startTime;

                var fileInfo = new FileInfo(file);
                var outputFileInfo = File.Exists(outputPath) ? new FileInfo(outputPath) : null;

                fileResults.Add(new FileProcessingResult
                {
                    InputPath = file,
                    OutputPath = outputPath,
                    Success = fileResult.IsSuccess,
                    Result = fileResult.IsSuccess ? fileResult.Value : null,
                    ErrorMessage = fileResult.IsFailure ? fileResult.Error.Message : null,
                    ProcessingTime = processingTime,
                    SizeInfo = new FileSizeInfo
                    {
                        OriginalSizeBytes = fileInfo.Length,
                        DeIdentifiedSizeBytes = outputFileInfo?.Length ?? 0
                    }
                });

                if (fileResult.IsSuccess)
                {
                    allMessages.AddRange(fileResult.Value.DeIdentifiedMessages);
                }
            }

            // Create batch result
            var successfulResults = fileResults.Where(r => r.Success && r.Result != null).Select(r => r.Result!).ToList();
            var combinedStatistics = CombineStatistics(successfulResults);
            var batchCompliance = await _complianceService.ValidateBatchComplianceAsync(successfulResults);

            var batchResult = new BatchDeIdentificationResult
            {
                FileResults = fileResults,
                CombinedStatistics = combinedStatistics,
                BatchCompliance = batchCompliance.IsSuccess ? batchCompliance.Value : new ComplianceVerification
                {
                    MeetsSafeHarbor = false,
                    SafeHarborChecklist = new Dictionary<string, bool>(),
                    Status = ComplianceStatus.Unknown
                },
                CombinedIdMappings = CombineIdMappings(successfulResults),
                Metadata = new BatchProcessingMetadata
                {
                    TotalFiles = fileResults.Count,
                    SuccessfulFiles = fileResults.Count(r => r.Success),
                    FailedFiles = fileResults.Count(r => !r.Success),
                    TotalProcessingTime = TimeSpan.FromTicks(fileResults.Sum(r => r.ProcessingTime.Ticks))
                }
            };

            return Result<BatchDeIdentificationResult>.Success(batchResult);
        }
        catch (Exception ex)
        {
            return Result<BatchDeIdentificationResult>.Failure($"Directory processing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Previews de-identification changes without modifying files.
    /// </summary>
    public async Task<Result<DeIdentificationPreview>> PreviewChangesAsync(
        string inputPath, 
        DeIdentificationOptions options)
    {
        // Delegate to specialized preview service (placeholder implementation)
        await Task.Yield();
        
        var preview = new DeIdentificationPreview
        {
            SampleChanges = Array.Empty<PreviewChange>(),
            EstimatedStatistics = new DeIdentificationStatistics
            {
                TotalMessages = 0,
                TotalIdentifiersProcessed = 0,
                IdentifiersByType = new Dictionary<IdentifierType, int>(),
                FieldsModified = 0,
                DatesShifted = 0,
                AverageProcessingTimeMs = 0,
                TotalProcessingTime = TimeSpan.Zero,
                UniqueSubjects = 0
            },
            ComplianceAssessment = new ComplianceVerification
            {
                MeetsSafeHarbor = true,
                SafeHarborChecklist = new Dictionary<string, bool>(),
                Status = ComplianceStatus.Compliant
            },
            FilesToProcess = Array.Empty<string>(),
            ResourceEstimate = new ProcessingEstimate
            {
                EstimatedTime = TimeSpan.Zero,
                EstimatedMemoryBytes = 0,
                FileCount = 0,
                TotalInputSizeBytes = 0,
                EstimatedThroughput = 0
            }
        };

        return Result<DeIdentificationPreview>.Success(preview);
    }

    /// <summary>
    /// Validates compliance by delegating to specialized service.
    /// </summary>
    public Task<Result<ValidationResult>> ValidateComplianceAsync(
        string deidentifiedPath, 
        string? originalPath = null,
        DeIdentificationOptions? options = null)
    {
        return _complianceService.ValidateComplianceAsync(deidentifiedPath, originalPath, options);
    }

    /// <summary>
    /// Generates audit report by delegating to specialized service.
    /// </summary>
    public Task<Result<string>> GenerateAuditReportAsync(
        DeIdentificationResult result, 
        string outputPath, 
        ReportFormat format = ReportFormat.Html)
    {
        return _auditService.GenerateAuditReportAsync(result, outputPath, format);
    }

    /// <summary>
    /// Estimates resources by delegating to specialized service.
    /// </summary>
    public Task<Result<ProcessingEstimate>> EstimateResourcesAsync(
        string inputPath, 
        DeIdentificationOptions options)
    {
        return _estimationService.EstimateResourcesAsync(inputPath, options);
    }

    // Private helper methods

    private static DeIdentificationStatistics CombineStatistics(List<DeIdentificationResult> results)
    {
        if (!results.Any())
        {
            return new DeIdentificationStatistics
            {
                TotalMessages = 0,
                TotalIdentifiersProcessed = 0,
                IdentifiersByType = new Dictionary<IdentifierType, int>(),
                FieldsModified = 0,
                DatesShifted = 0,
                AverageProcessingTimeMs = 0,
                TotalProcessingTime = TimeSpan.Zero,
                UniqueSubjects = 0
            };
        }

        var totalTime = TimeSpan.FromTicks(results.Sum(r => r.Statistics.TotalProcessingTime.Ticks));
        var totalMessages = results.Sum(r => r.Statistics.TotalMessages);

        return new DeIdentificationStatistics
        {
            TotalMessages = totalMessages,
            TotalIdentifiersProcessed = results.Sum(r => r.Statistics.TotalIdentifiersProcessed),
            IdentifiersByType = results.SelectMany(r => r.Statistics.IdentifiersByType)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Sum(kvp => kvp.Value)),
            FieldsModified = results.Sum(r => r.Statistics.FieldsModified),
            DatesShifted = results.Sum(r => r.Statistics.DatesShifted),
            AverageProcessingTimeMs = totalMessages > 0 ? totalTime.TotalMilliseconds / totalMessages : 0,
            TotalProcessingTime = totalTime,
            UniqueSubjects = results.Sum(r => r.Statistics.UniqueSubjects)
        };
    }

    private static IReadOnlyDictionary<string, string> CombineIdMappings(List<DeIdentificationResult> results)
    {
        var combinedMappings = new Dictionary<string, string>();
        
        foreach (var result in results)
        {
            foreach (var mapping in result.IdMappings)
            {
                combinedMappings[mapping.Key] = mapping.Value;
            }
        }
        
        return combinedMappings;
    }
}