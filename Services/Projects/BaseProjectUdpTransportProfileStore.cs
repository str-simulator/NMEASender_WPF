using System.IO;
using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services.Projects;

public abstract class BaseProjectUdpTransportProfileStore : IProjectUdpTransportProfileStore
{
    protected const string FileName = "UDPConfig.ini";
    protected const string SettingSection = "SETTING";
    protected const string BroadcastSection = "BROADCAST";
    protected const string MulticastSection = "MULTICAST";

    public abstract ProjectType ProjectType { get; }

    protected virtual int DefaultBroadcastPort => 40014;

    protected virtual int DefaultMulticastPortNo => 6000;

    protected virtual int DefaultMulticastSendPort => 40014;

    protected virtual string DefaultMulticastAddress => "225.0.0.0";

    protected virtual bool UseRequestedPort => true;

    protected virtual string? LegacyFileName => null;

    public virtual UdpTransportOptions Load(string configDirectory, int fallbackPort)
    {
        string directory = EnsureDirectory(configDirectory);
        string path = Path.Combine(directory, FileName);
        string loadPath = ResolveLoadPath(directory, path);

        IIniFileService ini = File.Exists(loadPath)
            ? IniFileService.Load(loadPath)
            : new IniFileService();

        ini.Set(SettingSection, "SET option", "Broadcast:1, Multicast:0");

        int useValue = ini.GetInt(BroadcastSection, "USE", 1);
        int broadcastPort = UdpTransportOptions.NormalizePort(
            ini.GetInt(BroadcastSection, "PORT NO", DefaultBroadcastPort),
            fallbackPort);
        int multicastPortNo = UdpTransportOptions.NormalizePort(
            ini.GetInt(MulticastSection, "PORT NO", DefaultMulticastPortNo),
            DefaultMulticastPortNo);
        int multicastSendPort = UdpTransportOptions.NormalizePort(
            ini.GetInt(MulticastSection, "SEND PORT", DefaultMulticastSendPort),
            fallbackPort);
        string multicastAddress = UdpTransportOptions.NormalizeAddress(
            ini.Get(MulticastSection, "SEND ADDRESS", DefaultMulticastAddress),
            DefaultMulticastAddress);

        UdpTransportOptions loaded = new(
            Mode: ParseMode(useValue),
            BroadcastPort: broadcastPort,
            MulticastPortNo: multicastPortNo,
            MulticastSendPort: multicastSendPort,
            MulticastAddress: multicastAddress,
            MulticastTtl: 32,
            MulticastInterfaceAddress: "0.0.0.0",
            UseRequestedPort: UseRequestedPort);

        UdpTransportOptions normalized = loaded.WithFallbackPort(fallbackPort);
        Save(configDirectory, normalized);
        return normalized;
    }

    public virtual void Save(string configDirectory, UdpTransportOptions options)
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

    protected static UdpTransportMode ParseMode(int rawMode)
    {
        return rawMode switch
        {
            0 => UdpTransportMode.Multicast,
            2 => UdpTransportMode.Disabled,
            _ => UdpTransportMode.Broadcast
        };
    }

    protected static int SerializeMode(UdpTransportMode mode)
    {
        return mode switch
        {
            UdpTransportMode.Multicast => 0,
            UdpTransportMode.Disabled => 2,
            _ => 1
        };
    }

    protected static string EnsureDirectory(string? directory)
    {
        string resolved = string.IsNullOrWhiteSpace(directory)
            ? AppContext.BaseDirectory
            : directory.Trim();

        Directory.CreateDirectory(resolved);
        return resolved;
    }

    private string ResolveLoadPath(string directory, string defaultPath)
    {
        if (File.Exists(defaultPath) || string.IsNullOrWhiteSpace(LegacyFileName))
        {
            return defaultPath;
        }

        string legacyPath = Path.Combine(directory, LegacyFileName);
        return File.Exists(legacyPath) ? legacyPath : defaultPath;
    }
}
