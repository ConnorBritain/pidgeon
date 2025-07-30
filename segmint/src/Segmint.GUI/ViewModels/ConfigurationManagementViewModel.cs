// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Segmint.Core.Configuration.Inference;
using Segmint.GUI.Services;

namespace Segmint.GUI.ViewModels;

/// <summary>
/// View model for configuration management operations.
/// </summary>
public partial class ConfigurationManagementViewModel : ObservableObject
{
    private readonly ILogger<ConfigurationManagementViewModel> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _selectedVendorFilter = "All";

    [ObservableProperty]
    private string _selectedMessageTypeFilter = "All";

    [ObservableProperty]
    private ConfigurationSummaryViewModel? _selectedConfiguration;

    public ObservableCollection<ConfigurationSummaryViewModel> Configurations { get; } = new();
    public ObservableCollection<ConfigurationSummaryViewModel> FilteredConfigurations { get; } = new();
    public ObservableCollection<string> VendorFilters { get; } = new() { "All" };
    public ObservableCollection<string> MessageTypeFilters { get; } = new() { "All" };

    public ConfigurationManagementViewModel(
        ILogger<ConfigurationManagementViewModel> logger,
        IConfigurationService configurationService,
        IDialogService dialogService)
    {
        _logger = logger;
        _configurationService = configurationService;
        _dialogService = dialogService;

        // Initialize
        _ = LoadConfigurationsAsync();
    }

    [RelayCommand]
    private async Task LoadConfigurationsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading configurations...";

            var configurations = await _configurationService.ListConfigurations();
            var configList = configurations.ToList();

            Configurations.Clear();
            VendorFilters.Clear();
            MessageTypeFilters.Clear();

            VendorFilters.Add("All");
            MessageTypeFilters.Add("All");

            foreach (var config in configList)
            {
                var viewModel = new ConfigurationSummaryViewModel(config);
                Configurations.Add(viewModel);

                // Add to filter collections
                if (!VendorFilters.Contains(config.Vendor))
                    VendorFilters.Add(config.Vendor);

                if (!MessageTypeFilters.Contains(config.MessageType))
                    MessageTypeFilters.Add(config.MessageType);
            }

            ApplyFilters();
            StatusMessage = $"Loaded {Configurations.Count} configurations";

