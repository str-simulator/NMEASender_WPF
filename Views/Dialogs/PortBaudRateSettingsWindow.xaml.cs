using NMEASender.Wpf.ViewModels.Dialogs;
using System.Windows;

namespace NMEASender.Wpf.Views.Dialogs;

public partial class PortBaudRateSettingsWindow : Window
{
    private readonly PortBaudRateSettingsViewModel _viewModel;

    public PortBaudRateSettingsWindow(PortBaudRateSettingsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _viewModel.CloseRequested += ViewModel_CloseRequested;

        DataContext = _viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.CloseRequested -= ViewModel_CloseRequested;
        base.OnClosed(e);
    }

    private void ViewModel_CloseRequested(object? sender, bool dialogResult)
    {
        DialogResult = dialogResult;
    }
}
