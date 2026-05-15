using System.IO;
using System.IO.Ports;
using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services;

public sealed class NmeaSenderConfigService : INmeaSenderConfigService
{
    private const string GpsSection = "GPS CONFIG";
    private const string ConfigSection = "CONFIG";
    private const string ProjectKey = "Project";
    private const string LegacyProjectKey = "PROJECT TYPE";
    private const string SocketSection = "SOCKET";
    private const string PortsSection = "SENTENCE PORTS";
    private const string UdpPortsSection = "UDP PORTS";
    private const string BaudSection = "BAUD RATE";

    public string Title { get; set; } = "ECDIS Sender";
    public string DefaultPort { get; set; } = "COM1";
    public int BaudRate { get; set; } = 19200;
    public int DataBits { get; set; } = 8;
    public Parity Parity { get; set; } = Parity.None;
    public StopBits StopBits { get; set; } = StopBits.One;
    public int SendInterval { get; set; } = 500;
    public bool RightRpm { get; set; } = true;
    public bool TrueWind { get; set; } = true;
    public bool UseHdmOutput { get; set; } = true;
    public bool UseUdp { get; set; } = true;
    public int UdpPort { get; set; } = 40014;
    public ProjectType ProjectType { get; set; } = global::NMEASender.Wpf.Models.ProjectType.PS2603;
    public NmeaSendFlag SendFlag { get; set; } = DefaultSendFlag;
    public NmeaSendFlag UdpSendFlag { get; set; } = DefaultSendFlag;
    public Dictionary<NmeaSentenceId, string> SentencePorts { get; } = new();
    public Dictionary<NmeaSentenceId, List<string>> SentencePortRows { get; } = new();
    public Dictionary<NmeaSentenceId, List<int>> SentenceUdpPortRows { get; } = new();
    public Dictionary<string, int> PortBaudRates { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string SavePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "NMEASender.Wpf.ini");

    public static NmeaSendFlag DefaultSendFlag =>
        NmeaSendFlag.Rmc | NmeaSendFlag.Gga | NmeaSendFlag.Gll | NmeaSendFlag.Vtg | NmeaSendFlag.Zda |
        NmeaSendFlag.Hdt | NmeaSendFlag.Vbw | NmeaSendFlag.Rot | NmeaSendFlag.Rsa | NmeaSendFlag.RpmPort | NmeaSendFlag.RpmStbd |
        NmeaSendFlag.Mwv | NmeaSendFlag.Hdg | NmeaSendFlag.Vdm | NmeaSendFlag.Dpt | NmeaSendFlag.Dbt |
        NmeaSendFlag.Etl | NmeaSendFlag.Cur | NmeaSendFlag.Mda | NmeaSendFlag.Trc | NmeaSendFlag.Trd |
        NmeaSendFlag.Hpm | NmeaSendFlag.Hrm | NmeaSendFlag.Vdo;

