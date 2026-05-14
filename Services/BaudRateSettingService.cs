using System.Windows;
using NMEASender.Wpf.Services.Interfaces;
using NMEASender.Wpf.ViewModels;

namespace NMEASender.Wpf.Services;

public sealed class BaudRateSettingService : IBaudRateSettingService
{
    public bool TryShow(
        IReadOnlyDictionary<string, int> currentPortBaudRates,
        IReadOnlyList<int> baudRateOptions,
        out IReadOnlyDictionary<string, int> updatedPortBaudRates)
    {
        PortBaudRateSettingsViewModel viewModel = new(currentPortBaudRates, baudRateOptions);
        PortBaudRateSettingsWindow window = new(viewModel);

        if (Application.Current.MainWindow is Window owner && owner.IsVisible)
        {
            window.Owner = owner;
        }

        bool? dialogResult = window.ShowDialog();
        if (dialogResult == true)
        {
            updatedPortBaudRates = viewModel.Result;
            return true;
        }

        updatedPortBaudRates = currentPortBaudRates;
        return false;
    }
}
