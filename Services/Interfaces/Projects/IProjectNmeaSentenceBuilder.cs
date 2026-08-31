using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;

namespace NMEASender.Wpf.Services.Interfaces.Projects;

public interface IProjectNmeaSentenceBuilder
{
    ProjectType ProjectType { get; }

    IReadOnlyList<string> Build(NmeaSentenceId id, NmeaDataDto data, NmeaBuildOptions options, string talkerId);

    byte Checksum(string body);

    string BuildVtgSentence(double gyroHeading, double magneticVariation, double waterSpeedKnots, double waterSpeedKmh, NmeaBuildOptions options, string talkerId);

    string ResolveDefaultTalkerId(NmeaSentenceId id, bool useHdmOutput);
}
