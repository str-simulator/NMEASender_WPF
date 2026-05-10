using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NMEASender.Wpf.Services;

public sealed class UdpBroadcastSender : IDisposable
{
    private readonly UdpClient _client = new(AddressFamily.InterNetwork);
    private IPEndPoint? _endPoint;
    private bool _disposed;

    public bool IsOpen => !_disposed && _endPoint is not null;

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

            _client.EnableBroadcast = true;
            _endPoint = new IPEndPoint(IPAddress.Broadcast, port);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _endPoint = null;
            return false;
        }
    }

    public bool Send(string sentence, out string error)
    {
        error = string.Empty;
        try
        {
            if (_endPoint is null)
            {
                error = "UDP sender is not open.";
                return false;
            }

            byte[] bytes = Encoding.ASCII.GetBytes(sentence);
            _client.Send(bytes, bytes.Length, _endPoint);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _endPoint = null;
            return false;
        }
    }

    public void Close()
    {
        _endPoint = null;
    }

    public void Dispose()
    {
        Close();
        _client.Dispose();
        _disposed = true;
    }
}
