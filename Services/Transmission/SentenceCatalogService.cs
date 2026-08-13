using NMEASender.Wpf.Exceptions;
using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.Config;
using NMEASender.Wpf.Services.Interfaces.Projects;
using NMEASender.Wpf.Services.Interfaces.Transmission;

namespace NMEASender.Wpf.Services.Transmission;

public sealed class SentenceCatalogService : ISentenceCatalogService
{
    private readonly IReadOnlyDictionary<ProjectType, IProjectSentenceCatalogPolicy> _projectPolicies;
    private readonly IProjectSentenceCatalogPolicy _fallbackPolicy;

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
        new(NmeaSentenceId.Vdvbw, NmeaSendFlag.Vdvbw, "$VDVBW", RequiredProjectType: ProjectType.PS2404A),
        new(NmeaSentenceId.Rot, NmeaSendFlag.Rot, "$--ROT"),
        new(NmeaSentenceId.Rsa, NmeaSendFlag.Rsa, "$--RSA"),
        new(NmeaSentenceId.RpmPort, NmeaSendFlag.RpmPort, "$--RPM(PORT)"),
        new(NmeaSentenceId.RpmStbd, NmeaSendFlag.RpmStbd, "$--RPM(STBD)"),
        new(NmeaSentenceId.Mwv, NmeaSendFlag.Mwv, "$--MWV"),
        new(NmeaSentenceId.Ths, NmeaSendFlag.Ths, "$HETHS", RequiredProjectType: ProjectType.PS2404A),
        new(NmeaSentenceId.Mws, NmeaSendFlag.Mws, "$WIMWS", RequiredProjectType: ProjectType.PS2404A),
        new(NmeaSentenceId.Mwh, NmeaSendFlag.Mwh, "$WIMWH", RequiredProjectType: ProjectType.PS2404A),
        new(NmeaSentenceId.Hdg, NmeaSendFlag.Hdg, "$--HDG"),
        new(NmeaSentenceId.Vhw, NmeaSendFlag.Vhw, "$VDVHW", RequiredProjectType: ProjectType.PS2404A),
        new(NmeaSentenceId.Vdr, NmeaSendFlag.Vdr, "$VDVDR", RequiredProjectType: ProjectType.PS2404A),
        new(NmeaSentenceId.Dpt, NmeaSendFlag.Dpt, "$--DPT"),
        new(NmeaSentenceId.Dbt, NmeaSendFlag.Dbt, "$--DBT"),
        new(NmeaSentenceId.Dtm, NmeaSendFlag.Dtm, "$VDDTM", RequiredProjectType: ProjectType.PS2404A),
        new(NmeaSentenceId.Gpdtm, NmeaSendFlag.Gpdtm, "$GPDTM", RequiredProjectType: ProjectType.PS2404A),
        new(NmeaSentenceId.Htd, NmeaSendFlag.Htd, "$--HTD", RequiredProjectType: ProjectType.PS2404A),
        new(NmeaSentenceId.Ttm, NmeaSendFlag.Ttm, "$RATTM", RequiredProjectType: ProjectType.PS2404A),
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

    public SentenceCatalogService(IEnumerable<IProjectSentenceCatalogPolicy> projectPolicies)
    {
        if (projectPolicies is null)
        {
            throw new ArgumentNullException(nameof(projectPolicies));
        }

        List<IProjectSentenceCatalogPolicy> policies = projectPolicies.ToList();
        if (policies.Count == 0)
        {
            throw new TransmissionServiceRegistrationException("At least one sentence catalog policy must be registered.");
        }

        _projectPolicies = policies
            .GroupBy(policy => policy.ProjectType)
            .ToDictionary(group => group.Key, group => group.First());
        _fallbackPolicy = _projectPolicies.TryGetValue(ProjectType.PS000, out IProjectSentenceCatalogPolicy? ps000Policy)
            ? ps000Policy
            : policies[0];
    }

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

        IProjectSentenceCatalogPolicy policy = ResolvePolicy(config.ProjectType);

