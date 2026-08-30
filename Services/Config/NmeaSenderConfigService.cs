using NMEASender.Wpf.Exceptions;
using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.Config;
using NMEASender.Wpf.Services.Interfaces.Network;
using NMEASender.Wpf.Services.Interfaces.Projects;
using System.Globalization;
using System.IO;
using System.IO.Ports;

namespace NMEASender.Wpf.Services.Config;

public sealed class NmeaSenderConfigService : INmeaSenderConfigService
{
    private readonly IUdpTransportProfileService _udpTransportProfileService;
    private readonly IReadOnlyDictionary<ProjectType, IProjectSendFlagCodec> _sendFlagCodecs;
    private readonly IProjectSendFlagCodec _fallbackSendFlagCodec;
    private const string GpsSection = "GPS CONFIG";
    private const string ConfigSection = "CONFIG";
    private const string ProjectKey = "Project";
    private const string LegacyProjectKey = "PROJECT TYPE";
    private const string SocketSection = "SOCKET";
    private const string UdpConfigFileName = "UDPConfig.ini";
    private const string UdpConfigSection = "UDP CONFIG";
    private const string PortsSection = "SENTENCE PORTS";
    private const string UdpPortsSection = "UDP PORTS";
    private const string UdpAddressesSection = "UDP ADDRESSES";
    private const string HzSection = "SENTENCE HZ";
    private const string BaudSection = "BAUD RATE";
    private const string SourceNotesSection = "SOURCE NOTES";

    public string Title { get; set; } = "ECDIS Sender";
    public string DefaultPort { get; set; } = "COM1";
    public int BaudRate { get; set; } = 19200;
    public int DataBits { get; set; } = 8;
    public Parity Parity { get; set; } = Parity.None;
    public StopBits StopBits { get; set; } = StopBits.One;
    public int SendInterval { get; set; } = 20;
    public bool RightRpm { get; set; } = true;
    public bool TrueWind { get; set; } = true;
    public bool UseHdmOutput { get; set; } = true;
    public bool UseUdp { get; set; } = true;
    public int UdpPort { get; set; } = 40014;
    public UdpTransportOptions UdpTransportOptions { get; set; } = UdpTransportOptions.CreateDefault(40014);
    public ProjectType ProjectType { get; set; } = ProjectType.PS000;
    public NmeaSendFlag SendFlag { get; set; } = DefaultSendFlag;
    public NmeaSendFlag UdpSendFlag { get; set; } = DefaultSendFlag;
    public Dictionary<NmeaSentenceId, string> SentencePorts { get; } = new();
    public Dictionary<NmeaSentenceId, List<string>> SentencePortRows { get; } = new();
    public Dictionary<NmeaSentenceId, List<int>> SentenceUdpPortRows { get; } = new();
    public Dictionary<NmeaSentenceId, List<string>> SentenceUdpAddressRows { get; } = new();
    public Dictionary<NmeaSentenceId, List<double>> SentenceHzRows { get; } = new();
    public Dictionary<string, int> PortBaudRates { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> SourceNotes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string SavePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "NMEASender.Wpf.ini");

    private NmeaSenderConfigService(
        IUdpTransportProfileService udpTransportProfileService,
        IEnumerable<IProjectSendFlagCodec> sendFlagCodecs)
    {
        _udpTransportProfileService = udpTransportProfileService ?? throw new ArgumentNullException(nameof(udpTransportProfileService));
        if (sendFlagCodecs is null)
        {
            throw new ArgumentNullException(nameof(sendFlagCodecs));
        }

        List<IProjectSendFlagCodec> codecs = sendFlagCodecs.ToList();
        if (codecs.Count == 0)
        {
            throw new ConfigServiceRegistrationException("At least one send flag codec must be registered.");
        }

        _sendFlagCodecs = codecs
            .GroupBy(codec => codec.ProjectType)
            .ToDictionary(group => group.Key, group => group.First());
        _fallbackSendFlagCodec = _sendFlagCodecs.TryGetValue(ProjectType.PS000, out IProjectSendFlagCodec? ps000Codec)
            ? ps000Codec
            : codecs[0];
    }

