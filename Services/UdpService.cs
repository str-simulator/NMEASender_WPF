using System.Net;
using System.Net.Sockets;
using System.Text;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services;

public sealed class UdpService : IUdpService
{
    private readonly UdpClient _client = new(AddressFamily.InterNetwork);
    private bool _isOpen;
    private bool _disposed;

    public bool IsOpen => !_disposed && _isOpen;

    public bool Open(int port, out string error)
    {
        error = string.Empty;
        try
        {
            if (_disposed)
            {
                error = "UDP sender is disposed.";
                return false;
            }

            if (port is < 1 or > 65535)
            {
                error = "UDP port must be between 1 and 65535.";
                return false;
            }

            _client.EnableBroadcast = true;
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

    public bool Send(string sentence, int port, out string error)
    {
        error = string.Empty;
        try
        {
            if (!_isOpen)
            {
                error = "UDP sender is not open.";
                return false;
            }

            if (port is < 1 or > 65535)
            {
                error = "UDP port must be between 1 and 65535.";
                return false;
            }

            byte[] bytes = Encoding.ASCII.GetBytes(sentence);
            IPEndPoint endPoint = new(IPAddress.Broadcast, port);
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
