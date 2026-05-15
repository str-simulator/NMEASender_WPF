using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface IBaudRateSettingService
{
    bool TryShow(
        IReadOnlyDictionary<string, int> currentPortBaudRates,
        IReadOnlyList<int> baudRateOptions,
        IReadOnlyList<SentenceUdpPortSetting> currentSentenceUdpPorts,
        out IReadOnlyDictionary<string, int> updatedPortBaudRates,
        out IReadOnlyDictionary<string, int> updatedSentenceUdpPorts);
}
