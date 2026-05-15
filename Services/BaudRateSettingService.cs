using System.Windows;
using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;
using NMEASender.Wpf.ViewModels;

namespace NMEASender.Wpf.Services;

public sealed class BaudRateSettingService : IBaudRateSettingService
{
    public bool TryShow(
        IReadOnlyDictionary<string, int> currentPortBaudRates,
        IReadOnlyList<int> baudRateOptions,
        IReadOnlyList<SentenceUdpPortSetting> currentSentenceUdpPorts,
        out IReadOnlyDictionary<string, int> updatedPortBaudRates,
        out IReadOnlyDictionary<string, int> updatedSentenceUdpPorts)
    {
        PortBaudRateSettingsViewModel viewModel = new(currentPortBaudRates, baudRateOptions, currentSentenceUdpPorts);
        PortBaudRateSettingsWindow window = new(viewModel);

        if (Application.Current.MainWindow is Window owner && owner.IsVisible)
        {
            window.Owner = owner;
        }

        bool? dialogResult = window.ShowDialog();
        if (dialogResult == true)
        {
            updatedPortBaudRates = viewModel.Result;
            updatedSentenceUdpPorts = viewModel.SentenceUdpPortResult;
            return true;
        }

        updatedPortBaudRates = currentPortBaudRates;
        updatedSentenceUdpPorts = currentSentenceUdpPorts.ToDictionary(
            item => item.RowKey,
            item => item.UdpPort,
            StringComparer.OrdinalIgnoreCase);
        return false;
    }
}
