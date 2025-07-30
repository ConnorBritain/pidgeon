// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Segmint.GUI.ViewModels;

/// <summary>
/// View model for validation dashboard functionality.
/// </summary>
public partial class ValidationDashboardViewModel : ObservableObject
{
    private readonly ILogger<ValidationDashboardViewModel> _logger;

    [ObservableProperty]
    private string _statusMessage = "Validation dashboard functionality coming soon...";

    public ValidationDashboardViewModel(ILogger<ValidationDashboardViewModel> logger)
    {
        _logger = logger;
    }
}