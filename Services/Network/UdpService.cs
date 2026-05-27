using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Services.Interfaces.Network;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NMEASender.Wpf.Services.Network;

public sealed class UdpService : IUdpService
{
    private readonly UdpClient _client = new(AddressFamily.InterNetwork);
    private UdpTransportOptions _options = UdpTransportOptions.CreateDefault();
    private bool _isOpen;
    private bool _disposed;

    public bool IsOpen => !_disposed && _isOpen;

    public bool Open(UdpTransportOptions options, out string error)
    {
        error = string.Empty;
        try
        {
            if (_disposed)
            {
                error = "UDP sender is disposed.";
                return false;
            }

            if (options is null)
            {
                error = "UDP transport options are required.";
                return false;
            }

            UdpTransportOptions normalized = options.WithFallbackPort(
                options.Mode == UdpTransportMode.Multicast ? options.MulticastSendPort : options.BroadcastPort);

            if (!normalized.IsEnabled)
            {
                error = "UDP transport is disabled.";
                _isOpen = false;
                return false;
            }

            _options = normalized;
            _client.EnableBroadcast = normalized.Mode == UdpTransportMode.Broadcast;

            if (normalized.Mode == UdpTransportMode.Multicast)
            {
                _client.Client.SetSocketOption(
                    SocketOptionLevel.IP,
                    SocketOptionName.MulticastTimeToLive,
                    Math.Clamp(normalized.MulticastTtl, 1, 255));

                if (IPAddress.TryParse(normalized.MulticastInterfaceAddress, out IPAddress? interfaceAddress))
                {
                    _client.Client.SetSocketOption(
                        SocketOptionLevel.IP,
                        SocketOptionName.MulticastInterface,
                        interfaceAddress.GetAddressBytes());
                }
            }

            _isOpen = true;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _isOpen = false;
            return false;
        }
    }

    public bool Send(string sentence, int port, string? addressOverride, out string error)
    {
        error = string.Empty;
        try
        {
            if (!_isOpen)
            {
                error = "UDP sender is not open.";
                return false;
            }

            if (!_options.IsEnabled)
            {
                error = "UDP transport is disabled.";
                return false;
            }

            int targetPort = _options.ResolveTargetPort(port);
            if (targetPort is < 1 or > 65535)
            {
                error = "UDP port must be between 1 and 65535.";
                return false;
            }

            string targetAddress = string.IsNullOrWhiteSpace(addressOverride)
                ? _options.ResolveTargetAddress()
                : addressOverride.Trim();
            if (!IPAddress.TryParse(targetAddress, out IPAddress? address))
            {
                error = $"Invalid UDP address: {targetAddress}";
                return false;
            }

            byte[] bytes = Encoding.ASCII.GetBytes(sentence);
            IPEndPoint endPoint = new(address, targetPort);
            _client.Send(bytes, bytes.Length, endPoint);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _isOpen = false;
            return false;
        }
    }

    public void Close()
    {
        _isOpen = false;
    }

    public void Dispose()
    {
        Close();
        _client.Dispose();
        _disposed = true;
    }
}
