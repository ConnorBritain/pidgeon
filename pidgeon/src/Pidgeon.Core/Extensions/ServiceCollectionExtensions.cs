// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;
using Pidgeon.Core.Standards.Common;
using Pidgeon.Core.Services;
using Pidgeon.Core.Services.Configuration;

namespace Pidgeon.Core.Extensions;

/// <summary>
/// Extension methods for configuring Pidgeon services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core Pidgeon services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddPidgeonCore(this IServiceCollection services)
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
        
        // Add configuration intelligence services
        services.AddScoped<IConfigurationInferenceService, ConfigurationInferenceService>();
        services.AddScoped<IConfigurationValidationService, ConfigurationValidationService>();
        services.AddScoped<IConfigurationCatalog, ConfigurationCatalog>();
        services.AddScoped<IVendorDetectionService, VendorDetectionService>();
        services.AddScoped<IFieldPatternAnalyzer, FieldPatternAnalyzer>();
        services.AddScoped<IMessagePatternAnalyzer, MessagePatternAnalyzer>();
        services.AddScoped<IConfidenceCalculator, ConfidenceCalculator>();
        services.AddScoped<IFormatDeviationDetector, FormatDeviationDetector>();
        services.AddScoped<IVendorPatternRepository, VendorPatternRepository>();
        
        // Register HL7 configuration plugins
        services.AddStandardConfigurationPlugins();
        
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

    /// <summary>
    /// Registers all standard-specific configuration plugins.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddStandardConfigurationPlugins(this IServiceCollection services)
    {
        // Register HL7 v2.3 configuration plugins
        services.AddScoped<Standards.HL7.v23.Configuration.HL7VendorDetectionPlugin>();
        services.AddScoped<Standards.HL7.v23.Configuration.HL7FieldAnalysisPlugin>();
        services.AddScoped<Standards.HL7.v23.Configuration.HL7ConfigurationPlugin>();

        // Register HL7 plugins in plugin registry
        services.AddScoped<IStandardVendorDetectionPlugin>(provider => 
            provider.GetRequiredService<Standards.HL7.v23.Configuration.HL7VendorDetectionPlugin>());
        services.AddScoped<IStandardFieldAnalysisPlugin>(provider => 
            provider.GetRequiredService<Standards.HL7.v23.Configuration.HL7FieldAnalysisPlugin>());
        services.AddScoped<IConfigurationPlugin>(provider => 
            provider.GetRequiredService<Standards.HL7.v23.Configuration.HL7ConfigurationPlugin>());

        // Future plugins would be added here:
        // services.AddScoped<Standards.FHIR.R4.Configuration.FHIRVendorDetectionPlugin>();
        // services.AddScoped<Standards.NCPDP.Configuration.NCPDPVendorDetectionPlugin>();

        return services;
    }
}