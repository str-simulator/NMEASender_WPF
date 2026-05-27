using NMEASender.Wpf.Models.Core;

namespace NMEASender.Wpf.Services.Interfaces.Transmission;

public interface INmeaSentenceBuilderService
{
    IReadOnlyList<string> Build(NmeaSentenceId id, NmeaDataDto data, NmeaBuildOptions options);

    byte Checksum(string body);

    string BuildVtgSentence(double gyroHeading, double magneticVariation, double waterSpeedKnots, double waterSpeedKmh, NmeaBuildOptions options);
}
