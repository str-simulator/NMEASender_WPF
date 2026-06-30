using NMEASender.Wpf.ViewModels.Dialogs;
using System.ComponentModel;
using System.Windows;

namespace NMEASender.Wpf.Views.Dialogs;

public partial class TransmissionSummaryWindow : Window
{
    private readonly TransmissionSummaryViewModel _viewModel;

    public TransmissionSummaryWindow(TransmissionSummaryViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.CloseRequested += ViewModel_CloseRequested;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _viewModel.CloseRequested -= ViewModel_CloseRequested;
        base.OnClosing(e);
    }

    private void ViewModel_CloseRequested(object? sender, EventArgs e)
    {
        Close();
    }
}
