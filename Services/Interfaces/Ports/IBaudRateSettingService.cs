using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.UI;

namespace NMEASender.Wpf.Services.Interfaces.Ports;

public interface IBaudRateSettingService
{
    bool TryShow(
        IReadOnlyDictionary<string, int> currentPortBaudRates,
        IReadOnlyList<int> baudRateOptions,
        IReadOnlyList<SentenceUdpPortSetting> currentSentenceUdpPorts,
        int currentUdpPort,
        UdpTransportOptions currentUdpTransportOptions,
        bool supportsPerSentenceMulticastAddress,
        out IReadOnlyDictionary<string, int> updatedPortBaudRates,
        out IReadOnlyDictionary<string, int> updatedSentenceUdpPorts,
        out IReadOnlyDictionary<string, string> updatedSentenceUdpAddresses,
        out int updatedUdpPort,
        out UdpTransportOptions updatedUdpTransportOptions);
}
