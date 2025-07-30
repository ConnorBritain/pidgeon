// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Segmint.Core.Configuration.Templates;

namespace Segmint.Core.Configuration;

/// <summary>
/// Service for managing Segmint configuration operations.
/// </summary>
public class ConfigurationService
{
    private readonly ConfigurationManager _configurationManager;
    private readonly ILogger<ConfigurationService> _logger;
    private SegmintConfiguration? _currentConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    /// <param name="configurationManager">The configuration manager.</param>
    /// <param name="logger">Logger instance.</param>
    public ConfigurationService(
        ConfigurationManager configurationManager,
        ILogger<ConfigurationService> logger)
    {
        _configurationManager = configurationManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    public SegmintConfiguration CurrentConfiguration => 
        _currentConfiguration ?? _configurationManager.CreateDefault();

    /// <summary>
    /// Loads configuration from a file or creates default if file doesn't exist.
    /// </summary>
    /// <param name="filePath">Path to configuration file.</param>
    /// <param name="createIfMissing">Whether to create default config if file doesn't exist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if configuration was loaded successfully.</returns>
    public async Task<bool> LoadConfigurationAsync(
        string filePath, 
        bool createIfMissing = true, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(filePath))
            {
                _currentConfiguration = await _configurationManager.LoadAsync(filePath, cancellationToken);
                _logger.LogInformation("Configuration loaded from {FilePath}", filePath);
                return true;
            }
            else if (createIfMissing)
            {
                _currentConfiguration = _configurationManager.CreateDefault();
                await _configurationManager.SaveAsync(_currentConfiguration, filePath, cancellationToken);
                _logger.LogInformation("Created default configuration at {FilePath}", filePath);
                return true;
            }
            else
            {
                _logger.LogWarning("Configuration file not found: {FilePath}", filePath);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Saves the current configuration to a file.
    /// </summary>
    /// <param name="filePath">Path where to save the configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if configuration was saved successfully.</returns>
    public async Task<bool> SaveConfigurationAsync(
        string filePath, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _currentConfiguration ?? _configurationManager.CreateDefault();
            await _configurationManager.SaveAsync(config, filePath, cancellationToken);
            _logger.LogInformation("Configuration saved to {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Creates a new configuration from a template.
    /// </summary>
    /// <param name="templateName">The template name.</param>
    /// <param name="setAsCurrent">Whether to set as current configuration.</param>
    /// <returns>The created configuration.</returns>
    public SegmintConfiguration CreateFromTemplate(string templateName, bool setAsCurrent = true)
    {
        try
        {
            var configuration = ConfigurationTemplates.CreateFromTemplate(templateName);
            
            if (setAsCurrent)
            {
                _currentConfiguration = configuration;
                _logger.LogInformation("Set current configuration to {TemplateName} template", templateName);
            }
            
            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create configuration from template {TemplateName}", templateName);
            throw;
        }
    }

    /// <summary>
    /// Creates a new configuration from a template and saves it to a file.
    /// </summary>
    /// <param name="templateName">The template name.</param>
    /// <param name="filePath">Path where to save the configuration.</param>
    /// <param name="setAsCurrent">Whether to set as current configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if configuration was created and saved successfully.</returns>
    public async Task<bool> CreateFromTemplateAsync(
        string templateName,
        string filePath,
        bool setAsCurrent = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = CreateFromTemplate(templateName, setAsCurrent);
            await _configurationManager.SaveAsync(configuration, filePath, cancellationToken);
            _logger.LogInformation("Created {TemplateName} template configuration at {FilePath}", templateName, filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create and save {TemplateName} template to {FilePath}", templateName, filePath);
            return false;
        }
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>List of validation issues.</returns>
    public List<string> ValidateCurrentConfiguration()
    {
        var config = _currentConfiguration ?? _configurationManager.CreateDefault();
        return _configurationManager.ValidateConfiguration(config);
    }

    /// <summary>
    /// Validates a configuration file.
    /// </summary>
    /// <param name="filePath">Path to configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of validation issues.</returns>
    public async Task<List<string>> ValidateConfigurationFileAsync(
        string filePath, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await _configurationManager.LoadAsync(filePath, cancellationToken);
            return _configurationManager.ValidateConfiguration(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate configuration file {FilePath}", filePath);
            return [$"Failed to load configuration: {ex.Message}"];
        }
    }

    /// <summary>
    /// Compares two configuration files.
    /// </summary>
    /// <param name="filePath1">Path to first configuration file.</param>
    /// <param name="filePath2">Path to second configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of differences.</returns>
    public async Task<List<string>> CompareConfigurationFilesAsync(
        string filePath1,
        string filePath2,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config1 = await _configurationManager.LoadAsync(filePath1, cancellationToken);
            var config2 = await _configurationManager.LoadAsync(filePath2, cancellationToken);
            
            return _configurationManager.CompareConfigurations(config1, config2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare configuration files {FilePath1} and {FilePath2}", filePath1, filePath2);
            return [$"Failed to compare configurations: {ex.Message}"];
        }
    }

    /// <summary>
    /// Gets available configuration templates.
    /// </summary>
    /// <returns>List of available template names.</returns>
    public List<string> GetAvailableTemplates()
    {
        return ConfigurationTemplates.GetAvailableTemplates();
    }

    /// <summary>
    /// Sets the current configuration.
    /// </summary>
    /// <param name="configuration">The configuration to set as current.</param>
    public void SetCurrentConfiguration(SegmintConfiguration configuration)
    {
        _currentConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger.LogDebug("Current configuration updated");
    }

    /// <summary>
    /// Resets the current configuration to default.
    /// </summary>
    public void ResetToDefault()
    {
        _currentConfiguration = _configurationManager.CreateDefault();
        _logger.LogInformation("Configuration reset to default");
    }

    /// <summary>
    /// Updates a specific configuration section.
    /// </summary>
    /// <param name="sectionName">The section name (HL7, Validation, Interface, etc.).</param>
    /// <param name="updates">Dictionary of property updates.</param>
    /// <returns>True if updates were applied successfully.</returns>
    public bool UpdateConfigurationSection(string sectionName, Dictionary<string, object> updates)
    {
        try
        {
            var config = _currentConfiguration ?? _configurationManager.CreateDefault();
            
            switch (sectionName.ToLowerInvariant())
            {
                case "hl7":
                    UpdateHL7Configuration(config.HL7, updates);
                    break;
                case "validation":
                    UpdateValidationConfiguration(config.Validation, updates);
                    break;
                case "interface":
                    UpdateInterfaceConfiguration(config.Interface, updates);
                    break;
                case "logging":
                    UpdateLoggingConfiguration(config.Logging, updates);
                    break;
                case "performance":
                    UpdatePerformanceConfiguration(config.Performance, updates);
                    break;
                default:
                    _logger.LogWarning("Unknown configuration section: {SectionName}", sectionName);
                    return false;
            }
            
            _currentConfiguration = config;
            _logger.LogInformation("Updated {SectionName} configuration section", sectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update {SectionName} configuration section", sectionName);
            return false;
        }
    }

    /// <summary>
    /// Gets configuration information as a formatted string.
    /// </summary>
    /// <returns>Configuration summary.</returns>
    public string GetConfigurationSummary()
    {
        var config = _currentConfiguration ?? _configurationManager.CreateDefault();
        var issues = _configurationManager.ValidateConfiguration(config);
        
        return $"""
            Segmint Configuration Summary
            ============================
            HL7 Version: {config.HL7.DefaultVersion}
            Sending Application: {config.HL7.DefaultSendingApplication}
            Processing ID: {config.HL7.DefaultProcessingId}
            Validation Enabled: {string.Join(", ", config.Validation.EnabledTypes)}
            Strict Mode: {config.Validation.StrictMode}
            Interface: {config.Interface.Name} v{config.Interface.Version}
            Supported Messages: {string.Join(", ", config.Interface.SupportedMessageTypes)}
            Log Level: {config.Logging.MinimumLevel}
            Performance Monitoring: {config.Performance.EnablePerformanceMonitoring}
            Validation Issues: {issues.Count}
            """;
    }

    private static void UpdateHL7Configuration(HL7Configuration hl7, Dictionary<string, object> updates)
    {
        foreach (var (key, value) in updates)
        {
            switch (key.ToLowerInvariant())
            {
                case "defaultversion":
                    hl7.DefaultVersion = value.ToString() ?? hl7.DefaultVersion;
                    break;
                case "defaultsendingapplication":
                    hl7.DefaultSendingApplication = value.ToString() ?? hl7.DefaultSendingApplication;
                    break;
                case "defaultprocessingid":
                    hl7.DefaultProcessingId = value.ToString() ?? hl7.DefaultProcessingId;
                    break;
                case "autogeneratecontrolid":
                    if (value is bool autoGen) hl7.AutoGenerateControlId = autoGen;
                    break;
                case "includetimestamps":
                    if (value is bool includeTime) hl7.IncludeTimestamps = includeTime;
                    break;
            }
        }
    }

    private static void UpdateValidationConfiguration(ValidationConfiguration validation, Dictionary<string, object> updates)
    {
        foreach (var (key, value) in updates)
        {
            switch (key.ToLowerInvariant())
            {
                case "strictmode":
                    if (value is bool strict) validation.StrictMode = strict;
                    break;
                case "validaterequiredfields":
                    if (value is bool validateReq) validation.ValidateRequiredFields = validateReq;
                    break;
                case "validatedatatypes":
                    if (value is bool validateData) validation.ValidateDataTypes = validateData;
                    break;
                case "enableclinicalvalidation":
                    if (value is bool enableClinical) validation.EnableClinicalValidation = enableClinical;
                    break;
            }
        }
    }

    private static void UpdateInterfaceConfiguration(InterfaceConfiguration interfaceConfig, Dictionary<string, object> updates)
    {
        foreach (var (key, value) in updates)
        {
            switch (key.ToLowerInvariant())
            {
                case "name":
                    interfaceConfig.Name = value.ToString() ?? interfaceConfig.Name;
                    break;
                case "description":
                    interfaceConfig.Description = value.ToString() ?? interfaceConfig.Description;
                    break;
                case "maxmessagesize":
                    if (value is int maxSize) interfaceConfig.MaxMessageSize = maxSize;
                    break;
                case "connectiontimeoutseconds":
                    if (value is int timeout) interfaceConfig.ConnectionTimeoutSeconds = timeout;
                    break;
            }
        }
    }

    private static void UpdateLoggingConfiguration(LoggingConfiguration logging, Dictionary<string, object> updates)
    {
        foreach (var (key, value) in updates)
        {
            switch (key.ToLowerInvariant())
            {
                case "minimumlevel":
                    if (Enum.TryParse<LogLevel>(value.ToString(), out var level))
                        logging.MinimumLevel = level;
                    break;
                case "loghl7messages":
                    if (value is bool logHl7) logging.LogHL7Messages = logHl7;
                    break;
                case "logvalidationresults":
                    if (value is bool logValidation) logging.LogValidationResults = logValidation;
                    break;
                case "masksensitivedata":
                    if (value is bool maskSensitive) logging.MaskSensitiveData = maskSensitive;
                    break;
            }
        }
    }

    private static void UpdatePerformanceConfiguration(PerformanceConfiguration performance, Dictionary<string, object> updates)
    {
        foreach (var (key, value) in updates)
        {
            switch (key.ToLowerInvariant())
            {
                case "enableperformancemonitoring":
                    if (value is bool enablePerf) performance.EnablePerformanceMonitoring = enablePerf;
                    break;
                case "maxmemoryusagemb":
                    if (value is int maxMem) performance.MaxMemoryUsageMB = maxMem;
                    break;
                case "processingtimeoutseconds":
                    if (value is int procTimeout) performance.ProcessingTimeoutSeconds = procTimeout;
                    break;
                case "enablecaching":
                    if (value is bool enableCache) performance.EnableCaching = enableCache;
                    break;
            }
        }
    }
}