using CommunityToolkit.Mvvm.ComponentModel;

namespace NMEASender.Wpf.Models.UI;

public sealed partial class SentenceUdpPortItem : ObservableObject
{
    public SentenceUdpPortItem(string rowKey, string sentenceLabel, int udpPort, string udpAddress)
    {
        RowKey = rowKey;
        SentenceLabel = sentenceLabel;
        _udpPort = udpPort;
        _udpAddress = (udpAddress ?? string.Empty).Trim();
    }

    public string RowKey { get; }

    public string SentenceLabel { get; }

    [ObservableProperty]
    private int _udpPort;

    [ObservableProperty]
    private string _udpAddress = string.Empty;

    partial void OnUdpAddressChanged(string value)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            UdpAddress = normalized;
        }
    }
}
