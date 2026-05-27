using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;

namespace NMEASender.Wpf.Services.Interfaces.Projects;

public interface IProjectNmeaSentenceBuilder
{
    ProjectType ProjectType { get; }

    IReadOnlyList<string> Build(NmeaSentenceId id, NmeaDataDto data, NmeaBuildOptions options);

    byte Checksum(string body);

    string BuildVtgSentence(double gyroHeading, double magneticVariation, double waterSpeedKnots, double waterSpeedKmh, NmeaBuildOptions options);
}