    public static NmeaSendFlag DefaultSendFlag =>
        NmeaSendFlag.Rmc | NmeaSendFlag.Gga | NmeaSendFlag.Gll | NmeaSendFlag.Vtg | NmeaSendFlag.Zda |
        NmeaSendFlag.Hdt | NmeaSendFlag.Vbw | NmeaSendFlag.Rot | NmeaSendFlag.Rsa | NmeaSendFlag.RpmPort | NmeaSendFlag.RpmStbd |
        NmeaSendFlag.Mwv | NmeaSendFlag.Hdg | NmeaSendFlag.Vdm | NmeaSendFlag.Dpt | NmeaSendFlag.Dbt |
        NmeaSendFlag.Etl | NmeaSendFlag.Cur | NmeaSendFlag.Mda | NmeaSendFlag.Trc | NmeaSendFlag.Trd |
        NmeaSendFlag.Hpm | NmeaSendFlag.Hrm | NmeaSendFlag.Vdo;

    public static NmeaSenderConfigService Load(
        IUdpTransportProfileService udpTransportProfileService,
        IEnumerable<IProjectSendFlagCodec> sendFlagCodecs)
    {
        string? basePath = FindUpwards("NMEASender.ini");
        string? savePath = basePath is null
            ? Path.Combine(AppContext.BaseDirectory, "NMEASender.Wpf.ini")
            : Path.Combine(Path.GetDirectoryName(basePath)!, "NMEASender.Wpf.ini");
        string configDirectory = ResolveConfigDirectory(basePath, savePath);

        IIniFileService ini = basePath is null ? new IniFileService() : IniFileService.Load(basePath);
        if (File.Exists(savePath))
        {
            ini.MergeFrom(IniFileService.Load(savePath));
        }

        string udpConfigPath = Path.Combine(configDirectory, UdpConfigFileName);
        bool hasUdpConfig = File.Exists(udpConfigPath);
        IIniFileService udpIni = hasUdpConfig
            ? IniFileService.Load(udpConfigPath)
            : new IniFileService();

        const string missing = "__MISSING__";
        string legacyRpmPort = ini.Get(PortsSection, "RPM", missing);
        string configuredProject = ini.Get(ConfigSection, ProjectKey, missing);
        if (configuredProject == missing)
        {
            configuredProject = ini.Get(ConfigSection, LegacyProjectKey, nameof(ProjectType.PS000));
        }

        bool hasLegacyRpmPort = legacyRpmPort != missing;
        bool hasRpmPortKey = ini.Get(PortsSection, nameof(NmeaSentenceId.RpmPort).ToUpperInvariant(), missing) != missing;
        bool hasRpmStbdKey = ini.Get(PortsSection, nameof(NmeaSentenceId.RpmStbd).ToUpperInvariant(), missing) != missing;
        bool legacyRpmLayout = hasLegacyRpmPort && !hasRpmPortKey && !hasRpmStbdKey;
        ProjectType projectType = ParseProjectType(configuredProject);
        NmeaSenderConfigService config = new NmeaSenderConfigService(udpTransportProfileService, sendFlagCodecs);
        IProjectSendFlagCodec sendFlagCodec = config.ResolveSendFlagCodec(projectType);

        ulong defaultRawSendFlag = sendFlagCodec.DefaultRawSendFlag((ulong)DefaultSendFlag);
        ulong rawSendFlag = GetULong(ini, GpsSection, "SEND FLAG", defaultRawSendFlag);
        ulong legacyRawUdpSendFlag = GetULong(ini, GpsSection, "UDP SEND FLAG", rawSendFlag);
        ulong rawUdpSendFlag = GetULong(udpIni, UdpConfigSection, "UDP SEND FLAG", legacyRawUdpSendFlag);
        NmeaSendFlag sendFlag = sendFlagCodec.Decode(rawSendFlag);
        NmeaSendFlag udpSendFlag = sendFlagCodec.Decode(rawUdpSendFlag);
        if (legacyRpmLayout && (sendFlag & NmeaSendFlag.RpmPort) == NmeaSendFlag.RpmPort)
        {
            sendFlag |= NmeaSendFlag.RpmStbd;
        }
        if (legacyRpmLayout && (udpSendFlag & NmeaSendFlag.RpmPort) == NmeaSendFlag.RpmPort)
        {
            udpSendFlag |= NmeaSendFlag.RpmStbd;
        }

        int portNo = ini.GetInt(GpsSection, "PORT NO", 1);
        config.SavePath = savePath;
        config.Title = ini.Get(ConfigSection, "TITLE", "ECDIS Sender");
        config.DefaultPort = $"COM{Math.Max(1, portNo)}";
        config.BaudRate = ini.GetInt(GpsSection, "BAUD RATE", 19200);
        config.DataBits = ini.GetInt(GpsSection, "DATA BIT", 8);
        config.StopBits = MapStopBits(ini.GetInt(GpsSection, "STOP BIT", 0));
        config.Parity = MapParity(ini.GetInt(GpsSection, "PARITY CHECK", 0));
        config.SendInterval = Math.Max(20, ini.GetInt(GpsSection, "SendInterval", 20));
        config.RightRpm = ini.GetBool(GpsSection, "RIGHT RPM", true);
        config.TrueWind = ini.GetBool(GpsSection, "TRUE WIND", true);
        config.UseHdmOutput = ini.GetBool(GpsSection, "Magnetic", true);
        bool legacyUseUdp = ini.GetBool(ConfigSection, "USE UDP", ini.GetBool(SocketSection, "USE UDP", true));
        int legacyUdpPort = NormalizeUdpPort(ini.GetInt(SocketSection, "SEND PORT", 40014));
        config.UseUdp = udpIni.GetBool(UdpConfigSection, "USE UDP", legacyUseUdp);
        config.UdpPort = NormalizeUdpPort(udpIni.GetInt(UdpConfigSection, "SEND PORT", legacyUdpPort));
        config.ProjectType = projectType;
        config.SendFlag = sendFlag;
        config.UdpSendFlag = udpSendFlag;

        config.UdpTransportOptions = udpTransportProfileService
            .Load(projectType, configDirectory, config.UdpPort)
            .WithFallbackPort(config.UdpPort);

        double defaultHz = SentenceItem.DefaultHz;
        foreach (NmeaSentenceId id in Enum.GetValues(typeof(NmeaSentenceId)))
        {
            string? defaultPort = id switch
            {
                NmeaSentenceId.RpmPort or NmeaSentenceId.RpmStbd when legacyRpmLayout => legacyRpmPort,
                _ => config.DefaultPort
            };
            string key = id.ToString().ToUpperInvariant();
            List<string> ports = LoadSentencePorts(ini, PortsSection, key, defaultPort);
            IIniFileService sentenceUdpIni = hasUdpConfig ? udpIni : ini;
            List<int> udpPorts = LoadSentenceUdpPorts(sentenceUdpIni, UdpPortsSection, key, config.UdpPort);
            string defaultUdpAddress = config.UdpTransportOptions.MulticastAddress;
            List<string> udpAddresses = LoadSentenceUdpAddresses(sentenceUdpIni, UdpAddressesSection, key, defaultUdpAddress);
            List<double> hzValues = LoadSentenceHz(ini, HzSection, key, defaultHz);
            int rowCount = Math.Max(ports.Count, Math.Max(udpPorts.Count, Math.Max(udpAddresses.Count, hzValues.Count)));
            while (ports.Count < rowCount)
            {
                ports.Add(defaultPort);
            }

            while (udpPorts.Count < rowCount)
            {
                udpPorts.Add(config.UdpPort);
            }

            while (udpAddresses.Count < rowCount)
            {
                udpAddresses.Add(defaultUdpAddress);
            }

            while (hzValues.Count < rowCount)
            {
                hzValues.Add(defaultHz);
            }

            config.SentencePorts[id] = ports[0];
            config.SentencePortRows[id] = ports;
            config.SentenceUdpPortRows[id] = udpPorts;
            config.SentenceUdpAddressRows[id] = udpAddresses;
            config.SentenceHzRows[id] = hzValues;
        }

        foreach ((string portName, string baudText) in ini.GetSectionValues(BaudSection))
        {
            if (TryParseBaudRate(baudText, out int baudRate))
            {
                config.PortBaudRates[NormalizePortName(portName)] = baudRate;
            }
        }

        foreach ((string sourceKey, string noteText) in ini.GetSectionValues(SourceNotesSection))
        {
            string normalizedKey = (sourceKey ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                continue;
            }

            config.SourceNotes[normalizedKey] = noteText ?? string.Empty;
        }

        return config;
    }

