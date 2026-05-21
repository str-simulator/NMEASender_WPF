using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Projects.PS2514;

public sealed class Ps2514NmeaSentenceBuilder : BaseProjectNmeaSentenceBuilder
{
    private static readonly NmeaTalkerProfile Ps2514TalkerProfile = new(
        GenericTalkerId: "--",
        VbwTalkerId: "--",
        GnssTalkerId: "GP",
        HeadingTalkerId: "HE",
        CompassTalkerId: "HC",
        AisTalkerId: "AI");

    public override ProjectType ProjectType => ProjectType.PS2514;

    protected override NmeaTalkerProfile TalkerProfile => Ps2514TalkerProfile;
}
