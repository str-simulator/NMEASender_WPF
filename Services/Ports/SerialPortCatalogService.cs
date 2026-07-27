using NMEASender.Wpf.Exceptions;
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
            error = new SerialPortCatalogException(ex).Message;
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

        // 요청 포트/기본 포트가 일시적으로 사용 불가능한 상태(예: 케이블 뽑힘).
        // 다른 연결된 포트로 조용히 전환하지 않고 설정된 포트 이름을 그대로 유지하여
        // 연결 해제로 인해 사용자 설정이 덮어써지지 않도록 한다.
        if (!string.IsNullOrWhiteSpace(requestedPort))
        {
            return requestedPort;
        }

        if (!string.IsNullOrWhiteSpace(defaultPort))
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
