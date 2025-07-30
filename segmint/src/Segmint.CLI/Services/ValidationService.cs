// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Segmint.Core.HL7.Validation;
using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.HL7;

namespace Segmint.CLI.Services;

/// <summary>
/// Implementation of HL7 message validation service.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;
    private readonly ICompositeValidator _compositeValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="compositeValidator">Composite validator instance.</param>
    public ValidationService(
        ILogger<ValidationService> logger,
        ICompositeValidator compositeValidator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _compositeValidator = compositeValidator ?? throw new ArgumentNullException(nameof(compositeValidator));
    }

    /// <inheritdoc />
    public async Task<ValidationSummary> ValidateAsync(
        string inputPath,
        string[] validationLevels,
        string? configurationPath = null,
        bool strictMode = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting validation of {InputPath}", inputPath);
        
        var stopwatch = Stopwatch.StartNew();
        var summary = new ValidationSummary();

        try
        {
            var messages = await ReadMessagesFromPathAsync(inputPath, cancellationToken);
            
            foreach (var (message, source, index) in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var messageStopwatch = Stopwatch.StartNew();
                
                var coreValidationResult = await ValidateMessageAsync(
                    message, 
                    validationLevels, 
                    configurationPath, 
                    strictMode);
                
                messageStopwatch.Stop();
                
                // Convert Core ValidationResult to CLI ValidationResult
                var cliValidationResult = ConvertValidationResult(coreValidationResult);
                
                summary.Results.Add(new MessageValidationResult
                {
                    Source = source,
                    MessageIndex = index,
                    ValidationResult = cliValidationResult,
                    ProcessingTime = messageStopwatch.Elapsed
                });
                
                summary.TotalMessages++;
                if (cliValidationResult.IsValid)
                {
                    summary.ValidMessages++;
                }
                else
                {
                    summary.InvalidMessages++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during validation of {InputPath}", inputPath);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            summary.ElapsedTime = stopwatch.Elapsed;
            
            _logger.LogInformation(
                "Validation completed: {TotalMessages} total, {ValidMessages} valid, {InvalidMessages} invalid, {ElapsedTime}ms",
                summary.TotalMessages, summary.ValidMessages, summary.InvalidMessages, summary.ElapsedTime.TotalMilliseconds);
        }

        return summary;
    }

    /// <inheritdoc />
    public async Task<Core.HL7.Validation.ValidationResult> ValidateMessageAsync(
        string hl7Message,
        string[] validationLevels,
        string? configurationPath = null,
        bool strictMode = false)
    {
        try
        {
            // Convert validation level strings to ValidationType enums
            var validationTypes = validationLevels.Select(level => level.ToLowerInvariant() switch
            {
                "syntax" => ValidationType.Syntax,
                "semantic" => ValidationType.Semantic,
                "clinical" => ValidationType.Clinical,
                "interface" => ValidationType.Interface,
                _ => ValidationType.Syntax
            }).ToList();

            // Use the composite validator to validate the HL7 string directly
            var result = _compositeValidator.ValidateHL7String(hl7Message);
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating HL7 message");
            
            var result = new Core.HL7.Validation.ValidationResult();
            result.AddIssue(ValidationIssue.SyntaxError("PARSE_ERROR", 
                $"Failed to parse HL7 message: {ex.Message}", "Message"));
            
            return result;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAvailableValidationLevels()
    {
        return new[] { "syntax", "semantic", "clinical" };
    }

    /// <summary>
    /// Reads HL7 messages from file or directory path.
    /// </summary>
    /// <param name="inputPath">Input file or directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of messages with source information.</returns>
    private async Task<IEnumerable<(string Message, string Source, int Index)>> ReadMessagesFromPathAsync(
        string inputPath, 
        CancellationToken cancellationToken)
    {
        var messages = new List<(string Message, string Source, int Index)>();

        if (File.Exists(inputPath))
        {
            // Single file
            var fileMessages = await ReadMessagesFromFileAsync(inputPath, cancellationToken);
            messages.AddRange(fileMessages.Select((msg, idx) => (msg, inputPath, idx)));
        }
        else if (Directory.Exists(inputPath))
        {
            // Directory - process all HL7 files
            var hl7Files = Directory.GetFiles(inputPath, "*.hl7", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(inputPath, "*.txt", SearchOption.AllDirectories));

            foreach (var file in hl7Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var fileMessages = await ReadMessagesFromFileAsync(file, cancellationToken);
                messages.AddRange(fileMessages.Select((msg, idx) => (msg, file, idx)));
            }
        }
        else
        {
            throw new FileNotFoundException($"Input path not found: {inputPath}");
        }

        return messages;
    }

    /// <summary>
    /// Reads HL7 messages from a single file.
    /// </summary>
    /// <param name="filePath">File path to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of HL7 message strings.</returns>
    private async Task<IEnumerable<string>> ReadMessagesFromFileAsync(
        string filePath, 
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        
        // Split on message boundaries (typically by MSH segments)
        var messages = new List<string>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var currentMessage = new List<string>();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("MSH") && currentMessage.Count > 0)
            {
                // Start of new message, save previous
                messages.Add(string.Join("\r", currentMessage));
                currentMessage.Clear();
            }
            
            if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                currentMessage.Add(trimmedLine);
            }
        }
        
        // Add final message
        if (currentMessage.Count > 0)
        {
            messages.Add(string.Join("\r", currentMessage));
        }
        
        return messages;
    }

    /// <summary>
    /// Parses an HL7 message string into a message object.
    /// </summary>
    /// <param name="hl7Message">HL7 message string.</param>
    /// <returns>Parsed HL7 message object.</returns>
    private async Task<HL7Message> ParseHL7MessageAsync(string hl7Message)
    {
        // For now, create a basic message wrapper
        // In future, implement proper message type detection and parsing
        var message = new RDEMessage();
        
        // Split into segments
        var segments = hl7Message.Split('\r', StringSplitOptions.RemoveEmptyEntries);
        
        // TODO: Implement proper segment parsing and validation
        // For now, skip detailed segment parsing for validation
        // This would need to be implemented based on the actual HL7 message structure
        foreach (var segmentString in segments)
        {
            if (!string.IsNullOrWhiteSpace(segmentString))
            {
                // Placeholder for segment validation
                // Would need proper segment parsing logic here
            }
        }
        
        return await Task.FromResult(message);
    }

    /// <summary>
    /// Converts a Core ValidationResult to a CLI ValidationResult.
    /// </summary>
    /// <param name="coreResult">The core validation result.</param>
    /// <returns>CLI validation result.</returns>
    private ValidationResult ConvertValidationResult(Core.HL7.Validation.ValidationResult coreResult)
    {
        var cliResult = new ValidationResult
        {
            IsValid = coreResult.IsValid
        };

        foreach (var issue in coreResult.Issues)
        {
            cliResult.Errors.Add(new ValidationError
            {
                Level = issue.Type switch
                {
                    ValidationType.Syntax => ValidationLevel.Syntax,
                    ValidationType.Semantic => ValidationLevel.Semantic,
                    ValidationType.Clinical => ValidationLevel.Clinical,
                    ValidationType.Interface => ValidationLevel.Interface,
                    _ => ValidationLevel.Syntax
                },
                Code = issue.Code,
                Message = issue.Description,
                Field = issue.Location
            });
        }

        return cliResult;
    }
}

/// <summary>
/// CLI validation result for compatibility.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();
}

/// <summary>
/// Generic segment for validation purposes.
/// </summary>
internal class GenericSegment : Segmint.Core.HL7.HL7Segment
{
    private readonly string _rawSegment;
    private readonly string _segmentId;

    public GenericSegment(string rawSegment)
    {
        _rawSegment = rawSegment ?? throw new ArgumentNullException(nameof(rawSegment));
        
        // Basic parsing for validation
        var fields = rawSegment.Split('|');
        if (fields.Length > 0)
        {
            _segmentId = fields[0];
        }
        else
        {
            _segmentId = "UNK";
        }
    }

    public override string SegmentId => _segmentId;

    public override HL7Segment Clone()
    {
        return new GenericSegment(_rawSegment);
    }

    protected override void InitializeFields()
    {
        // Implementation depends on segment type
        // For validation purposes, we'll keep it simple
    }

    public override string ToHL7String()
    {
        return _rawSegment;
    }
}

/// <summary>
/// Validation context for message validation.
/// </summary>
internal class ValidationContext
{
    public bool StrictMode { get; set; }
    public string[] ValidationLevels { get; set; } = Array.Empty<string>();
    public string? ConfigurationPath { get; set; }
}