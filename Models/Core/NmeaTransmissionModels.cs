using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.Config;

namespace NMEASender.Wpf.Models.Core;

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

public sealed record SentenceSendTask(
    SentenceItem Item,
    IReadOnlyList<string> FramedSentences,
    bool IsComEnabled,
    bool IsUdpEnabled,
    int UdpPort,
    string? UdpAddress);
