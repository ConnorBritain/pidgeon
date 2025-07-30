using System.Windows.Controls;
using Segmint.GUI.ViewModels;

namespace Segmint.GUI.Views;

public partial class MessageGenerationView : UserControl
{
    public MessageGenerationView(MessageGenerationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}