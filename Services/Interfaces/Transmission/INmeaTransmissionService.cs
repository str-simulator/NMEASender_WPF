using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Network;

namespace NMEASender.Wpf.Services.Interfaces.Transmission;

public interface INmeaTransmissionService
{
    Task<TransmissionStartResult> StartAsync(TransmissionStartContext context, Action<string> addLog);

    void Stop(bool wasRunning, Action<string> addLog);

    void HandleUdpToggleDuringRun(bool isRunning, bool isOpening, bool useUdp, UdpTransportOptions options, Action<string> addLog);

    IReadOnlyList<SentenceSendTask> ComposeTick(TransmissionTickContext context, Action<string> addLog);

    void ExecuteSend(IReadOnlyList<SentenceSendTask> tasks, Action<string> addLog, Action stopAction);
}