    public void Save(IEnumerable<SentenceItem> items)
    {
        List<SentenceItem> itemList = items.ToList();
        IniFileService ini = new IniFileService();
        ini.Set(ConfigSection, "TITLE", Title);
        ini.Set(GpsSection, "PORT NO", PortNumber(DefaultPort).ToString());
        ini.Set(GpsSection, "BAUD RATE", BaudRate.ToString());
        ini.Set(GpsSection, "STOP BIT", ReverseMapStopBits(StopBits).ToString());
        ini.Set(GpsSection, "DATA BIT", DataBits.ToString());
        ini.Set(GpsSection, "PARITY CHECK", ReverseMapParity(Parity).ToString());
        NmeaSendFlag comSendFlag = BuildComSendFlag(itemList);
        NmeaSendFlag udpSendFlag = BuildUdpSendFlag(itemList);
        IProjectSendFlagCodec sendFlagCodec = ResolveSendFlagCodec(ProjectType);
        ulong encodedComFlag = sendFlagCodec.Encode(comSendFlag);
        ulong encodedUdpFlag = sendFlagCodec.Encode(udpSendFlag);
        ini.Set(GpsSection, "SEND FLAG", encodedComFlag.ToString());
        ini.Set(GpsSection, "RIGHT RPM", RightRpm ? "1" : "0");
        ini.Set(GpsSection, "TRUE WIND", TrueWind ? "1" : "0");
        ini.Set(GpsSection, "Magnetic", UseHdmOutput ? "1" : "0");
        ini.Set(GpsSection, "SendInterval", SendInterval.ToString());
        ini.Set(ConfigSection, ProjectKey, SerializeProjectType(ProjectType));

        foreach ((string portName, int baudRate) in PortBaudRates.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                continue;
            }

            ini.Set(BaudSection, NormalizePortName(portName), NormalizeBaudRate(baudRate).ToString());
        }

