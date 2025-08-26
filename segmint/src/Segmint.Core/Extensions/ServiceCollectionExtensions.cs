// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Segmint.Core.Standards.Common;
using Segmint.Core.Domain;

namespace Segmint.Core.Extensions;

/// <summary>
/// Extension methods for configuring Segmint services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core Segmint services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSegmintCore(this IServiceCollection services)
    {
        // Add standard plugin registry
        services.AddSingleton<IStandardPluginRegistry, StandardPluginRegistry>();
        
        // Add core services
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IGenerationService, GenerationService>();
        services.AddScoped<ITransformationService, TransformationService>();
        
        // Add domain-based generation services
        services.AddScoped<Generation.IGenerationService, Generation.Algorithmic.AlgorithmicGenerationService>();
        
        // Add configuration services
        services.AddScoped<IConfigurationInferenceService, ConfigurationInferenceService>();
        services.AddScoped<IConfigurationValidationService, ConfigurationValidationService>();
        
        return services;
    }

    /// <summary>
    /// Registers a standard plugin with the dependency injection container.
    /// </summary>
    /// <typeparam name="TPlugin">The plugin type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddStandardPlugin<TPlugin>(this IServiceCollection services)
        where TPlugin : class, IStandardPlugin
    {
        services.AddScoped<TPlugin>();
        services.AddScoped<IStandardPlugin, TPlugin>();
        
        return services;
    }

    /// <summary>
    /// Registers a standard plugin with the dependency injection container using a factory.
    /// </summary>
    /// <typeparam name="TPlugin">The plugin type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="factory">The factory function to create the plugin</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddStandardPlugin<TPlugin>(
        this IServiceCollection services,
        Func<IServiceProvider, TPlugin> factory)
        where TPlugin : class, IStandardPlugin
    {
        services.AddScoped<TPlugin>(factory);
        services.AddScoped<IStandardPlugin>(provider => provider.GetRequiredService<TPlugin>());
        
        return services;
    }
}

/// <summary>
/// Registry for managing standard plugins.
/// </summary>
public interface IStandardPluginRegistry
{
    /// <summary>
    /// Gets all registered plugins.
    /// </summary>
    /// <returns>A list of all registered plugins</returns>
    IReadOnlyList<IStandardPlugin> GetAllPlugins();

    /// <summary>
    /// Gets a plugin by standard name and version.
    /// </summary>
    /// <param name="standardName">The standard name</param>
    /// <param name="standardVersion">The standard version (optional)</param>
    /// <returns>The plugin if found, null otherwise</returns>
    IStandardPlugin? GetPlugin(string standardName, Version? standardVersion = null);

    /// <summary>
    /// Gets plugins that can handle the specified message content.
    /// </summary>
    /// <param name="messageContent">The message content</param>
    /// <returns>A list of plugins that can handle the message</returns>
    IReadOnlyList<IStandardPlugin> GetCapablePlugins(string messageContent);

    /// <summary>
    /// Gets supported message types across all plugins.
    /// </summary>
    /// <returns>A dictionary mapping standards to their supported message types</returns>
    IReadOnlyDictionary<string, IReadOnlyList<string>> GetSupportedMessageTypes();
}

/// <summary>
/// Implementation of the standard plugin registry.
/// </summary>
internal class StandardPluginRegistry : IStandardPluginRegistry
{
    private readonly IEnumerable<IStandardPlugin> _plugins;
    private readonly ILogger<StandardPluginRegistry> _logger;

    public StandardPluginRegistry(IEnumerable<IStandardPlugin> plugins, ILogger<StandardPluginRegistry> logger)
    {
        _plugins = plugins;
        _logger = logger;
        
        LogRegisteredPlugins();
    }

    public IReadOnlyList<IStandardPlugin> GetAllPlugins()
    {
        return _plugins.ToList();
    }

