using System.IO;
using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services.Projects.PS2404A;

public sealed class Ps2404aUdpTransportProfileStore : BaseProjectUdpTransportProfileStore
{
    private const string FileName = "NMEAMultiCast.ini";
    private const string SettingSection = "SETTING";
    private const string BroadcastSection = "BROADCAST";
    private const string MulticastSection = "MULTICAST";

    public override ProjectType ProjectType => ProjectType.PS2404A;

    public override UdpTransportOptions Load(string configDirectory, int fallbackPort)
    {
        string directory = EnsureDirectory(configDirectory);
        string path = Path.Combine(directory, FileName);

        IIniFileService ini = File.Exists(path)
            ? IniFileService.Load(path)
            : new IniFileService();

        ini.Set(SettingSection, "SET option", "Broadcast:1, Multicast:0");

        int useValue = ini.GetInt(BroadcastSection, "USE", 1);
        int broadcastPort = UdpTransportOptions.NormalizePort(
            ini.GetInt(BroadcastSection, "PORT NO", 49552),
            fallbackPort);
        int multicastPortNo = UdpTransportOptions.NormalizePort(
            ini.GetInt(MulticastSection, "PORT NO", 6000),
            6000);
        int multicastSendPort = UdpTransportOptions.NormalizePort(
            ini.GetInt(MulticastSection, "SEND PORT", 6000),
            fallbackPort);
        string multicastAddress = UdpTransportOptions.NormalizeAddress(
            ini.Get(MulticastSection, "SEND ADDRESS", "225.0.0.0"),
            "225.0.0.0");

        UdpTransportOptions loaded = new(
            Mode: ParseMode(useValue),
            BroadcastPort: broadcastPort,
            MulticastPortNo: multicastPortNo,
            MulticastSendPort: multicastSendPort,
            MulticastAddress: multicastAddress,
            MulticastTtl: 32,
            MulticastInterfaceAddress: "0.0.0.0",
            UseRequestedPort: true);

        UdpTransportOptions normalized = loaded.WithFallbackPort(fallbackPort);
        ini.Set(BroadcastSection, "USE", SerializeMode(normalized.Mode).ToString());
        ini.Set(BroadcastSection, "PORT NO", normalized.BroadcastPort.ToString());
        ini.Set(MulticastSection, "PORT NO", normalized.MulticastPortNo.ToString());
        ini.Set(MulticastSection, "SEND PORT", normalized.MulticastSendPort.ToString());
        ini.Set(MulticastSection, "SEND ADDRESS", normalized.MulticastAddress);
        ini.Save(path);

        return normalized;
    }

    public override void Save(string configDirectory, UdpTransportOptions options)
    {
        string directory = EnsureDirectory(configDirectory);
        string path = Path.Combine(directory, FileName);

        IniFileService ini = File.Exists(path)
            ? IniFileService.Load(path)
            : new IniFileService();

        UdpTransportOptions normalized = options.WithFallbackPort(options.BroadcastPort);

        ini.Set(SettingSection, "SET option", "Broadcast:1, Multicast:0");
        ini.Set(BroadcastSection, "USE", SerializeMode(normalized.Mode).ToString());
        ini.Set(BroadcastSection, "PORT NO", normalized.BroadcastPort.ToString());
        ini.Set(MulticastSection, "PORT NO", normalized.MulticastPortNo.ToString());
        ini.Set(MulticastSection, "SEND PORT", normalized.MulticastSendPort.ToString());
        ini.Set(MulticastSection, "SEND ADDRESS", normalized.MulticastAddress);

        ini.Save(path);
    }

    private static UdpTransportMode ParseMode(int rawMode)
    {
        return rawMode switch
        {
            0 => UdpTransportMode.Multicast,
            2 => UdpTransportMode.Disabled,
            _ => UdpTransportMode.Broadcast
        };
    }

    private static int SerializeMode(UdpTransportMode mode)
    {
        return mode switch
        {
            UdpTransportMode.Multicast => 0,
            UdpTransportMode.Disabled => 2,
            _ => 1
        };
    }

    private static string EnsureDirectory(string? directory)
    {
        string resolved = string.IsNullOrWhiteSpace(directory)
            ? AppContext.BaseDirectory
            : directory.Trim();

        Directory.CreateDirectory(resolved);
        return resolved;
    }
}
