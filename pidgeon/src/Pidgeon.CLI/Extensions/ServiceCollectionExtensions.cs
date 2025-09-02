// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;
using Pidgeon.CLI.Commands;
using System.Reflection;

namespace Pidgeon.CLI.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register CLI services using conventions.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all CLI commands using convention-based discovery.
    /// </summary>
    public static IServiceCollection AddCliCommands(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var commandTypes = assembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          type.IsSubclassOf(typeof(CommandBuilderBase)))
            .ToList();

        foreach (var commandType in commandTypes)
        {
            services.AddScoped(commandType);
        }

        return services;
    }
}