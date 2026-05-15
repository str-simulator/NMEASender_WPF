using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface INmeaTransmissionService
{
    Task<TransmissionStartResult> StartAsync(TransmissionStartContext context, Action<string> addLog);

    void Stop(bool wasRunning, Action<string> addLog);

    void HandleUdpToggleDuringRun(bool isRunning, bool isOpening, bool useUdp, int udpPort, Action<string> addLog);

    void DispatchTick(TransmissionTickContext context, Action<string> addLog, Action stopAction);
}
