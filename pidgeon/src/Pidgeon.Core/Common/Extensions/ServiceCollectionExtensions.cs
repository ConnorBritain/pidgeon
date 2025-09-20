// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Infrastructure.Registry;
using Pidgeon.Core.Application.Services.Generation;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Infrastructure.Standards.HL7.v23;
using Pidgeon.Core.Application.Interfaces.Reference;
using Pidgeon.Core.Application.Services.Reference;
using Pidgeon.Core.Infrastructure.Reference;
using Pidgeon.Core.Application.Interfaces.Generation;
using Pidgeon.Core.Infrastructure.Generation.Constraints;

namespace Pidgeon.Core.Extensions;

/// <summary>
/// Extension methods for configuring Pidgeon services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core Pidgeon services to the dependency injection container.
    /// Uses convention-based registration to eliminate manual service registration duplication.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddPidgeonCore(this IServiceCollection services)
    {
        // Add standard plugin registry (singleton pattern)
        services.AddSingleton<IStandardPluginRegistry, StandardPluginRegistry>();
        
        // Add vendor plugin registry for multi-standard detection
        services.AddSingleton<Pidgeon.Core.Application.Interfaces.Configuration.IStandardVendorPluginRegistry, 
                             Pidgeon.Core.Application.Services.Configuration.StandardVendorPluginRegistry>();
        
        // Register standard vendor plugins from Infrastructure assembly
        services.AddStandardVendorPlugins();
        
        // Add all core services using convention-based registration
        services.AddPidgeonCoreServices();
        
        // Register standard-specific configuration plugins
        services.AddStandardConfigurationPlugins();
        
        // Register message generation plugins
        services.AddMessageGenerationPlugins();
        
        // Register HL7 v2.3 message factory
        services.AddScoped<IHL7MessageFactory, HL7v23MessageFactory>();
        
        // Register FHIR R4 resource factory
        services.AddScoped<Pidgeon.Core.Infrastructure.Standards.FHIR.R4.IFHIRResourceFactory, 
                          Pidgeon.Core.Infrastructure.Standards.FHIR.R4.FHIRResourceFactory>();
        
        // Register standards reference system
        services.AddStandardsReferenceSystem();

        // Register demographics data service for data-driven generation
        services.AddScoped<Pidgeon.Core.Application.Interfaces.Reference.IDemographicsDataService,
                          Pidgeon.Core.Application.Services.Reference.DemographicsDataService>();

        // Register constraint resolution system
        services.AddConstraintResolution();

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
    /// Registers the standards reference system with all plugins and services.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddStandardsReferenceSystem(this IServiceCollection services)
    {
        // Register core reference service
        services.AddScoped<IStandardReferenceService, StandardReferenceService>();
        
        // Register memory cache for plugin caching
        services.AddMemoryCache();
        
        // Register HL7 reference plugins for different versions
        services.AddHL7ReferencePlugin("2.3", "hl7v23", "HL7 v2.3");
        services.AddHL7ReferencePlugin("2.4", "hl7v24", "HL7 v2.4");
        services.AddHL7ReferencePlugin("2.5", "hl7v25", "HL7 v2.5");
        services.AddHL7ReferencePlugin("2.5.1", "hl7v251", "HL7 v2.5.1");
        
        return services;
    }

    /// <summary>
    /// Registers an HL7 reference plugin for a specific version.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="version">HL7 version (e.g., "2.3", "2.4")</param>
    /// <param name="standardId">Standard identifier (e.g., "hl7v23")</param>
    /// <param name="standardName">Display name (e.g., "HL7 v2.3")</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddHL7ReferencePlugin(
        this IServiceCollection services,
        string version,
        string standardId,
        string standardName)
    {
        var config = new Infrastructure.Reference.HL7VersionConfig(
            version, standardId, standardName, standardId);
        
        services.AddScoped<IStandardReferencePlugin>(provider =>
            new Infrastructure.Reference.JsonHL7ReferencePlugin(
                config,
                provider.GetRequiredService<ILogger<Infrastructure.Reference.JsonHL7ReferencePlugin>>(),
                provider.GetRequiredService<IMemoryCache>()));
        
        return services;
    }

    /// <summary>
    /// Registers the constraint resolution system with all plugins and services.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddConstraintResolution(this IServiceCollection services)
    {
        // Register core constraint resolver
        services.AddScoped<IConstraintResolver, ConstraintResolver>();

        // Register constraint resolver plugins
        services.AddScoped<IConstraintResolverPlugin, HL7ConstraintResolverPlugin>();

        // TODO: Add FHIR and NCPDP plugins when implemented
        // services.AddScoped<IConstraintResolverPlugin, FHIRConstraintResolverPlugin>();
        // services.AddScoped<IConstraintResolverPlugin, NCPDPConstraintResolverPlugin>();

        return services;
    }

}