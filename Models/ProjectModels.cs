namespace NMEASender.Wpf.Models;

public enum ProjectType
{
    PS2603, // 삼성중공업
    PS2514, // 현대글로벌 R&D 센터 | 권기연 책임
    PS2404A // 현대글로벌 R&D 센터 | 장연욱 책임
}

public sealed record NmeaTalkerProfile(
    string GenericTalkerId,
    string VbwTalkerId,
    string GnssTalkerId,
    string HeadingTalkerId,
    string CompassTalkerId,
    string AisTalkerId)
{
    public string ResolveTalkerId(NmeaSentenceId sentenceId, bool useHdmOutput)
    {
        return sentenceId switch
        {
            NmeaSentenceId.Gga or NmeaSentenceId.Gll or NmeaSentenceId.Rmc or NmeaSentenceId.Vtg or NmeaSentenceId.Zda
                => GnssTalkerId,
            NmeaSentenceId.Hdt => HeadingTalkerId,
            NmeaSentenceId.Vbw => VbwTalkerId,
            NmeaSentenceId.Hdg when !useHdmOutput => CompassTalkerId,
            NmeaSentenceId.Vdm or NmeaSentenceId.Vdo => AisTalkerId,
            _ => GenericTalkerId
        };
    }
}

public static class ProjectTalkerProfiles
{
    public static NmeaTalkerProfile For(ProjectType projectType)
    {
        return projectType switch
        {
            // 삼성중공업
            ProjectType.PS2603 => new NmeaTalkerProfile(
                GenericTalkerId: "--",
                VbwTalkerId: "IN",
                GnssTalkerId: "GP",
                HeadingTalkerId: "HE",
                CompassTalkerId: "HC",
                AisTalkerId: "AI"),

            // 현대글로벌 | 권기연 책임
            ProjectType.PS2514 => new NmeaTalkerProfile(
                GenericTalkerId: "--",
                VbwTalkerId: "--",
                GnssTalkerId: "GP",
                HeadingTalkerId: "HE",
                CompassTalkerId: "HC",
                AisTalkerId: "AI"),

            // 현대글로벌 | 장연욱 책임
            ProjectType.PS2404A => new NmeaTalkerProfile(
                GenericTalkerId: "--",
                VbwTalkerId: "--",
                GnssTalkerId: "GP",
                HeadingTalkerId: "HE",
                CompassTalkerId: "HC",
                AisTalkerId: "AI"),

            // Default
            _ => new NmeaTalkerProfile(
                GenericTalkerId: "--",
                VbwTalkerId: "--",
                GnssTalkerId: "--",
                HeadingTalkerId: "--",
                CompassTalkerId: "--",
                AisTalkerId: "--")
        };
    }
}
