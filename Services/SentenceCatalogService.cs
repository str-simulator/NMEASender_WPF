using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services;

public sealed class SentenceCatalogService
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
        NmeaSenderConfig config,
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
        NmeaSenderConfig config,
        Func<string, string> pickAvailablePort)
    {
        foreach (SentenceTemplate template in templates)
        {
            bool enabled = template.EnabledOverride ?? ((config.SendFlag & template.Flag) == template.Flag);
            string defaultPort = config.SentencePorts.TryGetValue(template.Id, out string? configuredPort) ? configuredPort : config.DefaultPort;
            List<string>? configuredPorts = config.SentencePortRows.TryGetValue(template.Id, out List<string>? ports) && ports.Count > 0
                ? ports
                : new List<string> { defaultPort };

            for (int index = 0; index < configuredPorts.Count; index++)
            {
                string? port = configuredPorts[index];
                bool isDuplicateRow = index > 0;
                target.Add(new SentenceItem(
                    template.Id,
                    template.Flag,
                    template.Label,
                    pickAvailablePort(port),
                    enabled,
                    template.HasSecondary,
                    isDuplicateRow));
            }
        }
    }

    private sealed record SentenceTemplate(
        NmeaSentenceId Id,
        NmeaSendFlag Flag,
        string Label,
        bool HasSecondary = false,
        bool? EnabledOverride = null);
}
