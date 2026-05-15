using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services;

public sealed class SentenceCatalogService : ISentenceCatalogService
{
    private static readonly SentenceTemplate[] GpsTemplates =
    [
        new(NmeaSentenceId.Gga, NmeaSendFlag.Gga, "$GPGGA"),
        new(NmeaSentenceId.Gll, NmeaSendFlag.Gll, "$GPGLL"),
        new(NmeaSentenceId.Rmc, NmeaSendFlag.Rmc, "$GPRMC"),
        new(NmeaSentenceId.Vtg, NmeaSendFlag.Vtg, "$GPVTG"),
        new(NmeaSentenceId.Zda, NmeaSendFlag.Zda, "$GPZDA")
    ];

    private static readonly SentenceTemplate[] OtherTemplates =
    [
        new(NmeaSentenceId.Hdt, NmeaSendFlag.Hdt, "$HEHDT"),
        new(NmeaSentenceId.Vbw, NmeaSendFlag.Vbw, "$--VBW"),
        new(NmeaSentenceId.Rot, NmeaSendFlag.Rot, "$--ROT"),
        new(NmeaSentenceId.Rsa, NmeaSendFlag.Rsa, "$--RSA"),
        new(NmeaSentenceId.RpmPort, NmeaSendFlag.RpmPort, "$--RPM(PORT)"),
        new(NmeaSentenceId.RpmStbd, NmeaSendFlag.RpmStbd, "$--RPM(STBD)"),
        new(NmeaSentenceId.Mwv, NmeaSendFlag.Mwv, "$--MWV"),
        new(NmeaSentenceId.Hdg, NmeaSendFlag.Hdg, "$--HDG"),
        new(NmeaSentenceId.Dpt, NmeaSendFlag.Dpt, "$--DPT"),
        new(NmeaSentenceId.Dbt, NmeaSendFlag.Dbt, "$--DBT"),
        new(NmeaSentenceId.Etl, NmeaSendFlag.Etl, "$--ETL", HasSecondary: true),
        new(NmeaSentenceId.Cur, NmeaSendFlag.Cur, "$--CUR"),
        new(NmeaSentenceId.Mda, NmeaSendFlag.Mda, "$--MDA"),
        new(NmeaSentenceId.Trc, NmeaSendFlag.Trc, "$--TRC", HasSecondary: true),
        new(NmeaSentenceId.Trd, NmeaSendFlag.Trd, "$--TRD", HasSecondary: true),
        new(NmeaSentenceId.Hpm, NmeaSendFlag.Hpm, "$--HPM"),
        new(NmeaSentenceId.Hrm, NmeaSendFlag.Hrm, "$--HRM"),
        new(NmeaSentenceId.Vdo, NmeaSendFlag.Vdo, "$AIVDO"),
        new(NmeaSentenceId.Vdm, NmeaSendFlag.Vdm, "$AIVDM")
    ];

    private static readonly SentenceTemplate[] InternalTemplates =
    [
        new(NmeaSentenceId.STR, NmeaSendFlag.STR, "$--STR", EnabledOverride: true)
    ];

    public void Populate(
        ICollection<SentenceItem> gpsSentences,
        ICollection<SentenceItem> otherSentences,
        ICollection<SentenceItem> internalSentences,
        INmeaSenderConfigService config,
        Func<string, string> pickAvailablePort)
    {
        gpsSentences.Clear();
        otherSentences.Clear();
        internalSentences.Clear();

        AddTemplates(gpsSentences, GpsTemplates, config, pickAvailablePort);
        AddTemplates(otherSentences, OtherTemplates, config, pickAvailablePort);
        AddTemplates(internalSentences, InternalTemplates, config, pickAvailablePort);
    }

    private static void AddTemplates(
        ICollection<SentenceItem> target,
        IEnumerable<SentenceTemplate> templates,
        INmeaSenderConfigService config,
        Func<string, string> pickAvailablePort)
    {
        foreach (SentenceTemplate template in templates)
        {
            bool isComEnabled = template.EnabledOverride ?? ((config.SendFlag & template.Flag) == template.Flag);
            bool isUdpEnabled = template.EnabledOverride ?? ((config.UdpSendFlag & template.Flag) == template.Flag);
            string defaultPort = config.SentencePorts.TryGetValue(template.Id, out string? configuredPort) ? configuredPort : config.DefaultPort;
            List<string>? configuredPorts = config.SentencePortRows.TryGetValue(template.Id, out List<string>? ports) && ports.Count > 0
                ? ports
                : new List<string> { defaultPort };
            List<int>? configuredUdpPorts = config.SentenceUdpPortRows.TryGetValue(template.Id, out List<int>? udpPorts) && udpPorts.Count > 0
                ? udpPorts
                : new List<int> { config.UdpPort };
            int rowCount = Math.Max(configuredPorts.Count, configuredUdpPorts.Count);

            for (int index = 0; index < rowCount; index++)
            {
                string port = ResolveConfiguredPort(configuredPorts, index, defaultPort);
                int udpPort = ResolveUdpPort(configuredUdpPorts, index, config.UdpPort);
                bool isDuplicateRow = index > 0;
                target.Add(new SentenceItem(
                    template.Id,
                    template.Flag,
                    template.Label,
                    pickAvailablePort(port),
                    isComEnabled,
                    isUdpEnabled,
                    udpPort,
                    template.HasSecondary,
                    isDuplicateRow));
            }
        }
    }

    private static string ResolveConfiguredPort(IReadOnlyList<string> configuredPorts, int index, string defaultPort)
    {
        if (index < configuredPorts.Count && !string.IsNullOrWhiteSpace(configuredPorts[index]))
        {
            return configuredPorts[index];
        }

        if (configuredPorts.Count > 0 && !string.IsNullOrWhiteSpace(configuredPorts[0]))
        {
            return configuredPorts[0];
        }

        return defaultPort;
    }

    private static int ResolveUdpPort(IReadOnlyList<int> configuredUdpPorts, int index, int defaultUdpPort)
    {
        if (index < configuredUdpPorts.Count && configuredUdpPorts[index] is >= 1 and <= 65535)
        {
            return configuredUdpPorts[index];
        }

        if (configuredUdpPorts.Count > 0 && configuredUdpPorts[0] is >= 1 and <= 65535)
        {
            return configuredUdpPorts[0];
        }

        return defaultUdpPort is >= 1 and <= 65535 ? defaultUdpPort : 40014;
    }

    private sealed record SentenceTemplate(
        NmeaSentenceId Id,
        NmeaSendFlag Flag,
        string Label,
        bool HasSecondary = false,
        bool? EnabledOverride = null);
}
