using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Models.Projects.PS2404A;
using NMEASender.Wpf.Services.Interfaces.Transmission;

namespace NMEASender.Wpf.Services.Projects.PS2404A;

public sealed class PS2404ASentenceComposerProfile : BaseProjectSentenceComposerProfile
{
    public override ProjectType ProjectType => ProjectType.PS2404A;

    public override string BuildIosVtgSentence(
        NmeaDataDto data,
        NmeaBuildOptions options,
        INmeaSentenceBuilderService sentenceBuilder)
    {
        bool useKsoe = data.KsoeMode == PS2404AKsoeModes.EngineAndRudder;
        double sogKnots;
        double trueCourse;
        double magneticVariation;
        if (useKsoe)
        {
            sogKnots = data.KsoeSogKnots;
            trueCourse = data.KsoeCog;
            magneticVariation = 0.0;
        }
        else
        {
            double dLongVel = data.LongitudinalSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
            double dLatVel = -data.LateralSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
            sogKnots = Math.Sqrt(dLongVel * dLongVel + dLatVel * dLatVel);
            trueCourse = NormalizeSingleTurn(Math.Atan2(data.LateralSpeedMps, data.LongitudinalSpeedMps) * NmeaConstants.ToDegrees + data.Heading);
            magneticVariation = data.MagneticVariation;
        }

        return sentenceBuilder.BuildVtgSentence(trueCourse, magneticVariation, sogKnots, sogKnots * 1.852, options);
    }

    private static double NormalizeSingleTurn(double degrees)
    {
        if (degrees > 360.0)
        {
            return degrees - 360.0;
        }

        if (degrees < 0.0)
        {
            return degrees + 360.0;
        }

        return degrees;
    }
}
