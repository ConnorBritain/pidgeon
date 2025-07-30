// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using ModernWpf;

namespace Segmint.GUI.Services;

/// <summary>
/// Implementation of theme service using ModernWPF theming.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;
    private readonly string _settingsPath;

    public ApplicationTheme CurrentTheme => ThemeManager.Current.ApplicationTheme ?? ApplicationTheme.Light;
    public Color CurrentAccentColor => ThemeManager.Current.AccentColor ?? Colors.DodgerBlue;

    public event EventHandler<ApplicationTheme>? ThemeChanged;
    public event EventHandler<Color>? AccentColorChanged;

    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Segmint",
            "theme-settings.json");

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        
        LoadThemeSettings();
    }

    public void SetTheme(ApplicationTheme theme)
    {
        try
        {
            var previousTheme = CurrentTheme;
            ThemeManager.Current.ApplicationTheme = theme;
            
            if (previousTheme != theme)
            {
                _logger.LogInformation("Theme changed from {PreviousTheme} to {NewTheme}", previousTheme, theme);
                ThemeChanged?.Invoke(this, theme);
                SaveThemeSettings();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set theme to {Theme}", theme);
        }
    }

    public void SetAccentColor(Color color)
    {
        try
        {
            var previousColor = CurrentAccentColor;
            ThemeManager.Current.AccentColor = color;
            
            if (previousColor != color)
            {
                _logger.LogInformation("Accent color changed from {PreviousColor} to {NewColor}", previousColor, color);
                AccentColorChanged?.Invoke(this, color);
                SaveThemeSettings();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set accent color to {Color}", color);
        }
    }

    public void ToggleTheme()
    {
        var newTheme = CurrentTheme == ApplicationTheme.Light 
            ? ApplicationTheme.Dark 
            : ApplicationTheme.Light;
        SetTheme(newTheme);
    }

    public Color[] GetPredefinedAccentColors()
    {
        return new[]
        {
            Colors.DodgerBlue,
            Colors.Green,
            Colors.Orange,
            Colors.Purple,
            Colors.Red,
            Colors.Teal,
            Colors.Pink,
            Colors.Brown,
            Colors.Gray,
            Colors.Indigo,
            Colors.Lime,
            Colors.Magenta
        };
    }

    public void SaveThemeSettings()
    {
        try
        {
            var settings = new ThemeSettings
            {
                Theme = CurrentTheme.ToString(),
                AccentColor = CurrentAccentColor.ToString()
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
            
            _logger.LogDebug("Theme settings saved to {Path}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save theme settings to {Path}", _settingsPath);
        }
    }

    public void LoadThemeSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogDebug("Theme settings file not found at {Path}, using defaults", _settingsPath);
                return;
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<ThemeSettings>(json);

            if (settings != null)
            {
                // Apply theme
                if (Enum.TryParse<ApplicationTheme>(settings.Theme, out var theme))
                {
                    ThemeManager.Current.ApplicationTheme = theme;
                }

                // Apply accent color
                if (ColorConverter.ConvertFromString(settings.AccentColor) is Color color)
                {
                    ThemeManager.Current.AccentColor = color;
                }

                _logger.LogInformation("Theme settings loaded: Theme={Theme}, AccentColor={AccentColor}", 
                    settings.Theme, settings.AccentColor);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load theme settings from {Path}", _settingsPath);
        }
    }

    private class ThemeSettings
    {
        public string Theme { get; set; } = "Light";
        public string AccentColor { get; set; } = "DodgerBlue";
    }
}