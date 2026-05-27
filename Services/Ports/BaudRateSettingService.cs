using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.Ports;
using NMEASender.Wpf.ViewModels.Dialogs;
using NMEASender.Wpf.Views.Dialogs;
using NMEASender.Wpf.Views.Shell;
using System.Windows;

namespace NMEASender.Wpf.Services.Ports;

public sealed class BaudRateSettingService : IBaudRateSettingService
{
    public bool TryShow(
        IReadOnlyDictionary<string, int> currentPortBaudRates,
        IReadOnlyList<int> baudRateOptions,
        IReadOnlyList<SentenceUdpPortSetting> currentSentenceUdpPorts,
        UdpTransportOptions currentUdpTransportOptions,
        bool supportsPerSentenceMulticastAddress,
        out IReadOnlyDictionary<string, int> updatedPortBaudRates,
        out IReadOnlyDictionary<string, int> updatedSentenceUdpPorts,
        out IReadOnlyDictionary<string, string> updatedSentenceUdpAddresses,
        out UdpTransportOptions updatedUdpTransportOptions)
    {
        PortBaudRateSettingsViewModel viewModel = new(
            currentPortBaudRates,
            baudRateOptions,
            currentSentenceUdpPorts,
            currentUdpTransportOptions,
            supportsPerSentenceMulticastAddress);
        PortBaudRateSettingsWindow window = new(viewModel);

        if (System.Windows.Application.Current.MainWindow is Window owner && owner.IsVisible)
        {
            window.Owner = owner;
        }

        bool? dialogResult = window.ShowDialog();
        if (dialogResult == true)
        {
            updatedPortBaudRates = viewModel.Result;
            updatedSentenceUdpPorts = viewModel.SentenceUdpPortResult;
            updatedSentenceUdpAddresses = viewModel.SentenceUdpAddressResult;
            updatedUdpTransportOptions = viewModel.UdpTransportResult;
            return true;
        }

        updatedPortBaudRates = currentPortBaudRates;
        updatedSentenceUdpPorts = currentSentenceUdpPorts.ToDictionary(
            item => item.RowKey,
            item => item.UdpPort,
            StringComparer.OrdinalIgnoreCase);
        updatedSentenceUdpAddresses = currentSentenceUdpPorts.ToDictionary(
            item => item.RowKey,
            item => (item.UdpAddress ?? string.Empty).Trim(),
            StringComparer.OrdinalIgnoreCase);
        updatedUdpTransportOptions = currentUdpTransportOptions;
        return false;
    }
}
