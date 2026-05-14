namespace NMEASender.Wpf.Services.Interfaces;

public interface IBaudRateSettingService
{
    bool TryShow(
        IReadOnlyDictionary<string, int> currentPortBaudRates,
        IReadOnlyList<int> baudRateOptions,
        out IReadOnlyDictionary<string, int> updatedPortBaudRates);
}
