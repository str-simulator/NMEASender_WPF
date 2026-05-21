using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface IProjectSentenceFrameService
{
    void Reset(ProjectType projectType, bool rightRpmFirst);

    bool SupportsPerSentenceMulticastAddress(ProjectType projectType);

    IReadOnlyList<SentenceItem> SelectForDispatch(
        IReadOnlyList<SentenceItem> enabledSentences,
        ProjectType projectType);

    IReadOnlyList<string> ExpandForTransmit(
        IReadOnlyList<string> sentences,
        NmeaSentenceId sentenceId,
        ProjectType projectType);

    int ResolveUdpPort(
        SentenceItem item,
        int defaultUdpPort,
        ProjectType projectType);

    string? ResolveUdpAddress(
        SentenceItem item,
        UdpTransportOptions options,
        ProjectType projectType);
}
