using NMEASender.Wpf.Models.UI;

namespace NMEASender.Wpf.Services.Interfaces.Workflow;

public interface ITransmissionSummaryDialogService
{
    IReadOnlyDictionary<string, string> Show(IReadOnlyList<TransmissionSourceSummaryItem> items);
}
