using NMEASender.Wpf.Models.Projects;

namespace NMEASender.Wpf.Services.Projects.PS2603;

public sealed class Ps2603NmeaSentenceBuilder : BaseProjectNmeaSentenceBuilder
{
    private static readonly NmeaTalkerProfile Ps2603TalkerProfile = new(
        GenericTalkerId: "--",
        VbwTalkerId: "IN",
        GnssTalkerId: "GP",
        HeadingTalkerId: "HE",
        CompassTalkerId: "HC",
        AisTalkerId: "AI");

    public override ProjectType ProjectType => ProjectType.PS2603;

    protected override NmeaTalkerProfile TalkerProfile => Ps2603TalkerProfile;
}

public sealed class Ps2603SentenceFramePolicy : BaseProjectSentenceFramePolicy
{
    public override ProjectType ProjectType => ProjectType.PS2603;

    public override bool SupportsPerSentenceMulticastAddress => true;
}
