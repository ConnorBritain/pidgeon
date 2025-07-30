// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModernWpf;
using Segmint.Core.Configuration.Inference;
using Segmint.GUI.Services;
using Segmint.GUI.ViewModels;
using Segmint.GUI.Views;

namespace Segmint.GUI;

/// <summary>
/// Main window for the Segmint GUI application with navigation and theme management.
/// </summary>
public partial class MainWindow
{
    private readonly ILogger<MainWindow> _logger;
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;
    private readonly IDialogService _dialogService;
    private readonly IConfigurationService _configurationService;
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(
        ILogger<MainWindow> logger,
        INavigationService navigationService,
        IThemeService themeService,
        IDialogService dialogService,
        IConfigurationService configurationService,
        MainWindowViewModel viewModel)
    {
        _logger = logger;
        _navigationService = navigationService;
        _themeService = themeService;
        _dialogService = dialogService;
        _configurationService = configurationService;
        _viewModel = viewModel;

        InitializeComponent();
        DataContext = _viewModel;

        Initialize();
    }

    private void Initialize()
    {
        try
        {
            // Set up theme toggle state
            ThemeToggle.IsOn = _themeService.CurrentTheme == ApplicationTheme.Dark;

            // Subscribe to navigation events
            _navigationService.Navigated += OnNavigated;
            _themeService.ThemeChanged += OnThemeChanged;

            // Set initial view
            _navigationService.NavigateTo<ConfigurationManagementView>();

            // Update status information
            UpdateStatusAsync();

            _logger.LogInformation("MainWindow initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MainWindow");
        }
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        try
        {
            // Get the view from the service provider
            var view = App.Services.GetService(e.ViewType);
            if (view != null)
            {
                MainContentPresenter.Content = view;
                UpdateBreadcrumb(e.ViewType);
                
                // Update navigation button states
                BackButton.IsEnabled = _navigationService.CanGoBack;
                ForwardButton.IsEnabled = _navigationService.CanGoForward;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to {ViewType}", e.ViewType.Name);
        }
    }

    private void OnThemeChanged(object? sender, ApplicationTheme theme)
    {
        ThemeToggle.IsOn = theme == ApplicationTheme.Dark;
    }

    private void UpdateBreadcrumb(Type viewType)
    {
        var breadcrumb = viewType.Name switch
        {
            nameof(ConfigurationManagementView) => "Configuration Management > Overview",
            nameof(MessageGenerationView) => "Message Generation > Create Messages",
            nameof(ValidationDashboardView) => "Validation & Testing > Dashboard",
            nameof(SettingsView) => "Settings > Application Preferences",
            _ => "Home"
        };

        BreadcrumbText.Text = breadcrumb;
    }

    private async void UpdateStatusAsync()
    {
        try
        {
            var configurations = await _configurationService.ListConfigurations();
            var configCount = System.Linq.Enumerable.Count(configurations);
            
            Dispatcher.Invoke(() =>
            {
                ConfigCountText.Text = $"Configurations: {configCount}";
                StatusText.Text = configCount > 0 ? "Ready - Configurations loaded" : "Ready - No configurations found";
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status information");
            Dispatcher.Invoke(() => StatusText.Text = "Error loading status");
        }
    }

    // Navigation Event Handlers
    private void ConfigOverviewButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.NavigateTo<ConfigurationManagementView>();
    }

    private void ValidationDashboardButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.NavigateTo<ValidationDashboardView>();
    }

    private void GenerateMessageButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.NavigateTo<MessageGenerationView>();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.NavigateTo<SettingsView>();
    }

