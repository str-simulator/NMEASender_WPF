using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Models;

public sealed record TransmissionStartContext(
    INmeaSenderConfigService Config,
    IReadOnlyList<string> EnabledPorts,
    bool UseUdp,
    int UdpPort,
    UdpTransportOptions UdpTransportOptions,
    bool IsIosSource);

public sealed record TransmissionStartResult(
    bool Started,
    IReadOnlyList<PortOpenOutcome> FailedComPorts);

public sealed record TransmissionTickContext(
    IReadOnlyList<SentenceItem> EnabledSentences,
    NmeaDataDto Data,
    bool IsIosSource,
    NmeaBuildOptions BuildOptions,
    int DefaultUdpPort,
    UdpTransportOptions UdpTransportOptions);
