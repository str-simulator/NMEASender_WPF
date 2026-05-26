using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Projects.PS2404A;

public sealed class PS2404AUdpTransportProfileStore : BaseProjectUdpTransportProfileStore
{
    public override ProjectType ProjectType => ProjectType.PS2404A;

    protected override int DefaultBroadcastPort => 49552;

    protected override int DefaultMulticastPortNo => 6000;

    protected override int DefaultMulticastSendPort => 6000;

    protected override string DefaultMulticastAddress => "225.0.0.0";

    protected override string? LegacyFileName => "NMEAMultiCast.ini";
}
