using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services.Projects.PS2404A;

public sealed class Ps2404aSentenceComposerProfile : BaseProjectSentenceComposerProfile
{
    public override ProjectType ProjectType => ProjectType.PS2404A;

    public override string BuildIosVtgSentence(
        NmeaDataDto data,
        NmeaBuildOptions options,
        INmeaSentenceBuilderService sentenceBuilder)
    {
        bool useKose = data.KoseMode == 4;
        double sogKnots;
        double trueCourse;
        if (useKose)
        {
            sogKnots = data.KoseSogKnots;
            trueCourse = NormalizeDegrees(data.KoseCog);
        }
        else
        {
            double dLongVel = data.LongitudinalSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
            double dLatVel = -data.LateralSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
            sogKnots = Math.Sqrt(dLongVel * dLongVel + dLatVel * dLatVel);
            trueCourse = NormalizeDegrees(
                Math.Atan2(data.LateralSpeedMps, data.LongitudinalSpeedMps) * NmeaConstants.ToDegrees + data.Heading);
        }

        return sentenceBuilder.BuildVtgSentence(trueCourse, data.MagneticVariation, sogKnots, sogKnots * 1.852, options);
    }
}
