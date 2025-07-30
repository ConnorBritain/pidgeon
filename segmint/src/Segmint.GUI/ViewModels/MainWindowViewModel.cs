// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Segmint.Core.Configuration.Inference;
using Segmint.GUI.Services;

namespace Segmint.GUI.ViewModels;

/// <summary>
/// Main window view model handling navigation and global application state.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly INavigationService _navigationService;
    private readonly IConfigurationService _configurationService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private int _configurationCount = 0;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _currentViewTitle = "Configuration Management";

    public bool CanGoBack => _navigationService.CanGoBack;
    public bool CanGoForward => _navigationService.CanGoForward;

    public ObservableCollection<NavigationItem> NavigationHistory { get; } = new();

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        INavigationService navigationService,
        IConfigurationService configurationService,
        IDialogService dialogService)
    {
        _logger = logger;
        _navigationService = navigationService;
        _configurationService = configurationService;
        _dialogService = dialogService;

        // Subscribe to navigation events
        _navigationService.Navigated += OnNavigated;

        // Initialize
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading configurations...";

            await RefreshConfigurationCountAsync();
            
            StatusMessage = ConfigurationCount > 0 
                ? $"Ready - {ConfigurationCount} configurations loaded" 
                : "Ready - No configurations found";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MainWindowViewModel");
            StatusMessage = "Error loading application data";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshConfigurationCountAsync()
    {
        try
        {
            var configurations = await _configurationService.ListConfigurations();
            ConfigurationCount = System.Linq.Enumerable.Count(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh configuration count");
            ConfigurationCount = 0;
        }
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        // Update navigation history
        NavigationHistory.Clear();
        foreach (var entry in _navigationService.History)
        {
            NavigationHistory.Add(new NavigationItem
            {
                DisplayName = entry.DisplayName,
                ViewType = entry.ViewType,
                NavigationTime = entry.NavigationTime
            });
        }

        // Update current view title
        CurrentViewTitle = e.ViewType.Name.Replace("View", "").Replace("Page", "");

        // Notify property changes for navigation state
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Refreshing data...";

            await RefreshConfigurationCountAsync();
            
            StatusMessage = "Data refreshed successfully";
            
            // Reset status message after delay
            await Task.Delay(2000);
            if (!IsLoading)
            {
                StatusMessage = "Ready";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh data");
            StatusMessage = "Error refreshing data";
            await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NavigateBack()
    {
        if (_navigationService.CanGoBack)
        {
            _navigationService.GoBack();
        }
    }

    [RelayCommand]
    private void NavigateForward()
    {
        if (_navigationService.CanGoForward)
        {
            _navigationService.GoForward();
        }
    }

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        var aboutMessage = """
            Segmint HL7 Generator Professional
            Version 2.0.0
            
            A comprehensive tool for managing vendor-specific HL7 configurations 
            and generating compliant healthcare messages.
            
            Features:
            • Configuration inference from sample messages
            • Vendor-specific pattern detection
            • Message validation and conformance scoring
            • Professional GUI with modern theming
            
            Copyright © 2025 Connor England
            Licensed under Mozilla Public License 2.0
            """;

        await _dialogService.ShowInfoAsync("About Segmint", aboutMessage);
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        
        // Log significant property changes
        if (e.PropertyName == nameof(StatusMessage))
        {
            _logger.LogDebug("Status message changed to: {StatusMessage}", StatusMessage);
        }
    }
}

/// <summary>
/// Navigation item for the history display.
/// </summary>
public class NavigationItem
{
    public string DisplayName { get; set; } = "";
    public Type ViewType { get; set; } = typeof(object);
    public DateTime NavigationTime { get; set; }
}