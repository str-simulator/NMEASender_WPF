using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Network;

namespace NMEASender.Wpf.Services.Interfaces.Transmission;

public interface INmeaTransmissionService
{
    Task<TransmissionStartResult> StartAsync(TransmissionStartContext context, Action<string> addLog);

    void Stop(bool wasRunning, Action<string> addLog);

    void HandleUdpToggleDuringRun(bool isRunning, bool isOpening, bool useUdp, UdpTransportOptions options, Action<string> addLog);

    void DispatchTick(TransmissionTickContext context, Action<string> addLog, Action stopAction);
}
