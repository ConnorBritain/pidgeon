// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Windows.Media;
using ModernWpf;

namespace Segmint.GUI.Services;

/// <summary>
/// Service interface for managing application theming.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current application theme.
    /// </summary>
    ApplicationTheme CurrentTheme { get; }

    /// <summary>
    /// Gets the current accent color.
    /// </summary>
    Color CurrentAccentColor { get; }

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<ApplicationTheme> ThemeChanged;

    /// <summary>
    /// Event raised when the accent color changes.
    /// </summary>
    event EventHandler<Color> AccentColorChanged;

    /// <summary>
    /// Sets the application theme.
    /// </summary>
    /// <param name="theme">Theme to apply.</param>
    void SetTheme(ApplicationTheme theme);

    /// <summary>
    /// Sets the accent color.
    /// </summary>
    /// <param name="color">Accent color to apply.</param>
    void SetAccentColor(Color color);

    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    void ToggleTheme();

    /// <summary>
    /// Gets all available predefined accent colors.
    /// </summary>
    /// <returns>Array of predefined colors.</returns>
    Color[] GetPredefinedAccentColors();

    /// <summary>
    /// Saves the current theme settings.
    /// </summary>
    void SaveThemeSettings();

    /// <summary>
    /// Loads saved theme settings.
    /// </summary>
    void LoadThemeSettings();
}