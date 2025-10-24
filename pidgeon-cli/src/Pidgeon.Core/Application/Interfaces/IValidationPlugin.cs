using System.Threading.Tasks;
using Pidgeon.Core.Domain.Validation;

namespace Pidgeon.Core.Application.Interfaces;

/// <summary>
/// Plugin interface for standard-specific validation logic.
/// </summary>
public interface IValidationPlugin
{
    /// <summary>
    /// Standard this plugin validates (e.g., "HL7v23", "FHIR-R4", "NCPDP").
    /// </summary>
    string Standard { get; }
    
    /// <summary>
    /// Checks if this plugin can handle the given standard.
    /// </summary>
    bool CanValidate(string standard);
    
    /// <summary>
    /// Validates a message according to the standard's rules.
    /// </summary>
    /// <param name="message">Raw message content</param>
    /// <param name="mode">Validation mode</param>
    /// <param name="profile">Optional vendor profile for additional rules</param>
    /// <returns>Validation result with standard-specific issues</returns>
    Task<Result<ValidationResult>> ValidateAsync(
        string message, 
        ValidationMode mode,
        ValidationProfile? profile = null);
    
    /// <summary>
    /// Gets the base validation rules for this standard.
    /// </summary>
    /// <param name="mode">Validation mode affects which rules are applied</param>
    /// <returns>Collection of validation rules</returns>
    Task<ValidationRule[]> GetStandardRulesAsync(ValidationMode mode);
}