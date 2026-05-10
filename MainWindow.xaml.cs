using System.Windows;
using NMEASender.Wpf.ViewModels;

namespace NMEASender.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void ComboBox_DropDownOpened(object sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.RefreshPorts();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }
}
