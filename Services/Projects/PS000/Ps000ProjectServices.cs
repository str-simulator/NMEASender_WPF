using NMEASender.Wpf.Models.Projects;

namespace NMEASender.Wpf.Services.Projects.PS000;

public sealed class Ps000NmeaSentenceBuilder : BaseProjectNmeaSentenceBuilder
{
    private static readonly NmeaTalkerProfile Ps000TalkerProfile = new(
        GenericTalkerId: "--",
        VbwTalkerId: "--",
        GnssTalkerId: "GP",
        HeadingTalkerId: "HE",
        CompassTalkerId: "HC",
        AisTalkerId: "AI");

    public override ProjectType ProjectType => ProjectType.PS000;

    protected override NmeaTalkerProfile TalkerProfile => Ps000TalkerProfile;
}