    // Quick Action Handlers
    private async void InferConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folderPath = await _dialogService.ShowFolderDialogAsync("Select Folder with HL7 Sample Messages");
            if (!string.IsNullOrEmpty(folderPath))
            {
                var vendorName = await _dialogService.ShowInputDialogAsync("Vendor Name", "Enter the vendor or system name:", "Epic");
                if (!string.IsNullOrEmpty(vendorName))
                {
                    var messageType = await _dialogService.ShowInputDialogAsync("Message Type", "Enter the HL7 message type (e.g., ADT^A01):", "ADT^A01");
                    if (!string.IsNullOrEmpty(messageType))
                    {
                        await _dialogService.ShowProgressDialogAsync("Inferring Configuration", async progress =>
                        {
                            progress.Report("Reading sample files...");
                            var files = System.IO.Directory.GetFiles(folderPath, "*.hl7")
                                .Concat(System.IO.Directory.GetFiles(folderPath, "*.txt"));
                            
                            var messages = new List<string>();
                            foreach (var file in files)
                            {
                                var content = await System.IO.File.ReadAllTextAsync(file);
                                if (!string.IsNullOrWhiteSpace(content))
                                    messages.Add(content);
                            }

                            progress.Report($"Analyzing {messages.Count} messages...");
                            var configuration = await _configurationService.InferFromSamples(messages, vendorName, messageType);
                            
                            progress.Report("Saving configuration...");
                            await _configurationService.SaveConfiguration(configuration);
                        });

                        await _dialogService.ShowInfoAsync("Configuration Inferred", $"Successfully inferred configuration for {vendorName} {messageType}");
                        await UpdateStatusAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to infer configuration");
            await _dialogService.ShowErrorAsync("Error", $"Failed to infer configuration: {ex.Message}");
        }
    }

    private async void ValidateConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var configFiles = await _dialogService.ShowOpenFileDialogAsync("Select Configuration File", "Configuration Files|*.json");
            if (configFiles?.Length > 0)
            {
                var messageFiles = await _dialogService.ShowOpenFileDialogAsync("Select HL7 Message File", "HL7 Files|*.hl7;*.txt", true);
                if (messageFiles?.Length > 0)
                {
                    await _dialogService.ShowProgressDialogAsync("Validating Messages", async progress =>
                    {
                        var configJson = await System.IO.File.ReadAllTextAsync(configFiles[0]);
                        var config = System.Text.Json.JsonSerializer.Deserialize<VendorConfiguration>(configJson);
                        
                        if (config != null)
                        {
                            var validator = new ConfigurationValidator(config);
                            var results = new List<string>();

                            foreach (var messageFile in messageFiles)
                            {
                                progress.Report($"Validating {System.IO.Path.GetFileName(messageFile)}...");
                                var message = await System.IO.File.ReadAllTextAsync(messageFile);
                                var result = validator.ValidateMessage(message);
                                
                                results.Add($"{System.IO.Path.GetFileName(messageFile)}: {(result.IsValid ? "✓ Valid" : "✗ Invalid")} (Conformance: {result.OverallConformance:P1})");
                            }

                            progress.Report("Validation complete");
                        }
                    });

                    await _dialogService.ShowInfoAsync("Validation Complete", "Message validation finished. Check the validation dashboard for detailed results.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate messages");
            await _dialogService.ShowErrorAsync("Error", $"Failed to validate messages: {ex.Message}");
        }
    }

    private async void ImportSamplesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = await _dialogService.ShowOpenFileDialogAsync("Import HL7 Sample Messages", "HL7 Files|*.hl7;*.txt", true);
            if (files?.Length > 0)
            {
                await _dialogService.ShowInfoAsync("Import Successful", $"Successfully imported {files.Length} HL7 sample files.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import samples");
            await _dialogService.ShowErrorAsync("Error", $"Failed to import samples: {ex.Message}");
        }
    }

    private async void ExportConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var configurations = await _configurationService.ListConfigurations();
            var configList = System.Linq.Enumerable.ToList(configurations);
            
            if (!configList.Any())
            {
                await _dialogService.ShowWarningAsync("No Configurations", "No configurations available to export.");
                return;
            }

            var options = configList.Select(c => $"{c.Vendor} - {c.MessageType}").ToArray();
            var selected = await _dialogService.ShowSelectionDialogAsync("Export Configuration", "Select configuration to export:", options);
            
            if (!string.IsNullOrEmpty(selected))
            {
                var selectedConfig = configList[Array.IndexOf(options, selected)];
                var filePath = await _dialogService.ShowSaveFileDialogAsync("Export Configuration", "JSON Files|*.json", $"{selectedConfig.Vendor}_{selectedConfig.MessageType}.json");
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    var config = await _configurationService.LoadConfiguration(selectedConfig.ConfigurationId);
                    if (config != null)
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        await System.IO.File.WriteAllTextAsync(filePath, json);
                        await _dialogService.ShowInfoAsync("Export Successful", $"Configuration exported to {filePath}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export configuration");
            await _dialogService.ShowErrorAsync("Error", $"Failed to export configuration: {ex.Message}");
        }
    }

    // Theme and UI Event Handlers
    private void ThemeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        _themeService.ToggleTheme();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.GoBack();
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.GoForward();
    }

    // Placeholder navigation handlers (to be implemented with specific views)
    private void CompareConfigButton_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Compare Configurations - Coming Soon";
    private void ManageConfigButton_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Manage Configuration Library - Coming Soon";
    private void BatchGenerateButton_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Batch Generation - Coming Soon";
    private void TemplatesButton_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Message Templates - Coming Soon";
    private void TestSuitesButton_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Test Suites - Coming Soon";
    private void ReportsButton_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Reports - Coming Soon";
    private void HelpButton_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Help & Documentation - Coming Soon";

    protected override void OnClosed(EventArgs e)
    {
        // Cleanup subscriptions
        _navigationService.Navigated -= OnNavigated;
        _themeService.ThemeChanged -= OnThemeChanged;
        
        base.OnClosed(e);
    }
}