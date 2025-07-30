using System.Windows.Controls;
using Segmint.GUI.ViewModels;

namespace Segmint.GUI.Views;

public partial class ValidationDashboardView : UserControl
{
    public ValidationDashboardView(ValidationDashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}