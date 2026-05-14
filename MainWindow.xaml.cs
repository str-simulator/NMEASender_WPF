using System.Windows;
using NMEASender.Wpf.ViewModels;

namespace NMEASender.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