    public IStandardPlugin? GetPlugin(string standardName, Version? standardVersion = null)
    {
        var candidates = _plugins.Where(p => 
            string.Equals(p.StandardName, standardName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!candidates.Any())
            return null;

        if (standardVersion == null)
        {
            // Return the plugin with the highest version
            return candidates.OrderByDescending(p => p.StandardVersion).First();
        }

        // Return exact version match
        return candidates.FirstOrDefault(p => p.StandardVersion == standardVersion);
    }

    public IReadOnlyList<IStandardPlugin> GetCapablePlugins(string messageContent)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
            return Array.Empty<IStandardPlugin>();

        var capablePlugins = new List<IStandardPlugin>();

        foreach (var plugin in _plugins)
        {
            try
            {
                if (plugin.CanHandle(messageContent))
                {
                    capablePlugins.Add(plugin);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Plugin {PluginName} threw exception when checking if it can handle message", 
                    plugin.StandardName);
            }
        }

        return capablePlugins;
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetSupportedMessageTypes()
    {
        return _plugins.ToDictionary(
            p => $"{p.StandardName} v{p.StandardVersion}",
            p => p.SupportedMessageTypes);
    }

    private void LogRegisteredPlugins()
    {
        if (!_plugins.Any())
        {
            _logger.LogWarning("No standard plugins are registered");
            return;
        }

        _logger.LogInformation("Registered {PluginCount} standard plugins:", _plugins.Count());
        
        foreach (var plugin in _plugins)
        {
            _logger.LogInformation("  - {StandardName} v{StandardVersion}: {MessageTypeCount} message types", 
                plugin.StandardName, 
                plugin.StandardVersion, 
                plugin.SupportedMessageTypes.Count);
        }
    }
}

/// <summary>
/// Core message processing service.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Processes a message by parsing, validating, and optionally transforming it.
    /// </summary>
    /// <param name="messageContent">The message content</param>
    /// <param name="options">Processing options</param>
    /// <returns>A result containing the processed message or an error</returns>
    Task<Result<ProcessedMessage>> ProcessMessageAsync(string messageContent, MessageProcessingOptions? options = null);

    /// <summary>
    /// Generates a new message from domain objects.
    /// </summary>
    /// <param name="domainObject">The domain object to serialize</param>
    /// <param name="standard">The target standard</param>
    /// <param name="messageType">The message type to generate</param>
    /// <param name="options">Generation options</param>
    /// <returns>A result containing the generated message or an error</returns>
    Task<Result<string>> GenerateMessageAsync(object domainObject, string standard, string messageType, Generation.GenerationOptions? options = null);
}

/// <summary>
/// Message validation service.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates a message according to its standard.
    /// </summary>
    /// <param name="messageContent">The message to validate</param>
    /// <param name="validationMode">The validation mode</param>
    /// <param name="standard">The specific standard to validate against (optional)</param>
    /// <returns>A result containing validation results</returns>
    Task<Result<ValidationResult>> ValidateAsync(string messageContent, ValidationMode validationMode = ValidationMode.Strict, string? standard = null);
}

/// <summary>
/// Message generation service.
/// </summary>
public interface IGenerationService
{
    /// <summary>
    /// Generates synthetic test data for a given standard and message type.
    /// </summary>
    /// <param name="standard">The target standard</param>
    /// <param name="messageType">The message type</param>
    /// <param name="count">Number of messages to generate</param>
    /// <param name="options">Generation options</param>
    /// <returns>A result containing the generated messages or an error</returns>
    Task<Result<IReadOnlyList<string>>> GenerateSyntheticDataAsync(string standard, string messageType, int count = 1, Generation.GenerationOptions? options = null);
}

/// <summary>
/// Message transformation service for converting between standards.
/// </summary>
public interface ITransformationService
{
    /// <summary>
    /// Transforms a message from one standard to another.
    /// </summary>
    /// <param name="sourceMessage">The source message</param>
    /// <param name="sourceStandard">The source standard</param>
    /// <param name="targetStandard">The target standard</param>
    /// <param name="options">Transformation options</param>
    /// <returns>A result containing the transformed message or an error</returns>
    Task<Result<string>> TransformAsync(string sourceMessage, string sourceStandard, string targetStandard, TransformationOptions? options = null);
}

/// <summary>
/// Configuration inference service for analyzing messages and inferring configurations.
/// </summary>
public interface IConfigurationInferenceService
{
    /// <summary>
    /// Analyzes sample messages and infers configuration patterns.
    /// </summary>
    /// <param name="sampleMessages">Sample messages to analyze</param>
    /// <param name="options">Analysis options</param>
    /// <returns>A result containing the inferred configuration</returns>
    Task<Result<InferredConfiguration>> InferConfigurationAsync(IEnumerable<string> sampleMessages, InferenceOptions? options = null);
}

/// <summary>
/// Configuration validation service.
/// </summary>
public interface IConfigurationValidationService
{
    /// <summary>
    /// Validates a configuration against known patterns and best practices.
    /// </summary>
    /// <param name="configuration">The configuration to validate</param>
    /// <returns>A result containing validation results</returns>
    Task<Result<ConfigurationValidationResult>> ValidateConfigurationAsync(IStandardConfig configuration);
}

// Placeholder service implementations (will be implemented in subsequent tasks)
internal class MessageService : IMessageService
{
    public Task<Result<ProcessedMessage>> ProcessMessageAsync(string messageContent, MessageProcessingOptions? options = null)
    {
        throw new NotImplementedException("MessageService implementation pending");
    }

