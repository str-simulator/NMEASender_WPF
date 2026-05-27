using NMEASender.Wpf.Models.Network;
using System.IO.Ports;

namespace NMEASender.Wpf.Models.Core;

public sealed record OutputOpenRequest(
    IReadOnlyList<string> EnabledPorts,
    int DefaultBaudRate,
    IReadOnlyDictionary<string, int> PortBaudRates,
    int DataBits,
    Parity Parity,
    StopBits StopBits,
    bool UseUdp,
    UdpTransportOptions UdpTransportOptions);

public sealed record PortOpenOutcome(string PortName, bool Success, string? Error);

public sealed record OutputOpenResult(
    IReadOnlyList<PortOpenOutcome> PortResults,
    bool UdpOpenSuccess,
    string? UdpOpenError);
