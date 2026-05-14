using NMEASender.Wpf.Services;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Models;

public sealed record TransmissionStartContext(
    INmeaSenderConfigService Config,
    IReadOnlyList<string> EnabledPorts,
    bool UseUdp,
    int UdpPort,
    bool IsIosSource);

public sealed record TransmissionTickContext(
    IReadOnlyList<SentenceItem> EnabledSentences,
    NmeaDataDto Data,
    bool IsIosSource,
    NmeaBuildOptions BuildOptions,
    string UdpPortText);
