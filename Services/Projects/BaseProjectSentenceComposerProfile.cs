using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services.Projects;

public abstract class BaseProjectSentenceComposerProfile : IProjectSentenceComposerProfile
{
    private double _lastVtgKnots;
    private double _lastVtgKmh;

    public abstract ProjectType ProjectType { get; }

    public virtual string BuildIosVtgSentence(
        NmeaDataDto data,
        NmeaBuildOptions options,
        INmeaSentenceBuilderService sentenceBuilder)
    {
        double waterLongitudinal = data.LongitudinalSpeedMps - data.CurrentDrift * Math.Cos((data.Heading - data.CurrentSet) * NmeaConstants.ToRadians);
        double waterKnots = waterLongitudinal * 3600.0 / NmeaConstants.NauticalMileMeters;
        double waterKmh = waterLongitudinal * 3600.0 / 1000.0;
        if (!data.FailLog)
        {
            _lastVtgKnots = waterKnots;
            _lastVtgKmh = waterKmh;
        }
        else
        {
            waterKnots = _lastVtgKnots;
            waterKmh = _lastVtgKmh;
        }

        return sentenceBuilder.BuildVtgSentence(data.GyroHeading, data.MagneticVariation, waterKnots, waterKmh, options);
    }

    protected static double NormalizeDegrees(double degrees)
    {
        degrees %= 360.0;
        return degrees < 0.0 ? degrees + 360.0 : degrees;
    }
}
