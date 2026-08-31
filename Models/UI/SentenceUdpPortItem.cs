using CommunityToolkit.Mvvm.ComponentModel;

namespace NMEASender.Wpf.Models.UI;

public sealed partial class SentenceUdpPortItem : ObservableObject
{
    public SentenceUdpPortItem(string rowKey, string sentenceLabel, int udpPort, string udpAddress, double hz, string talkerId)
    {
        RowKey = rowKey;
        SentenceLabel = sentenceLabel;
        _udpPort = udpPort;
        _udpAddress = (udpAddress ?? string.Empty).Trim();
        _hz = hz;
        _talkerId = NormalizeTalkerId(talkerId);
    }

    public string RowKey { get; }

    public string SentenceLabel { get; }

    [ObservableProperty]
    private int _udpPort;

    [ObservableProperty]
    private string _udpAddress = string.Empty;

    [ObservableProperty]
    private double _hz;

    [ObservableProperty]
    private string _talkerId = string.Empty;

    partial void OnUdpAddressChanged(string value)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            UdpAddress = normalized;
        }
    }

    partial void OnTalkerIdChanged(string value)
    {
        string normalized = NormalizeTalkerId(value);
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            TalkerId = normalized;
        }
    }

    private static string NormalizeTalkerId(string? talkerId)
    {
        string trimmed = (talkerId ?? string.Empty).Trim().ToUpperInvariant();
        return trimmed.Length > 2 ? trimmed[..2] : trimmed;
    }
}
