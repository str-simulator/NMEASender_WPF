using NMEASender.Wpf.Services.Interfaces.Config;
using NMEASender.Wpf.Services.Interfaces.Ports;

namespace NMEASender.Wpf.Services.Ports;

public sealed class PortBaudRateService : IPortBaudRateService
{
    private static readonly int[] DefaultBaudRates = [1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200];

    public IReadOnlyList<int> BaudRateOptions => DefaultBaudRates;

    public IReadOnlyDictionary<string, int> CreateSnapshot(
        INmeaSenderConfigService config,
        IEnumerable<string> knownPorts,
        IEnumerable<string> sentencePorts,
        string defaultPort)
    {
        HashSet<string> ports = new(StringComparer.OrdinalIgnoreCase);

        AddKnownPort(defaultPort, ports);
        foreach (string port in knownPorts)
        {
            AddKnownPort(port, ports);
        }

        foreach (string port in sentencePorts)
        {
            AddKnownPort(port, ports);
        }

        foreach (string port in config.PortBaudRates.Keys)
        {
            AddKnownPort(port, ports);
        }

        Dictionary<string, int> snapshot = new(StringComparer.OrdinalIgnoreCase);
        foreach (string port in ports.OrderBy(ExtractPortNumber).ThenBy(port => port, StringComparer.OrdinalIgnoreCase))
        {
            snapshot[port] = ResolveBaudRate(config, port);
        }

        return snapshot;
    }

    public bool TryApply(INmeaSenderConfigService config, IReadOnlyDictionary<string, int> portBaudRates, out string error)
    {
        error = string.Empty;
        Dictionary<string, int> normalized = new(StringComparer.OrdinalIgnoreCase);

        foreach ((string portName, int baudRate) in portBaudRates)
        {
            string normalizedPort = NormalizePortName(portName);
            if (string.IsNullOrWhiteSpace(normalizedPort))
            {
                continue;
            }

            if (baudRate <= 0)
            {
                error = $"{normalizedPort} baud rate is invalid.";
                return false;
            }

            normalized[normalizedPort] = baudRate;
        }

        config.PortBaudRates.Clear();
        foreach ((string portName, int baudRate) in normalized)
        {
            config.PortBaudRates[portName] = baudRate;
        }

        return true;
    }

    public int ResolveBaudRate(INmeaSenderConfigService config, string portName)
    {
        string normalizedPort = NormalizePortName(portName);
        if (config.PortBaudRates.TryGetValue(normalizedPort, out int baudRate) && baudRate > 0)
        {
            return baudRate;
        }

        return config.BaudRate;
    }

    private static void AddKnownPort(string? portName, ISet<string> target)
    {
        string normalized = NormalizePortName(portName);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            target.Add(normalized);
        }
    }

    private static int ExtractPortNumber(string portName)
    {
        string digits = new string((portName ?? string.Empty).Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int portNumber) ? portNumber : int.MaxValue;
    }

    private static string NormalizePortName(string? portName)
    {
        return (portName ?? string.Empty).Trim().ToUpperInvariant();
    }
}
