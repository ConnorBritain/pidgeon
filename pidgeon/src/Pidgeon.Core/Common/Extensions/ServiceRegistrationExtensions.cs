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
        
        // Find all plugin types by convention
        var pluginTypes = coreAssembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          type.Name.EndsWith("Plugin") &&
                          type.Namespace?.Contains("Standards") == true)
            .ToList();

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
        
        return services;
    }

    /// <summary>
    /// Registers services that follow the interface/implementation convention.
    /// Finds classes that implement interfaces with matching names (e.g., MessageService implements IMessageService).
    /// </summary>
    private static void RegisterServicesByConvention(IServiceCollection services, Assembly assembly)
    {
        var serviceTypes = assembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          type.Name.EndsWith("Service") &&
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