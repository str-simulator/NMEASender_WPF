namespace NMEASender.Wpf.Models;

public enum ProjectType
{
    PS2603, // Samsung Heavy Industries
    PS2514, // HD Hyundai Global R&D Center | Project 2514
    PS2404A // HD Hyundai Global R&D Center | Project 2404A
}

public sealed record NmeaTalkerProfile(
    string GenericTalkerId,
    string VbwTalkerId,
    string GnssTalkerId,
    string HeadingTalkerId,
    string CompassTalkerId,
    string AisTalkerId,
    IReadOnlyDictionary<NmeaSentenceId, string>? TalkerOverrides = null)
{
    public string ResolveTalkerId(NmeaSentenceId sentenceId, bool useHdmOutput)
    {
        if (TalkerOverrides is not null && TalkerOverrides.TryGetValue(sentenceId, out string? overriddenTalkerId))
        {
            return overriddenTalkerId;
        }

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