    public static NmeaSenderConfigService Load()
    {
        string? basePath = FindUpwards("NMEASender.ini");
        string? savePath = basePath is null
            ? Path.Combine(AppContext.BaseDirectory, "NMEASender.Wpf.ini")
            : Path.Combine(Path.GetDirectoryName(basePath)!, "NMEASender.Wpf.ini");

        IIniFileService ini = basePath is null ? new IniFileService() : IniFileService.Load(basePath);
        if (File.Exists(savePath))
        {
            ini.MergeFrom(IniFileService.Load(savePath));
        }

        const string missing = "__MISSING__";
        string legacyRpmPort = ini.Get(PortsSection, "RPM", missing);
        string configuredProject = ini.Get(ConfigSection, ProjectKey, missing);
        if (configuredProject == missing)
        {
            configuredProject = ini.Get(ConfigSection, LegacyProjectKey, nameof(global::NMEASender.Wpf.Models.ProjectType.PS2603));
        }

        bool hasLegacyRpmPort = legacyRpmPort != missing;
        bool hasRpmPortKey = ini.Get(PortsSection, nameof(NmeaSentenceId.RpmPort).ToUpperInvariant(), missing) != missing;
        bool hasRpmStbdKey = ini.Get(PortsSection, nameof(NmeaSentenceId.RpmStbd).ToUpperInvariant(), missing) != missing;
        bool legacyRpmLayout = hasLegacyRpmPort && !hasRpmPortKey && !hasRpmStbdKey;
        uint rawSendFlag = ini.GetUInt(GpsSection, "SEND FLAG", (uint)DefaultSendFlag);
        NmeaSendFlag sendFlag = (NmeaSendFlag)rawSendFlag;
        uint rawUdpSendFlag = ini.GetUInt(GpsSection, "UDP SEND FLAG", rawSendFlag);
        NmeaSendFlag udpSendFlag = (NmeaSendFlag)rawUdpSendFlag;
        if (legacyRpmLayout && (sendFlag & NmeaSendFlag.RpmPort) == NmeaSendFlag.RpmPort)
        {
            sendFlag |= NmeaSendFlag.RpmStbd;
        }
        if (legacyRpmLayout && (udpSendFlag & NmeaSendFlag.RpmPort) == NmeaSendFlag.RpmPort)
        {
            udpSendFlag |= NmeaSendFlag.RpmStbd;
        }

        int portNo = ini.GetInt(GpsSection, "PORT NO", 1);
        NmeaSenderConfigService config = new NmeaSenderConfigService
        {
            SavePath = savePath,
            Title = ini.Get(ConfigSection, "TITLE", "ECDIS Sender"),
            DefaultPort = $"COM{Math.Max(1, portNo)}",
            BaudRate = ini.GetInt(GpsSection, "BAUD RATE", 19200),
            DataBits = ini.GetInt(GpsSection, "DATA BIT", 8),
            StopBits = MapStopBits(ini.GetInt(GpsSection, "STOP BIT", 0)),
            Parity = MapParity(ini.GetInt(GpsSection, "PARITY CHECK", 0)),
            SendInterval = Math.Max(50, ini.GetInt(GpsSection, "SendInterval", 500)),
            RightRpm = ini.GetBool(GpsSection, "RIGHT RPM", true),
            TrueWind = ini.GetBool(GpsSection, "TRUE WIND", true),
            UseHdmOutput = ini.GetBool(GpsSection, "Magnetic", true),
            UseUdp = ini.GetBool(ConfigSection, "USE UDP", ini.GetBool(SocketSection, "USE UDP", true)),
            UdpPort = NormalizeUdpPort(ini.GetInt(SocketSection, "SEND PORT", 40014)),
            ProjectType = ParseProjectType(configuredProject),
            SendFlag = sendFlag,
            UdpSendFlag = udpSendFlag
        };

        foreach (NmeaSentenceId id in Enum.GetValues(typeof(NmeaSentenceId)))
        {
            string? defaultPort = id switch
            {
                NmeaSentenceId.RpmPort or NmeaSentenceId.RpmStbd when legacyRpmLayout => legacyRpmPort,
                _ => config.DefaultPort
            };
            string key = id.ToString().ToUpperInvariant();
            List<string> ports = LoadSentencePorts(ini, PortsSection, key, defaultPort);
            List<int> udpPorts = LoadSentenceUdpPorts(ini, UdpPortsSection, key, config.UdpPort);
            while (udpPorts.Count < ports.Count)
            {
                udpPorts.Add(config.UdpPort);
            }

            config.SentencePorts[id] = ports[0];
            config.SentencePortRows[id] = ports;
            config.SentenceUdpPortRows[id] = udpPorts;
        }

        foreach ((string portName, string baudText) in ini.GetSectionValues(BaudSection))
        {
            if (TryParseBaudRate(baudText, out int baudRate))
            {
                config.PortBaudRates[NormalizePortName(portName)] = baudRate;
            }
        }

        return config;
    }

