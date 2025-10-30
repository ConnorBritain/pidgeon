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
        
        // Register HL7 v2.3 message factory and data providers
        services.AddScoped<IHL7MessageFactory, HL7v23MessageFactory>();
        services.AddHL7DataProviders();

        // Register embedded data sources for free tier realistic data
        services.AddEmbeddedDataSources();

        // Register field value resolver system for session context integration
        services.AddFieldValueResolvers();
        
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

        // Register lock session management system
        services.AddLockSessionManagement();

        // Register subscription management system
        services.AddSubscriptionManagement();

#if ENABLE_AI
        // Register AI intelligence services
        services.AddAIIntelligenceServices();
#endif

        // Register search services for Find command
        services.AddSearchServices();

        // Register procedural analysis engine for enhanced diff analysis
        services.AddProceduralAnalysisEngine();

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
        // TODO: Add these when we have the data files
        // services.AddHL7ReferencePlugin("2.4", "hl7v24", "HL7 v2.4");
        // services.AddHL7ReferencePlugin("2.5", "hl7v25", "HL7 v2.5");
        // services.AddHL7ReferencePlugin("2.5.1", "hl7v251", "HL7 v2.5.1");
        
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

    /// <summary>
    /// Registers the lock session management system with all required services.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddLockSessionManagement(this IServiceCollection services)
    {
        // Register core lock session service
        services.AddScoped<Pidgeon.Core.Application.Interfaces.Configuration.ILockSessionService,
                          Pidgeon.Core.Application.Services.Configuration.LockSessionService>();

        // Register file system storage provider as default
        services.AddScoped<Pidgeon.Core.Application.Interfaces.Configuration.ILockStorageProvider,
                          Pidgeon.Core.Infrastructure.Configuration.FileSystemLockStorageProvider>();

        // Register session export/import service for template marketplace functionality
        services.AddScoped<Pidgeon.Core.Application.Interfaces.Configuration.ISessionExportService,
                          Pidgeon.Core.Application.Services.Configuration.SessionExportService>();

        return services;
    }

    /// <summary>
    /// Registers the subscription management system with all required services.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSubscriptionManagement(this IServiceCollection services)
    {
        // Register core subscription service
        services.AddScoped<Pidgeon.Core.Application.Interfaces.Subscription.ISubscriptionService,
                          Pidgeon.Core.Application.Services.Subscription.SubscriptionService>();

        return services;
    }

#if ENABLE_AI
    /// <summary>
    /// Registers AI intelligence services for message modification and analysis.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddAIIntelligenceServices(this IServiceCollection services)
    {
        // Register AI message modification service
        services.AddScoped<Pidgeon.Core.Application.Interfaces.Intelligence.IAIMessageModificationService,
                          Pidgeon.Core.Application.Services.Intelligence.AIMessageModificationService>();

        // Register field intelligence service (if not already registered by convention)
        services.AddScoped<Pidgeon.Core.Application.Services.Intelligence.FieldIntelligenceService>();

        return services;
    }