        AddTemplates(gpsSentences, GpsTemplates, config, pickAvailablePort, policy);
        AddTemplates(otherSentences, OtherTemplates, config, pickAvailablePort, policy);
        AddTemplates(internalSentences, InternalTemplates, config, pickAvailablePort, policy);
    }

    private static void AddTemplates(
        ICollection<SentenceItem> target,
        IEnumerable<SentenceTemplate> templates,
        INmeaSenderConfigService config,
        Func<string, string> pickAvailablePort,
        IProjectSentenceCatalogPolicy policy)
    {
        foreach (SentenceTemplate template in templates)
        {
            if (!policy.IsTemplateVisible(template.RequiredProjectType))
            {
                continue;
            }

            bool isComEnabled = template.EnabledOverride ?? ((config.SendFlag & template.Flag) == template.Flag);
            bool isUdpEnabled = template.EnabledOverride ?? ((config.UdpSendFlag & template.Flag) == template.Flag);
            string defaultPort = config.SentencePorts.TryGetValue(template.Id, out string? configuredPort) ? configuredPort : config.DefaultPort;
            List<string>? configuredPorts = config.SentencePortRows.TryGetValue(template.Id, out List<string>? ports) && ports.Count > 0
                ? ports
                : new List<string> { defaultPort };
            List<int>? configuredUdpPorts = config.SentenceUdpPortRows.TryGetValue(template.Id, out List<int>? udpPorts) && udpPorts.Count > 0
                ? udpPorts
                : new List<int> { config.UdpPort };
            List<string>? configuredUdpAddresses = config.SentenceUdpAddressRows.TryGetValue(template.Id, out List<string>? udpAddresses) && udpAddresses.Count > 0
                ? udpAddresses
                : new List<string> { config.UdpTransportOptions.MulticastAddress };
            List<double>? configuredHz = config.SentenceHzRows.TryGetValue(template.Id, out List<double>? hzValues) && hzValues.Count > 0
                ? hzValues
                : new List<double> { SentenceItem.DefaultHz };
            int rowCount = Math.Max(configuredPorts.Count, Math.Max(configuredUdpPorts.Count, Math.Max(configuredUdpAddresses.Count, configuredHz.Count)));

            for (int index = 0; index < rowCount; index++)
            {
                string port = ResolveConfiguredPort(configuredPorts, index, defaultPort);
                int udpPort = ResolveUdpPort(configuredUdpPorts, index, config.UdpPort);
                string udpAddress = ResolveUdpAddress(configuredUdpAddresses, index, config.UdpTransportOptions.MulticastAddress);
                double hz = ResolveHz(configuredHz, index);
                bool isDuplicateRow = index > 0;
                target.Add(new SentenceItem(
                    template.Id,
                    template.Flag,
                    template.Label,
                    pickAvailablePort(port),
                    isComEnabled,
                    isUdpEnabled,
                    udpPort,
                    udpAddress,
                    hz,
                    template.HasSecondary,
                    isDuplicateRow));
            }
        }
    }

    private IProjectSentenceCatalogPolicy ResolvePolicy(ProjectType projectType)
    {
        return _projectPolicies.TryGetValue(projectType, out IProjectSentenceCatalogPolicy? policy)
            ? policy
            : _fallbackPolicy;
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

    private static double ResolveHz(IReadOnlyList<double> configuredHz, int index)
    {
        if (index < configuredHz.Count && configuredHz[index] >= SentenceItem.MinHz)
        {
            return configuredHz[index];
        }

        return configuredHz.Count > 0 && configuredHz[0] >= SentenceItem.MinHz
            ? configuredHz[0]
            : SentenceItem.DefaultHz;
    }

    private static string ResolveUdpAddress(IReadOnlyList<string> configuredUdpAddresses, int index, string defaultUdpAddress)
    {
        if (index < configuredUdpAddresses.Count && !string.IsNullOrWhiteSpace(configuredUdpAddresses[index]))
        {
            return configuredUdpAddresses[index];
        }

        if (configuredUdpAddresses.Count > 0 && !string.IsNullOrWhiteSpace(configuredUdpAddresses[0]))
        {
            return configuredUdpAddresses[0];
        }

        return defaultUdpAddress;
    }

    private sealed record SentenceTemplate(
        NmeaSentenceId Id,
        NmeaSendFlag Flag,
        string Label,
        bool HasSecondary = false,
        bool? EnabledOverride = null,
        ProjectType? RequiredProjectType = null);
}
