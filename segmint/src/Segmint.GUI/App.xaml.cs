// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModernWpf;
// using Segmint.Core.Configuration.Inference;
using Segmint.GUI.Services;
using Segmint.GUI.ViewModels;
using Segmint.GUI.Views;
using Serilog;

namespace Segmint.GUI;

/// <summary>
/// Main application class for Segmint GUI with dependency injection and modern WPF theming.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    public static IServiceProvider Services => ((App)Current)._host?.Services 
        ?? throw new InvalidOperationException("Services not initialized");

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs/segmint-gui.log", rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            _host = CreateHostBuilder(e.Args).Build();

            // Configure ModernWPF theme
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            ThemeManager.Current.AccentColor = Colors.DodgerBlue;

            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            MessageBox.Show($"Application failed to start: {ex.Message}", "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services, context.Configuration);
            });

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Core services
        var configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Segmint", "Configurations");
        services.AddSingleton<IConfigurationService>(provider =>
            new FileBasedConfigurationService(configDirectory, provider.GetRequiredService<ILogger<FileBasedConfigurationService>>()));

        // GUI services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ConfigurationManagementViewModel>();
        services.AddTransient<MessageGenerationViewModel>();
        services.AddTransient<ValidationDashboardViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
        services.AddTransient<ConfigurationManagementView>();
        services.AddTransient<MessageGenerationView>();
        services.AddTransient<ValidationDashboardView>();
        services.AddTransient<SettingsView>();

        // Configuration
        services.Configure<GuiConfiguration>(configuration.GetSection("Gui"));
    }
}

/// <summary>
/// Configuration options for the GUI application.
/// </summary>
public class GuiConfiguration
{
    public string DefaultTheme { get; set; } = "Light";
    public string DefaultConfigurationDirectory { get; set; } = "";
    public bool AutoSaveConfigurations { get; set; } = true;
    public int MaxRecentConfigurations { get; set; } = 10;
    public bool EnableTelemetry { get; set; } = false;
}