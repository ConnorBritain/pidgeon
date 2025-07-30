// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Segmint.GUI.Services;

/// <summary>
/// Implementation of navigation service with history tracking.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService> _logger;
    private readonly List<NavigationEntry> _history = new();
    private int _currentIndex = -1;

    public Type? CurrentViewType => _currentIndex >= 0 && _currentIndex < _history.Count 
        ? _history[_currentIndex].ViewType 
        : null;

    public bool CanGoBack => _currentIndex > 0;
    public bool CanGoForward => _currentIndex < _history.Count - 1;
    public IReadOnlyList<NavigationEntry> History => _history.AsReadOnly();

    public event EventHandler<NavigationEventArgs>? Navigated;

    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void NavigateTo<T>(object? parameter = null) where T : class
    {
        NavigateTo(typeof(T), parameter);
    }

    public void NavigateTo(Type viewType, object? parameter = null)
    {
        try
        {
            if (!IsValidViewType(viewType))
            {
                _logger.LogError("Invalid view type: {ViewType}", viewType.Name);
                return;
            }

            // Create navigation entry
            var entry = new NavigationEntry(viewType, parameter);

            // Remove any forward history if we're not at the end
            if (_currentIndex < _history.Count - 1)
            {
                var itemsToRemove = _history.Count - _currentIndex - 1;
                _history.RemoveRange(_currentIndex + 1, itemsToRemove);
            }

            // Add new entry
            _history.Add(entry);
            _currentIndex = _history.Count - 1;

            // Limit history size (keep last 50 entries)
            if (_history.Count > 50)
            {
                _history.RemoveAt(0);
                _currentIndex--;
            }

            _logger.LogInformation("Navigated to {ViewType} with parameter {Parameter}", viewType.Name, parameter);
            
            // Raise navigation event
            Navigated?.Invoke(this, new NavigationEventArgs(viewType, parameter));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to {ViewType}", viewType.Name);
        }
    }

    public bool GoBack()
    {
        if (!CanGoBack) return false;

        try
        {
            _currentIndex--;
            var entry = _history[_currentIndex];
            
            _logger.LogInformation("Navigated back to {ViewType}", entry.ViewType.Name);
            
            Navigated?.Invoke(this, new NavigationEventArgs(entry.ViewType, entry.Parameter));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate back");
            return false;
        }
    }

    public bool GoForward()
    {
        if (!CanGoForward) return false;

        try
        {
            _currentIndex++;
            var entry = _history[_currentIndex];
            
            _logger.LogInformation("Navigated forward to {ViewType}", entry.ViewType.Name);
            
            Navigated?.Invoke(this, new NavigationEventArgs(entry.ViewType, entry.Parameter));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate forward");
            return false;
        }
    }

    public void ClearHistory()
    {
        _history.Clear();
        _currentIndex = -1;
        _logger.LogInformation("Navigation history cleared");
    }

    private bool IsValidViewType(Type viewType)
    {
        // Check if the type can be resolved from the service provider
        try
        {
            _serviceProvider.GetService(viewType);
            return true;
        }
        catch
        {
            return false;
        }
    }
}