using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.Workflow;
using NMEASender.Wpf.ViewModels.Dialogs;
using NMEASender.Wpf.Views.Dialogs;
using System.Windows;

namespace NMEASender.Wpf.Services.Workflow;

public sealed class TransmissionSummaryDialogService : ITransmissionSummaryDialogService
{
    public IReadOnlyDictionary<string, string> Show(IReadOnlyList<TransmissionSourceSummaryItem> items)
    {
        TransmissionSummaryViewModel viewModel = new(items);
        TransmissionSummaryWindow window = new(viewModel);

        if (System.Windows.Application.Current.MainWindow is Window owner && owner.IsVisible)
        {
            window.Owner = owner;
        }

        window.ShowDialog();
        return viewModel.SourceNotes;
    }
}
