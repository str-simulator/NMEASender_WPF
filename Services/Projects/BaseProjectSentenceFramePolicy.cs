using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace NMEASender.Wpf.Services.Projects;

public abstract class BaseProjectSentenceFramePolicy : IProjectSentenceFramePolicy
{
    public abstract ProjectType ProjectType { get; }

    public virtual bool SupportsPerSentenceMulticastAddress => false;

    public virtual void Reset(bool rightRpmFirst)
    {
    }

    public virtual IReadOnlyList<SentenceItem> SelectForDispatch(IReadOnlyList<SentenceItem> enabledSentences)
    {
        return enabledSentences;
    }

    public virtual IReadOnlyList<string> ExpandForTransmit(IReadOnlyList<string> sentences, NmeaSentenceId sentenceId)
    {
        return sentences;
    }

    public virtual int ResolveUdpPort(SentenceItem item, int defaultUdpPort)
    {
        int fallbackPort = defaultUdpPort is >= 1 and <= 65535 ? defaultUdpPort : 40014;
        return item.UdpPort is >= 1 and <= 65535
            ? item.UdpPort
            : fallbackPort;
    }

    public virtual string? ResolveUdpAddress(SentenceItem item, UdpTransportOptions options)
    {
        if (!SupportsPerSentenceMulticastAddress || options.Mode != UdpTransportMode.Multicast)
        {
            return null;
        }

        string candidate = (item.UdpAddress ?? string.Empty).Trim();
        if (!IPAddress.TryParse(candidate, out IPAddress? address) ||
            address.AddressFamily != AddressFamily.InterNetwork)
        {
            return null;
        }

        byte[] bytes = address.GetAddressBytes();
        return bytes.Length == 4 && bytes[0] is >= 224 and <= 239
            ? candidate
            : null;
    }
}
