using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface IBaudRateSettingService
{
    bool TryShow(
        IReadOnlyDictionary<string, int> currentPortBaudRates,
        IReadOnlyList<int> baudRateOptions,
        IReadOnlyList<SentenceUdpPortSetting> currentSentenceUdpPorts,
        UdpTransportOptions currentUdpTransportOptions,
        bool supportsPerSentenceMulticastAddress,
        out IReadOnlyDictionary<string, int> updatedPortBaudRates,
        out IReadOnlyDictionary<string, int> updatedSentenceUdpPorts,
        out IReadOnlyDictionary<string, string> updatedSentenceUdpAddresses,
        out UdpTransportOptions updatedUdpTransportOptions);
}
