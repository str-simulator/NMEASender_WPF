using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Models.UI;

namespace NMEASender.Wpf.Services.Interfaces.Projects;

public interface IProjectSentenceFramePolicy
{
    ProjectType ProjectType { get; }

    bool SupportsPerSentenceMulticastAddress { get; }

    void Reset(bool rightRpmFirst);

    IReadOnlyList<SentenceItem> SelectForDispatch(IReadOnlyList<SentenceItem> enabledSentences);

    IReadOnlyList<string> ExpandForTransmit(IReadOnlyList<string> sentences, NmeaSentenceId sentenceId);

    int ResolveUdpPort(SentenceItem item, int defaultUdpPort);

    string? ResolveUdpAddress(SentenceItem item, UdpTransportOptions options);
}
