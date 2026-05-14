namespace NMEASender.Wpf.Services.Interfaces;

public interface IPortBaudRateService
{
    IReadOnlyList<int> BaudRateOptions { get; }

    IReadOnlyDictionary<string, int> CreateSnapshot(
        INmeaSenderConfigService config,
        IEnumerable<string> knownPorts,
        IEnumerable<string> sentencePorts,
        string defaultPort);

    bool TryApply(INmeaSenderConfigService config, IReadOnlyDictionary<string, int> portBaudRates, out string error);

    int ResolveBaudRate(INmeaSenderConfigService config, string portName);
}
