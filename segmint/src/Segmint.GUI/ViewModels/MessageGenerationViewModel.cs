// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Segmint.GUI.ViewModels;

/// <summary>
/// View model for HL7 message generation functionality.
/// </summary>
public partial class MessageGenerationViewModel : ObservableObject
{
    private readonly ILogger<MessageGenerationViewModel> _logger;

    [ObservableProperty]
    private string _statusMessage = "Message generation functionality coming soon...";

    public MessageGenerationViewModel(ILogger<MessageGenerationViewModel> logger)
    {
        _logger = logger;
    }
}