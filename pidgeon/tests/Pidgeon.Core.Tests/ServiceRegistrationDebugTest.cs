// Debug test to understand service registration
using Microsoft.Extensions.DependencyInjection;
using Pidgeon.Core.Extensions;
using Xunit;
using System.Reflection;

namespace Pidgeon.Core.Tests;

public class ServiceRegistrationDebugTest
{
    [Fact]
    public void Debug_Convention_Registration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Debug what types are found by convention
        var coreAssembly = Assembly.GetAssembly(typeof(ServiceRegistrationExtensions))!;
        var serviceTypes = coreAssembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          type.Name.EndsWith("Service") &&
                          type.Namespace?.Contains("Application.Services") == true)
            .ToList();
            
        var serviceDebug = string.Join("\n", serviceTypes.Select(t => 
            $"{t.Name} in {t.Namespace} -> Interface: {string.Join(",", t.GetInterfaces().Where(i => i.Name.StartsWith("I")).Select(i => i.Name))}"));
            
        services.AddPidgeonCore();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Get all registered services
        var registrations = services
            .Where(s => s.ServiceType.FullName?.Contains("Configuration") == true)
            .Select(s => $"{s.ServiceType.Name} -> {s.ImplementationType?.Name}")
            .ToList();
            
        // This will show what was actually registered
        var debug = string.Join("\n", registrations);
        
        Assert.True(false, $"Found Types:\n{serviceDebug}\n\nRegistered Services:\n{debug}");
    }
}