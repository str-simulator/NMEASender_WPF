using NMEASender.Wpf.Exceptions;
using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Services.Interfaces.Network;
using System.Buffers;
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
                throw new UdpOpenException("UDP sender is disposed.");
            }

            if (options is null)
            {
                throw new UdpOpenException("UDP transport options are required.");
            }

            UdpTransportOptions normalized = options.WithFallbackPort(
                options.Mode == UdpTransportMode.Multicast ? options.MulticastSendPort : options.BroadcastPort);

            if (!normalized.IsEnabled)
            {
                throw new UdpOpenException("UDP transport is disabled.");
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
        catch (NetworkException ex)
        {
            error = ex.Message;
            _isOpen = false;
            return false;
        }
        catch (Exception ex)
        {
            error = new UdpOpenException($"UDP open failed: {ex.Message}", ex).Message;
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
                throw new UdpSendException("UDP sender is not open.");
            }

            if (!_options.IsEnabled)
            {
                throw new UdpSendException("UDP transport is disabled.");
            }

            int targetPort = _options.ResolveTargetPort(port);
            if (targetPort is < 1 or > 65535)
            {
                throw new UdpSendException("UDP port must be between 1 and 65535.");
            }

            string targetAddress = string.IsNullOrWhiteSpace(addressOverride)
                ? _options.ResolveTargetAddress()
                : addressOverride.Trim();
            if (!IPAddress.TryParse(targetAddress, out IPAddress? address))
            {
                throw new UdpSendException($"Invalid UDP address: {targetAddress}");
            }

            byte[] bytes = ArrayPool<byte>.Shared.Rent(sentence.Length);
            try
            {
                int count = Encoding.ASCII.GetBytes(sentence, 0, sentence.Length, bytes, 0);
                _client.Send(bytes, count, new IPEndPoint(address, targetPort));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
            return true;
        }
        catch (NetworkException ex)
        {
            error = ex.Message;
            _isOpen = false;
            return false;
        }
        catch (Exception ex)
        {
            error = new UdpSendException($"UDP send failed: {ex.Message}", ex).Message;
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