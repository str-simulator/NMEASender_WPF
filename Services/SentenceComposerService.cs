using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services;

public sealed class SentenceComposerService
{
    private double _lastVtgKnots;
    private double _lastVtgKmh;

    public IReadOnlyList<string> ComposeAndApplyPreview(
        SentenceItem item,
        NmeaDataDto data,
        bool isIosSource,
        NmeaBuildOptions options)
    {
        if (item.Id == NmeaSentenceId.Vtg && isIosSource)
        {
            string sentence = BuildIosVtgSentence(data);
            item.PrimaryText = sentence.TrimEnd();
            item.SecondaryText = string.Empty;
            return new[] { sentence };
        }

        IReadOnlyList<string> sentences = NmeaSentenceBuilder.Build(item.Id, data, options);
        item.PrimaryText = sentences.Count > 0 ? sentences[0].TrimEnd() : string.Empty;
        item.SecondaryText = sentences.Count > 1 ? sentences[1].TrimEnd() : string.Empty;
        return sentences;
    }

    public static bool ShouldSend(SentenceItem item, bool isIosSource, NmeaDataDto data)
    {
        if (!isIosSource)
        {
            return true;
        }

        return item.Id switch
        {
            NmeaSentenceId.Gga or NmeaSentenceId.Gll or NmeaSentenceId.Rmc or NmeaSentenceId.Vtg or NmeaSentenceId.Zda
                => !data.FailGps,
            NmeaSentenceId.Hdt => !data.FailGyro,
            NmeaSentenceId.Vbw => !data.FailLog,
            NmeaSentenceId.Dbt or NmeaSentenceId.Dpt => !data.FailEcho,
            _ => true
        };
    }

    private string BuildIosVtgSentence(NmeaDataDto data)
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

        return NmeaSentenceBuilder.BuildVtgSentence(data.GyroHeading, data.MagneticVariation, waterKnots, waterKmh);
    }
}