    public Task<Result<string>> GenerateMessageAsync(object domainObject, string standard, string messageType, Generation.GenerationOptions? options = null)
    {
        throw new NotImplementedException("MessageService implementation pending");
    }
}

internal class ValidationService : IValidationService
{
    public Task<Result<ValidationResult>> ValidateAsync(string messageContent, ValidationMode validationMode = ValidationMode.Strict, string? standard = null)
    {
        throw new NotImplementedException("ValidationService implementation pending");
    }
}

internal class GenerationService : IGenerationService
{
    private readonly Generation.IGenerationService _domainGenerationService;
    
    public GenerationService(Generation.IGenerationService domainGenerationService)
    {
        _domainGenerationService = domainGenerationService;
    }
    
    public async Task<Result<IReadOnlyList<string>>> GenerateSyntheticDataAsync(string standard, string messageType, int count = 1, Generation.GenerationOptions? options = null)
    {
        try
        {
            var messages = new List<string>();
            var generationOptions = new Generation.GenerationOptions();
            
            for (int i = 0; i < count; i++)
            {
                var result = messageType.ToUpperInvariant() switch
                {
                    "PATIENT" => await GeneratePatientMessageAsync(standard, generationOptions),
                    "MEDICATION" => await GenerateMedicationMessageAsync(standard, generationOptions),
                    "PRESCRIPTION" => await GeneratePrescriptionMessageAsync(standard, generationOptions),
                    "ENCOUNTER" => await GenerateEncounterMessageAsync(standard, generationOptions),
                    "ADT" => await GenerateEncounterMessageAsync(standard, generationOptions), // ADT maps to encounter
                    "RDE" => await GeneratePrescriptionMessageAsync(standard, generationOptions), // RDE maps to prescription
                    _ => Result<string>.Failure($"Unsupported message type: {messageType}")
                };
                
                if (!result.IsSuccess)
                    return Result<IReadOnlyList<string>>.Failure(result.Error);
                    
                messages.Add(result.Value);
            }
            
            return Result<IReadOnlyList<string>>.Success(messages);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<string>>.Failure($"Generation failed: {ex.Message}");
        }
    }
    
    private async Task<Result<string>> GeneratePatientMessageAsync(string standard, Generation.GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        // For now, return a simple representation - this would be enhanced with proper HL7/FHIR serialization
        var patient = patientResult.Value;
        return Result<string>.Success($"Patient: {patient.Name.DisplayName}, DOB: {patient.BirthDate:yyyy-MM-dd}");
    }
    
    private async Task<Result<string>> GenerateMedicationMessageAsync(string standard, Generation.GenerationOptions options)
    {
        var medicationResult = _domainGenerationService.GenerateMedication(options);
        if (!medicationResult.IsSuccess)
            return Result<string>.Failure(medicationResult.Error);
            
        var medication = medicationResult.Value;
        return Result<string>.Success($"Medication: {medication.DisplayName} ({medication.GenericName})");
    }
    
    private async Task<Result<string>> GeneratePrescriptionMessageAsync(string standard, Generation.GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"Prescription: {prescription.Medication.DisplayName} for {prescription.Patient.Name.DisplayName}");
    }
    
    private async Task<Result<string>> GenerateEncounterMessageAsync(string standard, Generation.GenerationOptions options)
    {
        var encounterResult = _domainGenerationService.GenerateEncounter(options);
        if (!encounterResult.IsSuccess)
            return Result<string>.Failure(encounterResult.Error);
            
        var encounter = encounterResult.Value;
        return Result<string>.Success($"Encounter: {encounter.Patient.Name.DisplayName} at {encounter.Location} ({encounter.Type})");
    }
}

internal class TransformationService : ITransformationService
{
    public Task<Result<string>> TransformAsync(string sourceMessage, string sourceStandard, string targetStandard, TransformationOptions? options = null)
    {
        throw new NotImplementedException("TransformationService implementation pending");
    }
}

internal class ConfigurationInferenceService : IConfigurationInferenceService
{
    public Task<Result<InferredConfiguration>> InferConfigurationAsync(IEnumerable<string> sampleMessages, InferenceOptions? options = null)
    {
        throw new NotImplementedException("ConfigurationInferenceService implementation pending");
    }
}

internal class ConfigurationValidationService : IConfigurationValidationService
{
    public Task<Result<ConfigurationValidationResult>> ValidateConfigurationAsync(IStandardConfig configuration)
    {
        throw new NotImplementedException("ConfigurationValidationService implementation pending");
    }
}

// Supporting types (placeholders for now)
public record ProcessedMessage;
public record MessageProcessingOptions;
public record TransformationOptions;
public record InferenceOptions;
public record InferredConfiguration;
public record ConfigurationValidationResult;