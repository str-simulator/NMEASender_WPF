using NMEASender.Wpf.Services.Interfaces.Ports;
using System.IO.Ports;

namespace NMEASender.Wpf.Services.Ports;

public sealed class SerialPortCatalogService : ISerialPortCatalogService
{
    public IReadOnlyList<string> GetSortedPorts(out string error)
    {
        error = string.Empty;
        try
        {
            List<string> names = SerialPort.GetPortNames()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(PortSortKey)
                .ThenBy(port => port, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return names;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return Array.Empty<string>();
        }
    }

    public string PickAvailablePort(IEnumerable<string> availablePorts, string requestedPort, string defaultPort)
    {
        IReadOnlyCollection<string> portSet = availablePorts as IReadOnlyCollection<string> ?? availablePorts.ToArray();
        if (!string.IsNullOrWhiteSpace(requestedPort) && portSet.Contains(requestedPort, StringComparer.OrdinalIgnoreCase))
        {
            return requestedPort;
        }

        if (!string.IsNullOrWhiteSpace(defaultPort) && portSet.Contains(defaultPort, StringComparer.OrdinalIgnoreCase))
        {
            return defaultPort;
        }

        return portSet.FirstOrDefault() ?? string.Empty;
    }

    private static int PortSortKey(string port)
    {
        string digits = new string(port.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int value) ? value : int.MaxValue;
    }
}