    public void Save(IEnumerable<SentenceItem> items)
    {
        IniFileService ini = new IniFileService();
        ini.Set(ConfigSection, "TITLE", Title);
        ini.Set(GpsSection, "PORT NO", PortNumber(DefaultPort).ToString());
        ini.Set(GpsSection, "BAUD RATE", BaudRate.ToString());
        ini.Set(GpsSection, "STOP BIT", ReverseMapStopBits(StopBits).ToString());
        ini.Set(GpsSection, "DATA BIT", DataBits.ToString());
        ini.Set(GpsSection, "PARITY CHECK", ReverseMapParity(Parity).ToString());
        ini.Set(GpsSection, "SEND FLAG", ((uint)BuildComSendFlag(items)).ToString());
        ini.Set(GpsSection, "UDP SEND FLAG", ((uint)BuildUdpSendFlag(items)).ToString());
        ini.Set(GpsSection, "RIGHT RPM", RightRpm ? "1" : "0");
        ini.Set(GpsSection, "TRUE WIND", TrueWind ? "1" : "0");
        ini.Set(GpsSection, "Magnetic", UseHdmOutput ? "1" : "0");
        ini.Set(GpsSection, "SendInterval", SendInterval.ToString());
        ini.Set(ConfigSection, "USE UDP", UseUdp ? "1" : "0");
        ini.Set(SocketSection, "USE UDP", UseUdp ? "1" : "0");
        ini.Set(SocketSection, "SEND PORT", NormalizeUdpPort(UdpPort).ToString());
        ini.Set(ConfigSection, ProjectKey, SerializeProjectType(ProjectType));

        foreach ((string portName, int baudRate) in PortBaudRates.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                continue;
            }

            ini.Set(BaudSection, NormalizePortName(portName), NormalizeBaudRate(baudRate).ToString());
        }

        foreach (IGrouping<NmeaSentenceId, SentenceItem> group in items.GroupBy(item => item.Id))
        {
            string key = group.Key.ToString().ToUpperInvariant();
            int index = 1;
            foreach (SentenceItem item in group)
            {
                if (item.Id == NmeaSentenceId.STR)
                {
                    continue; // STR sentences are only sent to ECDIS
                }

                string? rowKey = index == 1 ? key : $"{key}#{index}";
                ini.Set(PortsSection, rowKey, item.PortName);
                ini.Set(UdpPortsSection, rowKey, NormalizeSentenceUdpPort(item.UdpPort).ToString());
                index++;
            }
        }

