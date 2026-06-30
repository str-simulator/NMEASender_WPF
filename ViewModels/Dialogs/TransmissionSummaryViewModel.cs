using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMEASender.Wpf.Models.UI;
using System.Collections.ObjectModel;

namespace NMEASender.Wpf.ViewModels.Dialogs;

public sealed partial class TransmissionSummaryViewModel : ObservableObject
{
    public TransmissionSummaryViewModel(IReadOnlyList<TransmissionSourceSummaryItem> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (TransmissionSourceSummaryItem item in items)
        {
            Sources.Add(item);
        }
    }

    public ObservableCollection<TransmissionSourceSummaryItem> Sources { get; } = new();

    public IReadOnlyDictionary<string, string> SourceNotes =>
        Sources.ToDictionary(
            item => item.SourceKey,
            item => (item.Memo ?? string.Empty).Trim(),
            StringComparer.OrdinalIgnoreCase);

    public event EventHandler? CloseRequested;

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
