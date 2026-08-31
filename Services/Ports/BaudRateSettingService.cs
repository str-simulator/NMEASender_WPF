using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.Ports;
using NMEASender.Wpf.Services.Interfaces.Search;
using NMEASender.Wpf.ViewModels.Dialogs;
using NMEASender.Wpf.Views.Dialogs;
using System.Windows;

namespace NMEASender.Wpf.Services.Ports;

public sealed class BaudRateSettingService : IBaudRateSettingService
{
    private readonly ISentenceSearchService _sentenceSearchService;

    public BaudRateSettingService(ISentenceSearchService sentenceSearchService)
    {
        _sentenceSearchService = sentenceSearchService ?? throw new ArgumentNullException(nameof(sentenceSearchService));
    }

    public bool TryShow(
        IReadOnlyDictionary<string, int> currentPortBaudRates,
        IReadOnlyList<int> baudRateOptions,
        IReadOnlyList<SentenceUdpPortSetting> currentSentenceUdpPorts,
        int currentUdpPort,
        UdpTransportOptions currentUdpTransportOptions,
        bool supportsPerSentenceMulticastAddress,
        out IReadOnlyDictionary<string, int> updatedPortBaudRates,
        out IReadOnlyDictionary<string, int> updatedSentenceUdpPorts,
        out IReadOnlyDictionary<string, string> updatedSentenceUdpAddresses,
        out IReadOnlyDictionary<string, double> updatedSentenceHz,
        out IReadOnlyDictionary<string, string> updatedSentenceTalkerIds,
        out int updatedUdpPort,
        out UdpTransportOptions updatedUdpTransportOptions)
    {
        PortBaudRateSettingsViewModel viewModel = new(
            _sentenceSearchService,
            currentPortBaudRates,
            baudRateOptions,
            currentSentenceUdpPorts,
            currentUdpPort,
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
            updatedSentenceHz = viewModel.SentenceHzResult;
            updatedSentenceTalkerIds = viewModel.SentenceTalkerIdResult;
            updatedUdpPort = viewModel.UdpPortResult;
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
        updatedSentenceHz = currentSentenceUdpPorts.ToDictionary(
            item => item.RowKey,
            item => item.Hz,
            StringComparer.OrdinalIgnoreCase);
        updatedSentenceTalkerIds = currentSentenceUdpPorts.ToDictionary(
            item => item.RowKey,
            item => item.TalkerId,
            StringComparer.OrdinalIgnoreCase);
        updatedUdpPort = currentUdpPort;
        updatedUdpTransportOptions = currentUdpTransportOptions;
        return false;
    }
}
