// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace Segmint.GUI.Services;

/// <summary>
/// Service interface for managing navigation between views.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the current view type.
    /// </summary>
    Type? CurrentViewType { get; }

    /// <summary>
    /// Event raised when navigation occurs.
    /// </summary>
    event EventHandler<NavigationEventArgs> Navigated;

    /// <summary>
    /// Navigates to a view by type.
    /// </summary>
    /// <typeparam name="T">View type to navigate to.</typeparam>
    /// <param name="parameter">Optional navigation parameter.</param>
    void NavigateTo<T>(object? parameter = null) where T : class;

    /// <summary>
    /// Navigates to a view by type.
    /// </summary>
    /// <param name="viewType">View type to navigate to.</param>
    /// <param name="parameter">Optional navigation parameter.</param>
    void NavigateTo(Type viewType, object? parameter = null);

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    /// <returns>True if navigation was successful, false if no previous view.</returns>
    bool GoBack();

    /// <summary>
    /// Navigates forward to the next view.
    /// </summary>
    /// <returns>True if navigation was successful, false if no next view.</returns>
    bool GoForward();

    /// <summary>
    /// Clears the navigation history.
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Gets whether back navigation is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets whether forward navigation is possible.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Gets the navigation history.
    /// </summary>
    IReadOnlyList<NavigationEntry> History { get; }
}

/// <summary>
/// Navigation event arguments.
/// </summary>
public class NavigationEventArgs : EventArgs
{
    public Type ViewType { get; }
    public object? Parameter { get; }
    public DateTime NavigationTime { get; }

    public NavigationEventArgs(Type viewType, object? parameter = null)
    {
        ViewType = viewType;
        Parameter = parameter;
        NavigationTime = DateTime.Now;
    }
}

/// <summary>
/// Navigation history entry.
/// </summary>
public class NavigationEntry
{
    public Type ViewType { get; }
    public object? Parameter { get; }
    public DateTime NavigationTime { get; }
    public string DisplayName { get; }

    public NavigationEntry(Type viewType, object? parameter = null, string? displayName = null)
    {
        ViewType = viewType;
        Parameter = parameter;
        NavigationTime = DateTime.Now;
        DisplayName = displayName ?? viewType.Name.Replace("View", "").Replace("Page", "");
    }
}