        foreach ((string sourceKey, string noteText) in SourceNotes.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            string normalizedKey = (sourceKey ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                continue;
            }

            string normalizedNote = noteText ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedNote))
            {
                continue;
            }

            ini.Set(SourceNotesSection, normalizedKey, normalizedNote);
        }

        foreach (IGrouping<NmeaSentenceId, SentenceItem> group in itemList.GroupBy(item => item.Id))
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
                ini.Set(HzSection, rowKey, item.Hz.ToString(CultureInfo.InvariantCulture));
                index++;
            }
        }

        ini.Save(SavePath);

        UdpTransportOptions normalizedUdpTransport = this.UdpTransportOptions.WithFallbackPort(UdpPort);
        UdpTransportOptions = normalizedUdpTransport;
        string configDirectory = Path.GetDirectoryName(SavePath) ?? AppContext.BaseDirectory;
        _udpTransportProfileService.Save(
            ProjectType,
            configDirectory,
            normalizedUdpTransport);
        SaveUdpConfig(configDirectory, itemList, encodedUdpFlag);
    }

    private void SaveUdpConfig(string configDirectory, IEnumerable<SentenceItem> items, ulong encodedUdpFlag)
    {
        string udpConfigPath = Path.Combine(configDirectory, UdpConfigFileName);
        IniFileService udpIni = File.Exists(udpConfigPath)
            ? IniFileService.Load(udpConfigPath)
            : new IniFileService();

        udpIni.RemoveSection(UdpConfigSection);
        udpIni.RemoveSection(UdpPortsSection);
        udpIni.RemoveSection(UdpAddressesSection);

        udpIni.Set(UdpConfigSection, "USE UDP", UseUdp ? "1" : "0");
        udpIni.Set(UdpConfigSection, "SEND PORT", NormalizeUdpPort(UdpPort).ToString());
        udpIni.Set(UdpConfigSection, "UDP SEND FLAG", encodedUdpFlag.ToString());

        foreach (IGrouping<NmeaSentenceId, SentenceItem> group in items.GroupBy(item => item.Id))
        {
            string key = group.Key.ToString().ToUpperInvariant();
            int index = 1;
            foreach (SentenceItem item in group)
            {
                if (item.Id == NmeaSentenceId.STR)
                {
                    continue;
                }

                string rowKey = index == 1 ? key : $"{key}#{index}";
                udpIni.Set(UdpPortsSection, rowKey, NormalizeSentenceUdpPort(item.UdpPort).ToString());
                udpIni.Set(UdpAddressesSection, rowKey, NormalizeSentenceUdpAddress(item.UdpAddress, UdpTransportOptions.MulticastAddress));
                index++;
            }
        }

        udpIni.Save(udpConfigPath);
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

    private static string ResolveConfigDirectory(string? basePath, string savePath)
    {
        if (!string.IsNullOrWhiteSpace(basePath))
        {
            string? fromBase = Path.GetDirectoryName(basePath);
            if (!string.IsNullOrWhiteSpace(fromBase))
            {
                return fromBase;
            }
        }

        string? fromSave = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrWhiteSpace(fromSave))
        {
            return fromSave;
        }

        return AppContext.BaseDirectory;
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

    private static List<double> LoadSentenceHz(IIniFileService ini, string section, string key, double defaultHz)
    {
        const string missing = "__MISSING__";
        List<double> values = new List<double>();
        string firstValue = ini.Get(section, key, missing);
        values.Add(ParseSentenceHz(firstValue, defaultHz));

        for (int index = 2; ; index++)
        {
            string value = ini.Get(section, $"{key}#{index}", missing);
            if (value == missing)
            {
                break;
            }

            values.Add(ParseSentenceHz(value, defaultHz));
        }

        return values;
    }

    private static List<string> LoadSentenceUdpAddresses(IIniFileService ini, string section, string key, string defaultUdpAddress)
    {
        const string missing = "__MISSING__";
        List<string> addresses = new List<string>();
        string firstAddress = ini.Get(section, key, missing);
        addresses.Add(ParseSentenceUdpAddress(firstAddress, defaultUdpAddress));

        for (int index = 2; ; index++)
        {
            string address = ini.Get(section, $"{key}#{index}", missing);
            if (address == missing)
            {
                break;
            }

            addresses.Add(ParseSentenceUdpAddress(address, defaultUdpAddress));
        }

        return addresses;
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

    private static double ParseSentenceHz(string? value, double fallbackHz)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double hz) && hz >= SentenceItem.MinHz
            ? hz
            : fallbackHz;
    }

    private static string ParseSentenceUdpAddress(string? value, string fallbackAddress)
    {
        string normalizedFallback = NormalizeSentenceUdpAddress(fallbackAddress, "225.0.0.0");
        return NormalizeSentenceUdpAddress(value, normalizedFallback);
    }

    private static string NormalizeSentenceUdpAddress(string? value, string fallbackAddress)
    {
        string candidate = (value ?? string.Empty).Trim();
        if (candidate.Length == 0)
        {
            return fallbackAddress;
        }

        return UdpTransportOptions.NormalizeAddress(candidate, fallbackAddress);
    }

    private static ulong GetULong(IIniFileService ini, string section, string key, ulong defaultValue)
    {
        string raw = ini.Get(section, key, defaultValue.ToString());
        return ulong.TryParse(raw, out ulong parsed) ? parsed : defaultValue;
    }

    private IProjectSendFlagCodec ResolveSendFlagCodec(ProjectType projectType)
    {
        return _sendFlagCodecs.TryGetValue(projectType, out IProjectSendFlagCodec? codec)
            ? codec
            : _fallbackSendFlagCodec;
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
            return ProjectType.PS000;
        }

        return normalized switch
        {
            "PS000" => ProjectType.PS000,
            "PS603" or "PS2603" => ProjectType.PS2603,
            "PS404A" or "PS2404A" => ProjectType.PS2404A,
            _ when Enum.TryParse(normalized, ignoreCase: true, out ProjectType parsed) => parsed,
            _ => ProjectType.PS000
        };
    }

    private static string SerializeProjectType(ProjectType projectType)
    {
        return projectType switch
        {
            ProjectType.PS000 => "PS000",
            ProjectType.PS2603 => "PS603",
            ProjectType.PS2404A => "PS2404A",
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
