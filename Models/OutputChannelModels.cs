using System.IO.Ports;

namespace NMEASender.Wpf.Models;

public sealed record OutputOpenRequest(
    IReadOnlyList<string> EnabledPorts,
    int DefaultBaudRate,
    IReadOnlyDictionary<string, int> PortBaudRates,
    int DataBits,
    Parity Parity,
    StopBits StopBits,
    bool UseUdp,
    int UdpPort);

public sealed record PortOpenOutcome(string PortName, bool Success, string? Error);

public sealed record OutputOpenResult(
    IReadOnlyList<PortOpenOutcome> PortResults,
    bool UdpOpenSuccess,
    string? UdpOpenError);
