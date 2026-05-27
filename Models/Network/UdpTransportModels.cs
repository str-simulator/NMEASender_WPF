using System.Net;

namespace NMEASender.Wpf.Models.Network;

public enum UdpTransportMode
{
    Multicast = 0,
    Broadcast = 1,
    Disabled = 2
}

public sealed record UdpTransportOptions(
    UdpTransportMode Mode,
    int BroadcastPort,
    int MulticastPortNo,
    int MulticastSendPort,
    string MulticastAddress,
    int MulticastTtl = 32,
    string MulticastInterfaceAddress = "0.0.0.0",
    bool UseRequestedPort = true)
{
    public bool IsEnabled => Mode != UdpTransportMode.Disabled;

    public int ResolveTargetPort(int requestedPort)
    {
        if (UseRequestedPort && requestedPort is >= 1 and <= 65535)
        {
            return requestedPort;
        }

        return Mode == UdpTransportMode.Multicast
            ? NormalizePort(MulticastSendPort, 6000)
            : NormalizePort(BroadcastPort, 40014);
    }

    public string ResolveTargetAddress()
    {
        return Mode == UdpTransportMode.Multicast
            ? NormalizeAddress(MulticastAddress, "225.0.0.0")
            : IPAddress.Broadcast.ToString();
    }

    public static UdpTransportOptions CreateDefault(int fallbackPort = 40014)
    {
        int normalizedPort = NormalizePort(fallbackPort, 40014);
        return new UdpTransportOptions(
            UdpTransportMode.Broadcast,
            BroadcastPort: normalizedPort,
            MulticastPortNo: 6000,
            MulticastSendPort: normalizedPort,
            MulticastAddress: "225.0.0.0",
            UseRequestedPort: true);
    }

    public UdpTransportOptions WithFallbackPort(int fallbackPort)
    {
        int normalizedPort = NormalizePort(fallbackPort, 40014);
        return this with
        {
            BroadcastPort = NormalizePort(BroadcastPort, normalizedPort),
            MulticastSendPort = NormalizePort(MulticastSendPort, normalizedPort),
            MulticastPortNo = NormalizePort(MulticastPortNo, 6000),
            MulticastAddress = NormalizeAddress(MulticastAddress, "225.0.0.0"),
            MulticastTtl = Math.Clamp(MulticastTtl, 1, 255),
            MulticastInterfaceAddress = NormalizeAddress(MulticastInterfaceAddress, "0.0.0.0")
        };
    }

    public static int NormalizePort(int value, int fallback)
    {
        return value is >= 1 and <= 65535
            ? value
            : (fallback is >= 1 and <= 65535 ? fallback : 40014);
    }

    public static string NormalizeAddress(string? value, string fallback)
    {
        string trimmed = (value ?? string.Empty).Trim();
        if (IPAddress.TryParse(trimmed, out _))
        {
            return trimmed;
        }

        return fallback;
    }
}
