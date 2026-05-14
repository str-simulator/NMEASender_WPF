using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services;

namespace NMEASender.Wpf.Services.Interfaces;

public interface INmeaSentenceBuilderService
{
    IReadOnlyList<string> Build(NmeaSentenceId id, NmeaDataDto data, NmeaBuildOptions options);

    byte Checksum(string body);

    string BuildVtgSentence(double gyroHeading, double magneticVariation, double waterSpeedKnots, double waterSpeedKmh);
}
