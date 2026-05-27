using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.IO;
using NMEASender.Wpf.Services.Interfaces.Ports;
using NMEASender.Wpf.Services.Interfaces.Transmission;
using System.Diagnostics;

namespace NMEASender.Wpf.Services.Transmission;

public sealed class NmeaTransmissionService : INmeaTransmissionService
{
    private readonly IOutputChannelService _outputChannelService;
    private readonly ISentenceComposerService _sentenceComposer;
    private readonly IPortBaudRateService _portBaudRateService;
    private readonly IProjectSentenceFrameService _projectSentenceFrameService;

    public NmeaTransmissionService(
        IOutputChannelService outputChannelService,
        ISentenceComposerService sentenceComposer,
        IPortBaudRateService portBaudRateService,
        IProjectSentenceFrameService projectSentenceFrameService)
    {
        _outputChannelService = outputChannelService ?? throw new ArgumentNullException(nameof(outputChannelService));
        _sentenceComposer = sentenceComposer ?? throw new ArgumentNullException(nameof(sentenceComposer));
        _portBaudRateService = portBaudRateService ?? throw new ArgumentNullException(nameof(portBaudRateService));
        _projectSentenceFrameService = projectSentenceFrameService ?? throw new ArgumentNullException(nameof(projectSentenceFrameService));
    }

    public async Task<TransmissionStartResult> StartAsync(TransmissionStartContext context, Action<string> addLog)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        _projectSentenceFrameService.Reset(context.Config.ProjectType, context.Config.RightRpm);

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
                context.UdpTransportOptions);

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
                    addLog($"UDP {context.UdpTransportOptions.Mode} Open: {context.UdpPort}");
                }
                else
                {
                    addLog($"UDP {context.UdpTransportOptions.Mode} Open Fail: {openResult.UdpOpenError}");
                }
            }
        }
        else
        {
            addLog("No COM port selected; UDP only");

            if (context.UseUdp)
            {
                if (_outputChannelService.TryOpenUdp(context.UdpTransportOptions, out string? udpError))
                {
                    addLog($"UDP {context.UdpTransportOptions.Mode} Open: {context.UdpPort}");
                }
                else
                {
                    addLog($"UDP {context.UdpTransportOptions.Mode} Open Fail: {udpError}");
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

    public void HandleUdpToggleDuringRun(bool isRunning, bool isOpening, bool useUdp, UdpTransportOptions options, Action<string> addLog)
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
                addLog("UDP Close");
            }

            return;
        }

        if (_outputChannelService.IsUdpOpen)
        {
            return;
        }

        if (_outputChannelService.TryOpenUdp(options, out string? udpError))
        {
            addLog($"UDP {options.Mode} Open");
            return;
        }

        addLog($"UDP {options.Mode} Open Fail: {udpError}");
    }

    public void DispatchTick(TransmissionTickContext context, Action<string> addLog, Action stopAction)
    {
        IReadOnlyList<SentenceItem> dispatchSentences = _projectSentenceFrameService.SelectForDispatch(
            context.EnabledSentences,
            context.BuildOptions.ProjectType);

        foreach (SentenceItem item in dispatchSentences)
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
            IReadOnlyList<string> framedSentences = _projectSentenceFrameService.ExpandForTransmit(
                sentences,
                item.Id,
                context.BuildOptions.ProjectType);

            if (item.IsComEnabled &&
                !string.IsNullOrWhiteSpace(item.PortName) &&
                _outputChannelService.IsComPortOpen(item.PortName))
            {
                if (!SendToCom(item, framedSentences, addLog, stopAction))
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
                int udpPort = _projectSentenceFrameService.ResolveUdpPort(
                    item,
                    context.DefaultUdpPort,
                    context.BuildOptions.ProjectType);
                string? udpAddress = _projectSentenceFrameService.ResolveUdpAddress(
                    item,
                    context.UdpTransportOptions,
                    context.BuildOptions.ProjectType);
                SendToUdp(item, framedSentences, udpPort, udpAddress, addLog, stopAction);
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

    private void SendToUdp(SentenceItem item, IReadOnlyList<string> sentences, int udpPort, string? udpAddress, Action<string> addLog, Action stopAction)
    {
        string udpPrefix = string.IsNullOrWhiteSpace(udpAddress)
            ? $"UDP:{udpPort}"
            : $"UDP:{udpAddress}:{udpPort}";

        foreach (string sentence in sentences)
        {
            if (_outputChannelService.TrySendUdp(sentence, udpPort, udpAddress, out string? error))
            {
                if (item.Id != NmeaSentenceId.STR)
                {
                    addLog($"{udpPrefix} {sentence.TrimEnd()}");
                }

                continue;
            }

            addLog($"{udpPrefix} {item.Label} Send Fail: {error}");
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
