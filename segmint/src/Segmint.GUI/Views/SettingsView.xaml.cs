using System.Windows.Controls;
using Segmint.GUI.ViewModels;

namespace Segmint.GUI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}