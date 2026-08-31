using CommunityToolkit.Mvvm.ComponentModel;
using NMEASender.Wpf.Models.Core;
using System.Net;
using System.Net.Sockets;

namespace NMEASender.Wpf.Models.UI;

public sealed partial class SentenceItem : ObservableObject
{
    [ObservableProperty]
    private bool _isComEnabled;

    [ObservableProperty]
    private bool _isUdpEnabled;

    [ObservableProperty]
    private bool _isDuplicateRow;

    [ObservableProperty]
    private string _portName;

    [ObservableProperty]
    private int _udpPort;

    [ObservableProperty]
    private string _udpAddress = string.Empty;

    [ObservableProperty]
    private double _hz;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayLabel))]
    private string _talkerId = string.Empty;

    [ObservableProperty]
    private string _primaryText = string.Empty;

    [ObservableProperty]
    private string _secondaryText = string.Empty;

    // Runtime-only send-scheduling state (not persisted, not UI-bound):
    // ticks (Environment.TickCount64) at which this sentence was last actually sent.
    public long LastSentTicks { get; set; } = long.MinValue;

    public SentenceItem(
        NmeaSentenceId id,
        NmeaSendFlag flag,
        string label,
        string portName,
        bool isComEnabled,
        bool isUdpEnabled,
        int udpPort,
        string udpAddress = "",
        double hz = DefaultHz,
        string talkerId = "",
        bool hasSecondary = false,
        bool isDuplicateRow = false)
    {
        Id = id;
        Flag = flag;
        Label = label;
        _portName = portName;
        _isComEnabled = isComEnabled;
        _isUdpEnabled = isUdpEnabled;
        _udpPort = NormalizeUdpPort(udpPort);
        _udpAddress = NormalizeUdpAddress(udpAddress);
        _hz = NormalizeHz(hz);
        _talkerId = NormalizeTalkerId(talkerId);
        HasSecondary = hasSecondary;
        _isDuplicateRow = isDuplicateRow;
    }

    public const double DefaultHz = 1.0;
    public const double MinHz = 0.1;

    public NmeaSentenceId Id { get; }

    public NmeaSendFlag Flag { get; }

    public string Label { get; }

    public string DisplayLabel => BuildDisplayLabel(Label, TalkerId);

    public bool HasSecondary { get; }

    partial void OnPortNameChanged(string value)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            PortName = normalized;
        }
    }

    partial void OnUdpPortChanged(int value)
    {
        int normalized = NormalizeUdpPort(value);
        if (value != normalized)
        {
            UdpPort = normalized;
        }
    }

    partial void OnUdpAddressChanged(string value)
    {
        string normalized = NormalizeUdpAddress(value);
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            UdpAddress = normalized;
        }
    }

    partial void OnHzChanged(double value)
    {
        double normalized = NormalizeHz(value);
        if (Math.Abs(value - normalized) > double.Epsilon)
        {
            Hz = normalized;
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

    private static double NormalizeHz(double hz)
    {
        return double.IsFinite(hz) && hz >= MinHz ? hz : DefaultHz;
    }

    private static string NormalizeTalkerId(string? talkerId)
    {
        string trimmed = (talkerId ?? string.Empty).Trim().ToUpperInvariant();
        return trimmed.Length > 2 ? trimmed[..2] : trimmed;
    }

    private static string BuildDisplayLabel(string label, string talkerId)
    {
        if (label.Length < 3 || label[0] != '$')
        {
            return label;
        }

        string effectiveTalkerId = talkerId.Length == 2 ? talkerId : "--";
        return $"${effectiveTalkerId}{label[3..]}";
    }

    private static int NormalizeUdpPort(int port)
    {
        return port is >= 1 and <= 65535 ? port : 40014;
    }

    private static string NormalizeUdpAddress(string? value)
    {
        string candidate = (value ?? string.Empty).Trim();
        if (candidate.Length == 0)
        {
            return string.Empty;
        }

        if (IPAddress.TryParse(candidate, out IPAddress? address) &&
            address.AddressFamily == AddressFamily.InterNetwork)
        {
            return candidate;
        }

        return string.Empty;
    }
}
