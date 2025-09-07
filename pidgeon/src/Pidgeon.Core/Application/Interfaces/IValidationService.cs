using System.Threading.Tasks;
using Pidgeon.Core.Domain.Validation;

namespace Pidgeon.Core.Application.Interfaces;

/// <summary>
/// Service for validating healthcare messages against standards and profiles.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates a message against a specific standard.
    /// </summary>
    /// <param name="message">Raw message content to validate</param>
    /// <param name="standard">Standard to validate against (e.g., "HL7v23", "FHIR-R4")</param>
    /// <param name="mode">Validation mode (Strict or Compatibility)</param>
    /// <returns>Validation result with issues and statistics</returns>
    Task<Result<ValidationResult>> ValidateMessageAsync(
        string message, 
        string standard, 
        ValidationMode mode = ValidationMode.Compatibility);
    
    /// <summary>
    /// Validates a message against a vendor-specific profile.
    /// </summary>
    /// <param name="message">Raw message content to validate</param>
    /// <param name="profileId">Profile identifier (e.g., "epic-lab-v2")</param>
    /// <returns>Validation result with profile-specific issues</returns>
    Task<Result<ValidationResult>> ValidateWithProfileAsync(
        string message, 
        string profileId);
    
    /// <summary>
    /// Validates multiple messages as a batch.
    /// </summary>
    /// <param name="messages">Collection of messages to validate</param>
    /// <param name="standard">Standard to validate against</param>
    /// <param name="mode">Validation mode</param>
    /// <returns>Collection of validation results</returns>
    Task<Result<ValidationBatchResult>> ValidateBatchAsync(
        string[] messages, 
        string standard, 
        ValidationMode mode = ValidationMode.Compatibility);
}

/// <summary>
/// Result of validating multiple messages.
/// </summary>
public record ValidationBatchResult
{
    public required int TotalMessages { get; init; }
    public required int ValidMessages { get; init; }
    public required int InvalidMessages { get; init; }
    public required ValidationResult[] Results { get; init; }
    public required ValidationStatistics OverallStatistics { get; init; }
}