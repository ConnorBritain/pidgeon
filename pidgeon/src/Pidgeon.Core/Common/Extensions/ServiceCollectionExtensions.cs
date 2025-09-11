// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Infrastructure.Registry;
using Pidgeon.Core.Application.Services.Generation;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Infrastructure.Standards.HL7.v23;

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