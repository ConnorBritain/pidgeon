// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Windows.Controls;
using Segmint.GUI.ViewModels;

namespace Segmint.GUI.Views;

/// <summary>
/// Configuration management view for vendor-specific HL7 configurations.
/// </summary>
public partial class ConfigurationManagementView : UserControl
{
    public ConfigurationManagementView(ConfigurationManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}