        ini.Save(SavePath);
    }

    private static NmeaSendFlag BuildComSendFlag(IEnumerable<SentenceItem> items)
    {
        NmeaSendFlag result = 0;
        foreach (SentenceItem item in items.Where(item => item.IsComEnabled))
        {
            result |= item.Flag;
        }

        return result;
    }

    private static NmeaSendFlag BuildUdpSendFlag(IEnumerable<SentenceItem> items)
    {
        NmeaSendFlag result = 0;
        foreach (SentenceItem item in items.Where(item => item.IsUdpEnabled))
        {
            result |= item.Flag;
        }

        return result;
    }

    private static string? FindUpwards(string fileName)
    {
        DirectoryInfo start = new DirectoryInfo(AppContext.BaseDirectory);
        for (DirectoryInfo? directory = start; directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        string currentDirectoryCandidate = Path.Combine(Environment.CurrentDirectory, fileName);
        return File.Exists(currentDirectoryCandidate) ? currentDirectoryCandidate : null;
    }

    private static List<string> LoadSentencePorts(IIniFileService ini, string section, string key, string defaultPort)
    {
        const string missing = "__MISSING__";
        List<string> ports = new List<string>();
        string firstPort = ini.Get(section, key, missing);
        ports.Add(firstPort == missing ? defaultPort : firstPort);

        for (int index = 2; ; index++)
        {
            string port = ini.Get(section, $"{key}#{index}", missing);
            if (port == missing)
            {
                break;
            }

            ports.Add(port);
        }

        return ports;
    }

    private static List<int> LoadSentenceUdpPorts(IIniFileService ini, string section, string key, int defaultUdpPort)
    {
        const string missing = "__MISSING__";
        List<int> ports = new List<int>();
        string firstPort = ini.Get(section, key, missing);
        ports.Add(ParseSentenceUdpPort(firstPort, defaultUdpPort));

        for (int index = 2; ; index++)
        {
            string port = ini.Get(section, $"{key}#{index}", missing);
            if (port == missing)
            {
                break;
            }

            ports.Add(ParseSentenceUdpPort(port, defaultUdpPort));
        }

        return ports;
    }

    private static int PortNumber(string portName)
    {
        string digits = new string(portName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int value) ? value : 1;
    }

    private static int NormalizeUdpPort(int port)
    {
        return port is >= 1 and <= 65535 ? port : 40014;
    }

    private static int ParseSentenceUdpPort(string? value, int fallbackPort)
    {
        if (int.TryParse(value, out int port) && port is >= 1 and <= 65535)
        {
            return port;
        }

        return NormalizeUdpPort(fallbackPort);
    }

    private static int NormalizeSentenceUdpPort(int port)
    {
        return port is >= 1 and <= 65535 ? port : 40014;
    }

    private static ProjectType ParseProjectType(string value)
    {
        string normalized = new string((value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

        if (normalized.Length == 0)
        {
            return global::NMEASender.Wpf.Models.ProjectType.PS2603;
        }

        return normalized switch
        {
            "PS603" or "PS2603" => global::NMEASender.Wpf.Models.ProjectType.PS2603,
            "PS514" or "PS2514" => global::NMEASender.Wpf.Models.ProjectType.PS2514,
            "PS404A" or "PS2404A" => global::NMEASender.Wpf.Models.ProjectType.PS2404A,
            _ when Enum.TryParse(normalized, ignoreCase: true, out ProjectType parsed) => parsed,
            _ => global::NMEASender.Wpf.Models.ProjectType.PS2603
        };
    }

    private static string SerializeProjectType(ProjectType projectType)
    {
        return projectType switch
        {
            global::NMEASender.Wpf.Models.ProjectType.PS2603 => "PS603",
            global::NMEASender.Wpf.Models.ProjectType.PS2514 => "PS2514",
            global::NMEASender.Wpf.Models.ProjectType.PS2404A => "PS2404A",
            _ => projectType.ToString()
        };
    }

    private static bool TryParseBaudRate(string value, out int baudRate)
    {
        if (int.TryParse(value, out int parsed) && parsed > 0)
        {
            baudRate = parsed;
            return true;
        }

        baudRate = 0;
        return false;
    }

    private static int NormalizeBaudRate(int baudRate)
    {
        return baudRate > 0 ? baudRate : 19200;
    }

    private static string NormalizePortName(string portName)
    {
        return (portName ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static StopBits MapStopBits(int value)
    {
        return value switch
        {
            1 => StopBits.OnePointFive,
            2 => StopBits.Two,
            _ => StopBits.One
        };
    }

    private static int ReverseMapStopBits(StopBits value)
    {
        return value switch
        {
            StopBits.OnePointFive => 1,
            StopBits.Two => 2,
            _ => 0
        };
    }

    private static Parity MapParity(int value)
    {
        return value switch
        {
            1 => Parity.Odd,
            2 => Parity.Even,
            3 => Parity.Mark,
            4 => Parity.Space,
            _ => Parity.None
        };
    }

    private static int ReverseMapParity(Parity value)
    {
        return value switch
        {
            Parity.Odd => 1,
            Parity.Even => 2,
            Parity.Mark => 3,
            Parity.Space => 4,
            _ => 0
        };
    }
}
