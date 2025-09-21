// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core;

namespace Pidgeon.CLI.Services;

/// <summary>
/// Helper service for managing template configuration with smart defaults and interactive prompting.
/// Improves CLI UX by reducing required flags through intelligent configuration management.
/// </summary>
public class TemplateConfigHelper
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<TemplateConfigHelper> _logger;

    public TemplateConfigHelper(
        IConfigurationService configurationService,
        ILogger<TemplateConfigHelper> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets template author, prompting interactively if not configured and not provided via flag.
    /// </summary>
    /// <param name="explicitAuthor">Author provided via command flag</param>
    /// <param name="allowPrompting">Whether to allow interactive prompting</param>
    /// <returns>Author name to use</returns>
    public async Task<string?> GetTemplateAuthorAsync(string? explicitAuthor, bool allowPrompting = true)
    {
        // Use explicit flag value if provided
        if (!string.IsNullOrWhiteSpace(explicitAuthor))
        {
            return explicitAuthor;
        }

        // Check configuration for default
        var configResult = await _configurationService.GetCurrentConfigurationAsync();
        if (configResult.IsSuccess && !string.IsNullOrWhiteSpace(configResult.Value.TemplateAuthor))
        {
            return configResult.Value.TemplateAuthor;
        }

        // Prompt if allowed and not configured
        if (allowPrompting)
        {
            return await PromptAndSaveAuthorAsync();
        }

        return null;
    }

    /// <summary>
    /// Gets template category, prompting interactively if not configured and not provided via flag.
    /// </summary>
    /// <param name="explicitCategory">Category provided via command flag</param>
    /// <param name="allowPrompting">Whether to allow interactive prompting</param>
    /// <returns>Category to use</returns>
    public async Task<string?> GetTemplateCategoryAsync(string? explicitCategory, bool allowPrompting = true)
    {
        // Use explicit flag value if provided
        if (!string.IsNullOrWhiteSpace(explicitCategory))
        {
            return explicitCategory;
        }

        // Check configuration for default
        var configResult = await _configurationService.GetCurrentConfigurationAsync();
        if (configResult.IsSuccess && !string.IsNullOrWhiteSpace(configResult.Value.DefaultTemplateCategory))
        {
            return configResult.Value.DefaultTemplateCategory;
        }

        // Prompt if allowed and not configured
        if (allowPrompting)
        {
            return await PromptAndSaveCategoryAsync();
        }

        return null;
    }

    /// <summary>
    /// Gets export format, using configured default if not explicitly specified.
    /// </summary>
    /// <param name="explicitFormat">Format provided via command flag</param>
    /// <returns>Export format to use</returns>
    public async Task<ExportFormat> GetExportFormatAsync(string? explicitFormat)
    {
        // Use explicit flag value if provided
        if (!string.IsNullOrWhiteSpace(explicitFormat) &&
            Enum.TryParse<ExportFormat>(explicitFormat, true, out var explicitFormatEnum))
        {
            return explicitFormatEnum;
        }

        // Check configuration for default
        var configResult = await _configurationService.GetCurrentConfigurationAsync();
        if (configResult.IsSuccess && !string.IsNullOrWhiteSpace(configResult.Value.DefaultExportFormat) &&
            Enum.TryParse<ExportFormat>(configResult.Value.DefaultExportFormat, true, out var configFormat))
        {
            return configFormat;
        }

        // Fall back to YAML default
        return ExportFormat.Yaml;
    }

    private async Task<string?> PromptAndSaveAuthorAsync()
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine("📝 Template author not configured. Who should be credited as the author?");
            Console.Write("Author: ");

            var author = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(author))
            {
                return null;
            }

            // Ask if they want to save as default
            Console.Write("Save as default author? (Y/n): ");
            var saveResponse = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (saveResponse != "n" && saveResponse != "no")
            {
                await SaveAuthorDefaultAsync(author);
                Console.WriteLine($"✅ Saved '{author}' as default template author");
            }

            return author;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to prompt for template author");
            return null;
        }
    }

    private async Task<string?> PromptAndSaveCategoryAsync()
    {
        try
        {
            var commonCategories = new[]
            {
                "emergency", "surgical", "lab", "admin", "testing", "pharmacy", "imaging", "other"
            };

            Console.WriteLine();
            Console.WriteLine("📂 Template category not specified. What category is this template?");

            for (int i = 0; i < commonCategories.Length; i++)
            {
                Console.WriteLine($"{i + 1}) {commonCategories[i]}");
            }

            Console.Write("Category (number or type custom): ");
            var categoryInput = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(categoryInput))
            {
                return null;
            }

            string category;

            // Check if input is a number (selecting from list)
            if (int.TryParse(categoryInput, out var categoryNumber) &&
                categoryNumber >= 1 && categoryNumber <= commonCategories.Length)
            {
                category = commonCategories[categoryNumber - 1];
            }
            else
            {
                // Use custom category
                category = categoryInput.ToLowerInvariant();
            }

            // Ask if they want to save as default
            Console.Write($"Save '{category}' as default category? (Y/n): ");
            var saveResponse = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (saveResponse != "n" && saveResponse != "no")
            {
                await SaveCategoryDefaultAsync(category);
                Console.WriteLine($"✅ Saved '{category}' as default template category");
            }

            return category;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to prompt for template category");
            return null;
        }
    }

    private async Task SaveAuthorDefaultAsync(string author)
    {
        try
        {
            var configResult = await _configurationService.GetCurrentConfigurationAsync();
            if (configResult.IsFailure)
            {
                _logger.LogWarning("Failed to get current configuration: {Error}", configResult.Error.Message);
                return;
            }

            var updatedConfig = configResult.Value with
            {
                TemplateAuthor = author
            };

            var saveResult = await _configurationService.UpdateConfigurationAsync(updatedConfig);
            if (saveResult.IsFailure)
            {
                _logger.LogWarning("Failed to save author default: {Error}", saveResult.Error.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save author default");
        }
    }

    private async Task SaveCategoryDefaultAsync(string category)
    {
        try
        {
            var configResult = await _configurationService.GetCurrentConfigurationAsync();
            if (configResult.IsFailure)
            {
                _logger.LogWarning("Failed to get current configuration: {Error}", configResult.Error.Message);
                return;
            }

            var updatedConfig = configResult.Value with
            {
                DefaultTemplateCategory = category
            };

            var saveResult = await _configurationService.UpdateConfigurationAsync(updatedConfig);
            if (saveResult.IsFailure)
            {
                _logger.LogWarning("Failed to save category default: {Error}", saveResult.Error.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save category default");
        }
    }
}