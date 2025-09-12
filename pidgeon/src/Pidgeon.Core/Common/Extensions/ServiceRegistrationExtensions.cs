// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Pidgeon.Core.Extensions;

/// <summary>
/// Convention-based service registration for Pidgeon Core services.
/// Eliminates manual AddScoped registration duplication.
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers all Pidgeon Core services using convention-based discovery.
    /// Scans assemblies for service implementations and registers them automatically.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddPidgeonCoreServices(this IServiceCollection services)
    {
        var coreAssembly = Assembly.GetAssembly(typeof(ServiceRegistrationExtensions))!;
        
        // Register services by convention
        RegisterServicesByConvention(services, coreAssembly);
        
        // Register adapters by convention
        RegisterAdaptersByConvention(services, coreAssembly);
        
        // Register de-identification services
        services.AddDeIdentificationServices();
        
        // Register AI services
        services.AddAIServices();
        
        return services;
    }

    /// <summary>
    /// Registers all standard configuration plugins using convention-based discovery.
    /// Scans for plugin implementations and registers them automatically.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddStandardConfigurationPlugins(this IServiceCollection services)
    {
        var coreAssembly = Assembly.GetAssembly(typeof(ServiceRegistrationExtensions))!;
        
        // Find all plugin types by convention in Core assembly
        var pluginTypes = coreAssembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          type.Name.EndsWith("Plugin") &&
                          type.Namespace?.Contains("Standards") == true)
            .ToList();

        // Register plugins from Core assembly
        RegisterPluginTypes(services, pluginTypes);
        
        return services;
    }

    /// <summary>
    /// Registers standard vendor plugins using convention-based discovery.
    /// Extends plugin registration to include vendor-specific plugins from Infrastructure namespace.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddStandardVendorPlugins(this IServiceCollection services)
    {
        var coreAssembly = Assembly.GetAssembly(typeof(ServiceRegistrationExtensions))!;
        
        // Find vendor plugin types by convention in Core assembly Infrastructure namespace
        var vendorPluginTypes = coreAssembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          type.Name.EndsWith("VendorPlugin") &&
                          type.Namespace?.Contains("Infrastructure.Standards") == true)
            .ToList();

        // Register vendor plugins from Core assembly
        RegisterPluginTypes(services, vendorPluginTypes);
        
        return services;
    }

    /// <summary>
    /// Registers a collection of plugin types using the standard plugin registration pattern.
    /// </summary>
    private static void RegisterPluginTypes(IServiceCollection services, IEnumerable<Type> pluginTypes)
    {
        foreach (var pluginType in pluginTypes)
        {
            // Register the concrete plugin
            services.AddScoped(pluginType);
            
            // Register for all interfaces it implements
            var interfaces = pluginType.GetInterfaces()
                .Where(i => i.Name.StartsWith("I") && i != typeof(IDisposable))
                .ToList();
                
            foreach (var interfaceType in interfaces)
            {
                services.AddScoped(interfaceType, provider => provider.GetRequiredService(pluginType));
            }
        }
    }

    /// <summary>
    /// Registers all message generation plugins using convention-based discovery.
    /// Scans for IMessageGenerationPlugin implementations and registers them automatically.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMessageGenerationPlugins(this IServiceCollection services)
    {
        var coreAssembly = Assembly.GetAssembly(typeof(ServiceRegistrationExtensions))!;
        
        // Find all message generation plugin types by convention
        var generationPluginTypes = coreAssembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          type.Name.EndsWith("MessageGenerationPlugin") &&
                          type.Namespace?.Contains("Generation.Plugins") == true)
            .ToList();

        foreach (var pluginType in generationPluginTypes)
        {
            // Register the concrete plugin
            services.AddScoped(pluginType);
            
            // Register as IMessageGenerationPlugin interface
            var generationInterface = pluginType.GetInterfaces()
                .FirstOrDefault(i => i.Name == "IMessageGenerationPlugin");
                
            if (generationInterface != null)
            {
                services.AddScoped(generationInterface, provider => provider.GetRequiredService(pluginType));
            }
        }
        
        return services;
    }

    /// <summary>
    /// Registers all de-identification services using convention-based discovery.
    /// Scans for de-identification implementations and registers them automatically.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddDeIdentificationServices(this IServiceCollection services)
    {
        var coreAssembly = Assembly.GetAssembly(typeof(ServiceRegistrationExtensions))!;
        
        // Find all de-identification service types by convention
        var deidentTypes = coreAssembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          (type.Name.Contains("DeIdentif") || type.Name.Contains("PhiPattern") || type.Name.Contains("SafeHarbor")) &&
                          type.Namespace?.Contains("Infrastructure.Standards") == true)
            .ToList();

        foreach (var deidentType in deidentTypes)
        {
            // Register the concrete de-identifier
            services.AddScoped(deidentType);
        }
        
        // Find all infrastructure message composers and related services
        var infrastructureTypes = coreAssembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          (type.Name.EndsWith("MessageComposer") || type.Name.EndsWith("SegmentBuilder")) &&
                          type.Namespace?.Contains("Infrastructure.Standards") == true)
            .ToList();

        foreach (var infrastructureType in infrastructureTypes)
        {
            // Register the concrete infrastructure service
            services.AddScoped(infrastructureType);
        }
        
        // Register services that don't have interfaces (internal helpers)
        var helperTypes = new[] 
        { 
            typeof(Application.Services.DeIdentification.ConsistencyManager),
            typeof(Application.Services.DeIdentification.ComplianceValidationService),
            typeof(Application.Services.DeIdentification.AuditReportService),
            typeof(Application.Services.DeIdentification.ResourceEstimationService)
        };
        
        foreach (var helperType in helperTypes)
        {
            services.AddScoped(helperType);
        }
        
        return services;
    }

    /// <summary>
    /// Registers all AI services using convention-based discovery.
    /// Scans for AI provider implementations and registers them automatically.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddAIServices(this IServiceCollection services)
    {
        var coreAssembly = Assembly.GetAssembly(typeof(ServiceRegistrationExtensions))!;
        
        // Find all AI service types by convention
        var aiServiceTypes = coreAssembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          (type.Name.Contains("Model") || type.Name.Contains("AI") || type.Name.Contains("Intelligence")) &&
                          type.Namespace?.Contains("Application.Services.Intelligence") == true)
            .ToList();

        foreach (var serviceType in aiServiceTypes)
        {
            // Register the concrete service
            services.AddScoped(serviceType);
            
            // Register for all interfaces it implements
            var interfaces = serviceType.GetInterfaces()
                .Where(i => i.Namespace?.Contains("Application.Interfaces.Intelligence") == true)
                .ToList();
                
            foreach (var interfaceType in interfaces)
            {
                services.AddScoped(interfaceType, provider => provider.GetRequiredService(serviceType));
            }
        }
        
        return services;
    }

    /// <summary>
    /// Registers services that follow the interface/implementation convention.
    /// Finds classes that implement interfaces with matching names (e.g., MessageService implements IMessageService).
    /// Enhanced to handle multi-interface services automatically.
    /// </summary>
    private static void RegisterServicesByConvention(IServiceCollection services, Assembly assembly)
    {
        var serviceTypes = assembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          (type.Name.EndsWith("Service") || type.Name.EndsWith("Catalog") || type.Name.EndsWith("Repository") || type.Name.EndsWith("Manager") || type.Name.EndsWith("Orchestrator") || type.Name.EndsWith("Engine") || type.Name.EndsWith("Detector")) &&
                          type.Namespace?.Contains("Application.Services") == true)
            .ToList();

        foreach (var serviceType in serviceTypes)
        {
            // Find the primary interface (usually I + ClassName)
            var primaryInterface = serviceType.GetInterfaces()
                .FirstOrDefault(i => i.Name == $"I{serviceType.Name}");
                
            if (primaryInterface != null)
            {
                services.AddScoped(primaryInterface, serviceType);
            }
            
            // Handle multi-interface services (e.g., ConfigurationCatalog implementing IConfigurationAnalyzer, IConfigurationQuery, etc.)
            if (serviceType.Name.EndsWith("Catalog") || serviceType.Name.EndsWith("Repository") || serviceType.Name.EndsWith("Manager") || serviceType.Name.EndsWith("Orchestrator"))
            {
                var applicationInterfaces = serviceType.GetInterfaces()
                    .Where(i => i.Namespace?.Contains("Application.Interfaces") == true &&
                               i.Name.StartsWith("I"))
                    .ToList();
                    
                foreach (var interfaceType in applicationInterfaces)
                {
                    // Only skip if the primary interface was actually registered
                    if (interfaceType.Name == $"I{serviceType.Name}" && primaryInterface != null)
                    {
                        continue; // Skip because already registered above
                    }
                    
                    services.AddScoped(interfaceType, serviceType);
                }
            }
        }
    }

    /// <summary>
    /// Registers adapter implementations that follow the interface/implementation convention.
    /// Finds classes that implement adapter interfaces.
    /// </summary>
    private static void RegisterAdaptersByConvention(IServiceCollection services, Assembly assembly)
    {
        var adapterTypes = assembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          type.Name.EndsWith("Adapter") &&
                          type.Namespace?.Contains("Adapters") == true)
            .ToList();

        foreach (var adapterType in adapterTypes)
        {
            // Register for all adapter interfaces it implements
            var adapterInterfaces = adapterType.GetInterfaces()
                .Where(i => i.Name.Contains("Adapter"))
                .ToList();
                
            foreach (var interfaceType in adapterInterfaces)
            {
                services.AddScoped(interfaceType, adapterType);
            }
        }
    }
}