#endif

    /// <summary>
    /// Registers search services for the Find command functionality.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSearchServices(this IServiceCollection services)
    {
        // Register field discovery service
        services.AddScoped<Pidgeon.Core.Application.Interfaces.Search.IFieldDiscoveryService,
                          Pidgeon.Core.Application.Services.Search.FieldDiscoveryService>();

        // Register message search service
        services.AddScoped<Pidgeon.Core.Application.Interfaces.Search.IMessageSearchService,
                          Pidgeon.Core.Application.Services.Search.MessageSearchService>();

        return services;
    }

    /// <summary>
    /// Registers the procedural analysis engine for enhanced diff analysis.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddProceduralAnalysisEngine(this IServiceCollection services)
    {
        // Register the procedural analysis engine
        services.AddScoped<Pidgeon.Core.Application.Interfaces.Comparison.IProceduralAnalysisEngine,
                          Pidgeon.Core.Application.Services.Comparison.ProceduralAnalysisEngine>();

        return services;
    }

    /// <summary>
    /// Registers HL7 data providers for completely data-driven message generation.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddHL7DataProviders(this IServiceCollection services)
    {
        // Register all HL7 data providers - load JSON from Pidgeon.Data assembly
        services.AddScoped<IHL7TriggerEventProvider, HL7TriggerEventProvider>();
        services.AddScoped<IHL7SegmentProvider, HL7SegmentProvider>();
        services.AddScoped<IHL7DataTypeProvider, HL7DataTypeProvider>();
        services.AddScoped<IHL7TableProvider, HL7TableProvider>();

        // Register HL7MessageComposer for data-driven message composition
        services.AddScoped<HL7MessageComposer>();

        return services;
    }

    /// <summary>
    /// Registers the field value resolver system for priority-based field resolution.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddFieldValueResolvers(this IServiceCollection services)
    {
        // Register the main resolver service
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.IFieldValueResolverService,
                          Pidgeon.Core.Services.FieldValueResolvers.FieldValueResolverService>();

        // Register individual resolvers in priority order (highest to lowest)
        // Priority 100: Session context (user-set values override all)
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.IFieldValueResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.SemanticPathResolver>();

        // Priority 90: HL7 specific fields (MSH fields, message structure)
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.IFieldValueResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.HL7SpecificFieldResolver>();

        // Priority 85: HL7 table-driven coded values (uses official HL7 tables)
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.IFieldValueResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.HL7TableFieldResolver>();

        // Priority 82: Composite-aware coded element resolver (CE/CF semantic coherence)
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.ICompositeAwareResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.CodedElementResolver>();

        // Priority 78: Identifier coherence resolver (CX/EI/XCN/XON with check digits)
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.ICompositeAwareResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.IdentifierCoherenceResolver>();

        // Priority 76: Range coherence resolver (CQ/CM_RANGE/DR with coherent values)
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.ICompositeAwareResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.RangeCoherenceResolver>();

        // Priority 80: Demographic tables (realistic patient data)
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.IFieldValueResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.DemographicFieldResolver>();

        // Priority 75: Clinical data (medications, diagnoses, lab tests)
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.IFieldValueResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.MedicationFieldResolver>();

        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.IFieldValueResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.DiagnosisFieldResolver>();

        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.IFieldValueResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.LabTestFieldResolver>();

        // Priority 75: Identifier fields (patient IDs, account numbers, license numbers)
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.IFieldValueResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.IdentifierFieldResolver>();

        // Priority 75: Contact information (phone numbers, emails, fax)
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.IFieldValueResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.ContactFieldResolver>();

        // Priority 70: HL7 coded values (language codes, status codes, types)
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.IFieldValueResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.HL7CodedValueResolver>();

        // Priority 10: Smart random fallback (always provides value)
        services.AddScoped<Pidgeon.Core.Services.FieldValueResolvers.IFieldValueResolver,
                          Pidgeon.Core.Services.FieldValueResolvers.SmartRandomResolver>();

        return services;
    }

    /// <summary>
    /// Registers embedded data sources for free tier realistic healthcare data.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddEmbeddedDataSources(this IServiceCollection services)
    {
        // Register demographic data source (names and addresses)
        services.AddSingleton<Pidgeon.Core.Application.Interfaces.Data.IDemographicDataSource,
                             Pidgeon.Core.Infrastructure.Data.EmbeddedDemographicDataSource>();

        // Register medication data source
        services.AddSingleton<Pidgeon.Core.Application.Interfaces.Data.IMedicationDataSource,
                             Pidgeon.Core.Infrastructure.Data.EmbeddedMedicationDataSource>();

        // Register diagnosis data source
        services.AddSingleton<Pidgeon.Core.Application.Interfaces.Data.IDiagnosisDataSource,
                             Pidgeon.Core.Infrastructure.Data.EmbeddedDiagnosisDataSource>();

        // Register lab test data source
        services.AddSingleton<Pidgeon.Core.Application.Interfaces.Data.ILabTestDataSource,
                             Pidgeon.Core.Infrastructure.Data.EmbeddedLabTestDataSource>();

        // Register vaccine data source
        services.AddSingleton<Pidgeon.Core.Application.Interfaces.Data.IVaccineDataSource,
                             Pidgeon.Core.Infrastructure.Data.EmbeddedVaccineDataSource>();

        return services;
    }

}