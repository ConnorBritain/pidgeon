using System.Threading.Tasks;
using Pidgeon.Core.Domain.Validation;

namespace Pidgeon.Core.Application.Interfaces;

/// <summary>
/// Service for validating healthcare messages against standards and rules.
/// </summary>
public interface IMessageValidationService
{
    /// <summary>
    /// Validates a healthcare message against its standard and rules.
    /// </summary>
    /// <param name="messageContent">The raw message content to validate</param>
    /// <param name="standard">The healthcare standard to validate against (e.g., "hl7", "fhir", "ncpdp"). If null, will attempt to detect.</param>
    /// <param name="mode">The validation mode - Strict enforces all rules, Compatibility allows common variations</param>
    /// <returns>A result containing validation outcome with any issues found</returns>
    Task<Result<ValidationResult>> ValidateAsync(
        string messageContent, 
        string? standard = null, 
        ValidationMode mode = ValidationMode.Strict);
}