            _logger.LogInformation("Loaded {ConfigCount} configurations", Configurations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configurations");
            StatusMessage = "Error loading configurations";
            await _dialogService.ShowErrorAsync("Load Error", $"Failed to load configurations: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateConfigurationAsync()
    {
        try
        {
            var folderPath = await _dialogService.ShowFolderDialogAsync("Select Folder with HL7 Sample Messages");
            if (string.IsNullOrEmpty(folderPath)) return;

            var vendorName = await _dialogService.ShowInputDialogAsync("Vendor Name", "Enter the vendor or system name:", "");
            if (string.IsNullOrEmpty(vendorName)) return;

            var messageType = await _dialogService.ShowInputDialogAsync("Message Type", "Enter the HL7 message type (e.g., ADT^A01):", "");
            if (string.IsNullOrEmpty(messageType)) return;

            await _dialogService.ShowProgressDialogAsync("Creating Configuration", async progress =>
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

            await _dialogService.ShowInfoAsync("Configuration Created", 
                $"Successfully created configuration for {vendorName} {messageType}");

            await LoadConfigurationsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create configuration");
            await _dialogService.ShowErrorAsync("Creation Error", $"Failed to create configuration: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteConfigurationAsync(ConfigurationSummaryViewModel? configuration)
    {
        if (configuration == null) return;

        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Configuration",
                $"Are you sure you want to delete the configuration for {configuration.Vendor} {configuration.MessageType}?",
                "Delete", "Cancel");

            if (!confirmed) return;

            IsLoading = true;
            StatusMessage = "Deleting configuration...";

            var deleted = await _configurationService.DeleteConfiguration(configuration.ConfigurationId);
            if (deleted)
            {
                await _dialogService.ShowInfoAsync("Configuration Deleted", "Configuration deleted successfully");
                await LoadConfigurationsAsync();
            }
            else
            {
                await _dialogService.ShowWarningAsync("Delete Failed", "Failed to delete configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete configuration {ConfigId}", configuration.ConfigurationId);
            await _dialogService.ShowErrorAsync("Delete Error", $"Failed to delete configuration: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportConfigurationAsync(ConfigurationSummaryViewModel? configuration)
    {
        if (configuration == null) return;

        try
        {
            var filePath = await _dialogService.ShowSaveFileDialogAsync(
                "Export Configuration",
                "JSON Files|*.json",
                $"{configuration.Vendor}_{configuration.MessageType}.json");

            if (string.IsNullOrEmpty(filePath)) return;

            IsLoading = true;
            StatusMessage = "Exporting configuration...";

            var config = await _configurationService.LoadConfiguration(configuration.ConfigurationId);
            if (config != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(filePath, json);

                await _dialogService.ShowInfoAsync("Export Successful", $"Configuration exported to {filePath}");
                StatusMessage = "Export completed successfully";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export configuration {ConfigId}", configuration.ConfigurationId);
            await _dialogService.ShowErrorAsync("Export Error", $"Failed to export configuration: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ValidateConfigurationAsync(ConfigurationSummaryViewModel? configuration)
    {
        if (configuration == null) return;

        try
        {
            var messageFiles = await _dialogService.ShowOpenFileDialogAsync(
                "Select HL7 Messages to Validate", 
                "HL7 Files|*.hl7;*.txt", 
                true);

            if (messageFiles?.Length == 0) return;

            await _dialogService.ShowProgressDialogAsync("Validating Messages", async progress =>
            {
                var config = await _configurationService.LoadConfiguration(configuration.ConfigurationId);
                if (config == null) return;

                var validator = new ConfigurationValidator(config);
                var totalResults = new List<ValidationResultSummary>();

                foreach (var messageFile in messageFiles!)
                {
                    progress.Report($"Validating {System.IO.Path.GetFileName(messageFile)}...");
                    var message = await System.IO.File.ReadAllTextAsync(messageFile);
                    var result = validator.ValidateMessage(message);

                    totalResults.Add(new ValidationResultSummary
                    {
                        FileName = System.IO.Path.GetFileName(messageFile),
                        IsValid = result.IsValid,
                        Conformance = result.OverallConformance,
                        DeviationCount = result.Deviations.Count
                    });
                }

                var validCount = totalResults.Count(r => r.IsValid);
                var avgConformance = totalResults.Average(r => r.Conformance);

                progress.Report("Validation complete");

                await _dialogService.ShowInfoAsync("Validation Results",
                    $"Validation completed:\n" +
                    $"• {validCount}/{totalResults.Count} messages passed validation\n" +
                    $"• Average conformance: {avgConformance:P1}\n" +
                    $"• Total deviations: {totalResults.Sum(r => r.DeviationCount)}");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate configuration {ConfigId}", configuration.ConfigurationId);
            await _dialogService.ShowErrorAsync("Validation Error", $"Failed to validate configuration: {ex.Message}");
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedVendorFilterChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedMessageTypeFilterChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        FilteredConfigurations.Clear();

        var filtered = Configurations.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(c =>
                c.Vendor.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                c.MessageType.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (c.Version?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // Apply vendor filter
        if (SelectedVendorFilter != "All")
        {
            filtered = filtered.Where(c => c.Vendor == SelectedVendorFilter);
        }

        // Apply message type filter
        if (SelectedMessageTypeFilter != "All")
        {
            filtered = filtered.Where(c => c.MessageType == SelectedMessageTypeFilter);
        }

        foreach (var item in filtered)
        {
            FilteredConfigurations.Add(item);
        }

        StatusMessage = FilteredConfigurations.Count != Configurations.Count
            ? $"Showing {FilteredConfigurations.Count} of {Configurations.Count} configurations"
            : $"Showing {Configurations.Count} configurations";
    }
}

/// <summary>
/// View model wrapper for configuration summary data.
/// </summary>
public partial class ConfigurationSummaryViewModel : ObservableObject
{
    public string ConfigurationId { get; }
    public string Vendor { get; }
    public string? Version { get; }
    public string MessageType { get; }
    public DateTime CreatedAt { get; }
    public int SampleCount { get; }
    public double Confidence { get; }
    public int SegmentCount { get; }
    public int ValidationRuleCount { get; }

    public string ConfidenceText => $"{Confidence:P1}";
    public string CreatedText => CreatedAt.ToString("MMM dd, yyyy");
    public string DetailsText => $"{SegmentCount} segments, {ValidationRuleCount} rules, {SampleCount} samples";

    public ConfigurationSummaryViewModel(ConfigurationSummary summary)
    {
        ConfigurationId = summary.ConfigurationId;
        Vendor = summary.Vendor;
        Version = summary.Version;
        MessageType = summary.MessageType;
        CreatedAt = summary.CreatedAt;
        SampleCount = summary.SampleCount;
        Confidence = summary.Confidence;
        SegmentCount = summary.SegmentCount;
        ValidationRuleCount = summary.ValidationRuleCount;
    }
}

/// <summary>
/// Summary of validation results for display.
/// </summary>
public class ValidationResultSummary
{
    public string FileName { get; set; } = "";
    public bool IsValid { get; set; }
    public double Conformance { get; set; }
    public int DeviationCount { get; set; }
}