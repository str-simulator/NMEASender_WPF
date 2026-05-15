using System.Diagnostics;
using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services;

public sealed class NmeaTransmissionService : INmeaTransmissionService
{
    private readonly IOutputChannelService _outputChannelService;
    private readonly ISentenceComposerService _sentenceComposer;
    private readonly IPortBaudRateService _portBaudRateService;

    public NmeaTransmissionService(
        IOutputChannelService outputChannelService,
        ISentenceComposerService sentenceComposer,
        IPortBaudRateService portBaudRateService)
    {
        _outputChannelService = outputChannelService ?? throw new ArgumentNullException(nameof(outputChannelService));
        _sentenceComposer = sentenceComposer ?? throw new ArgumentNullException(nameof(sentenceComposer));
        _portBaudRateService = portBaudRateService ?? throw new ArgumentNullException(nameof(portBaudRateService));
    }

    public async Task<TransmissionStartResult> StartAsync(TransmissionStartContext context, Action<string> addLog)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.EnabledPorts.Count == 0 && !context.UseUdp)
        {
            addLog("No COM port selected");
            return new TransmissionStartResult(false, Array.Empty<PortOpenOutcome>());
        }

        List<PortOpenOutcome> failedComPorts = new();

        if (context.EnabledPorts.Count > 0)
        {
            addLog($"Opening {context.EnabledPorts.Count} COM port(s)...");

            OutputOpenRequest request = new(
                context.EnabledPorts,
                context.Config.BaudRate,
                context.Config.PortBaudRates,
                context.Config.DataBits,
                context.Config.Parity,
                context.Config.StopBits,
                context.UseUdp,
                context.UdpPort);

            OutputOpenResult openResult = await _outputChannelService.OpenAsync(request);
            foreach (PortOpenOutcome result in openResult.PortResults)
            {
                if (result.Success)
                {
                    int baudRate = _portBaudRateService.ResolveBaudRate(context.Config, result.PortName);
                    addLog($"{result.PortName} Open Success ({baudRate} bps)");
                }
                else
                {
                    addLog($"{result.PortName} Open Fail: {result.Error}");
                    failedComPorts.Add(result);
                }
            }

            if (context.UseUdp)
            {
                if (openResult.UdpOpenSuccess)
                {
                    addLog($"UDP Broadcast Open: {context.UdpPort}");
                }
                else
                {
                    addLog($"UDP Broadcast Open Fail: {openResult.UdpOpenError}");
                }
            }
        }
        else
        {
            addLog("No COM port selected; UDP only");

            if (context.UseUdp)
            {
                if (_outputChannelService.TryOpenUdp(context.UdpPort, out string? udpError))
                {
                    addLog($"UDP Broadcast Open: {context.UdpPort}");
                }
                else
                {
                    addLog($"UDP Broadcast Open Fail: {udpError}");
                }
            }
        }

        if (_outputChannelService.OpenComPortCount == 0 && !_outputChannelService.IsUdpOpen)
        {
            addLog("Send stopped: no output opened.");
            return new TransmissionStartResult(false, failedComPorts);
        }

        addLog(context.IsIosSource
            ? "By IOS selected: reading STR_OWNSHIP_DATA"
            : "TEST selected: current input values are used");

        return new TransmissionStartResult(true, failedComPorts);
    }

    public void Stop(bool wasRunning, Action<string> addLog)
    {
        _outputChannelService.CloseAll();
        if (wasRunning)
        {
            addLog("COM Close");
        }
    }

    public void HandleUdpToggleDuringRun(bool isRunning, bool isOpening, bool useUdp, int udpPort, Action<string> addLog)
    {
        if (!isRunning || isOpening)
        {
            return;
        }

        if (!useUdp)
        {
            if (_outputChannelService.IsUdpOpen)
            {
                _outputChannelService.CloseUdp();
                addLog("UDP Broadcast Close");
            }

            return;
        }

        if (_outputChannelService.IsUdpOpen)
        {
            return;
        }

        if (_outputChannelService.TryOpenUdp(udpPort, out string? udpError))
        {
            addLog($"UDP Broadcast Open: {udpPort}");
            return;
        }

        addLog($"UDP Broadcast Open Fail: {udpError}");
    }

    public void DispatchTick(TransmissionTickContext context, Action<string> addLog, Action stopAction)
    {
        foreach (SentenceItem item in context.EnabledSentences)
        {
            if (!_sentenceComposer.ShouldSend(item, context.IsIosSource, context.Data))
            {
                continue;
            }

            IReadOnlyList<string> sentences = _sentenceComposer.ComposeAndApplyPreview(
                item,
                context.Data,
                context.IsIosSource,
                context.BuildOptions);

            if (item.IsComEnabled &&
                !string.IsNullOrWhiteSpace(item.PortName) &&
                _outputChannelService.IsComPortOpen(item.PortName))
            {
                if (!SendToCom(item, sentences, addLog, stopAction))
                {
                    continue;
                }
            }
            else if (item.IsComEnabled && !_outputChannelService.IsUdpOpen && string.IsNullOrWhiteSpace(item.PortName))
            {
                addLog($"{item.Label} COM not selected");
            }

            if (item.IsUdpEnabled && _outputChannelService.IsUdpOpen)
            {
                int udpPort = item.UdpPort is >= 1 and <= 65535 ? item.UdpPort : context.DefaultUdpPort;
                SendToUdp(item, sentences, udpPort, addLog, stopAction);
            }
        }
    }

    private bool SendToCom(SentenceItem item, IReadOnlyList<string> sentences, Action<string> addLog, Action stopAction)
    {
        foreach (string sentence in sentences)
        {
            if (_outputChannelService.TryWriteCom(item.PortName, sentence, out string? error))
            {
                if (item.Id == NmeaSentenceId.STR)
                {
                    Debug.WriteLine($"{item.PortName} {sentence.TrimEnd()}");
                    continue;
                }

                addLog($"{item.PortName} {sentence.TrimEnd()}");
                continue;
            }

            addLog($"{item.PortName} {item.Label} Send Fail: {error}");
            _outputChannelService.MarkComPortClosed(item.PortName);
            addLog($"{item.PortName} disabled for this run.");
            StopIfNoOutputIsOpen(addLog, stopAction);
            return false;
        }

        return true;
    }

    private void SendToUdp(SentenceItem item, IReadOnlyList<string> sentences, int udpPort, Action<string> addLog, Action stopAction)
    {
        foreach (string sentence in sentences)
        {
            if (_outputChannelService.TrySendUdp(sentence, udpPort, out string? error))
            {
                if (item.Id != NmeaSentenceId.STR)
                {
                    addLog($"UDP:{udpPort} {sentence.TrimEnd()}");
                }

                continue;
            }

            addLog($"UDP:{udpPort} {item.Label} Send Fail: {error}");
            _outputChannelService.CloseUdp();
            StopIfNoOutputIsOpen(addLog, stopAction);
            break;
        }
    }

    private void StopIfNoOutputIsOpen(Action<string> addLog, Action stopAction)
    {
        if (_outputChannelService.OpenComPortCount > 0 || _outputChannelService.IsUdpOpen)
        {
            return;
        }

        addLog("Send stopped: all outputs are closed.");
        stopAction();
    }
}
