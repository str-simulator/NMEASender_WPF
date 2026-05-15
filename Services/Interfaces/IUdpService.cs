namespace NMEASender.Wpf.Services.Interfaces;

public interface IUdpService : IDisposable
{
    bool IsOpen { get; }

    bool Open(int port, out string error);

    bool Send(string sentence, int port, out string error);

    void Close();
}
