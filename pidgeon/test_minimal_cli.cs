using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pidgeon.Core.Extensions;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.CLI.Commands;

Console.WriteLine("Testing minimal CLI setup...");

// Mimic what the CLI does but without Host.CreateDefaultBuilder
var services = new ServiceCollection();
services.AddLogging();
services.AddPidgeonCore(); // This is what works in tests
services.AddScoped<ConfigCommand>(); // Register ConfigCommand directly

var serviceProvider = services.BuildServiceProvider();

try 
{
    var configCommand = serviceProvider.GetRequiredService<ConfigCommand>();
    Console.WriteLine($"✅ ConfigCommand created successfully: {configCommand.GetType().Name}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ ConfigCommand failed: {ex.Message}");
    
    // Try to resolve the dependency directly
    try
    {
        var analyzer = serviceProvider.GetRequiredService<IConfigurationAnalyzer>();
        Console.WriteLine($"✅ But IConfigurationAnalyzer resolves fine: {analyzer.GetType().Name}");
    }
    catch (Exception ex2)
    {
        Console.WriteLine($"❌ IConfigurationAnalyzer also fails: {ex2.Message}");
    }
}