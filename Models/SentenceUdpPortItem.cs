using CommunityToolkit.Mvvm.ComponentModel;

namespace NMEASender.Wpf.Models;

public sealed partial class SentenceUdpPortItem : ObservableObject
{
    public SentenceUdpPortItem(string rowKey, string sentenceLabel, int udpPort)
    {
        RowKey = rowKey;
        SentenceLabel = sentenceLabel;
        _udpPort = udpPort;
    }

    public string RowKey { get; }

    public string SentenceLabel { get; }

    [ObservableProperty]
    private int _udpPort;
}
