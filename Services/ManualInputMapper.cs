using System.Globalization;
using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services;

public readonly record struct ManualInputValues(string Longitude, string Latitude, string Speed, string Heading);

public static class ManualInputMapper
{
    public static ManualInputValues ToInputValues(NmeaDataDto data)
    {
        return new ManualInputValues(
            data.Longitude.ToString("0.0000", CultureInfo.InvariantCulture),
            data.Latitude.ToString("0.0000", CultureInfo.InvariantCulture),
            data.SpeedKnots.ToString("0.0", CultureInfo.InvariantCulture),
            data.Heading.ToString("0.0", CultureInfo.InvariantCulture));
    }

    public static NmeaDataDto ApplyToData(NmeaDataDto baseData, ManualInputValues input)
    {
        NmeaDataDto data = baseData.Clone();
        data.Time = DateTime.Now;
        data.SimulationTimeSeconds = new DateTimeOffset(data.Time).ToUnixTimeSeconds();
        data.Longitude = ParseDouble(input.Longitude, data.Longitude);
        data.Latitude = ParseDouble(input.Latitude, data.Latitude);
        data.SpeedKnots = ParseDouble(input.Speed, data.SpeedKnots);
        data.Heading = NormalizeDegrees(ParseDouble(input.Heading, data.Heading));
        data.GyroHeading = data.Heading;
        data.OwnLatitude = data.Latitude;
        data.OwnLongitude = data.Longitude;
        data.LongitudinalSpeedMps = data.SpeedKnots * NmeaConstants.NauticalMileMeters / 3600.0;
        data.LateralSpeedMps = 0.0;
        data.MagneticVariation = 0.0;
        data.WindDirection = NormalizeDegrees(data.Heading + 30.0);
        data.WindRelativeDirection = 30.0;
        data.WindSpeedMps = 5.0;
        data.WindRelativeSpeedMps = 5.0;
        data.RpmPort = data.SpeedKnots * 60.0;
        data.RpmStbd = data.SpeedKnots * 60.0;
        data.PitchPort = Math.Clamp(data.SpeedKnots * 5.0, -100.0, 100.0);
        data.PitchStbd = Math.Clamp(data.SpeedKnots * 5.0, -100.0, 100.0);
        data.EngineCommandPort = Math.Clamp(data.SpeedKnots / 20.0, -1.0, 1.0);
        data.EngineCommandStbd = Math.Clamp(data.SpeedKnots / 20.0, -1.0, 1.0);
        data.IsFinished = false;
        data.FailGps = false;
        data.FailGyro = false;
        data.FailLog = false;
        data.FailEcho = false;
        data.UsesTrafficShipData = false;
        data.TrafficShips = [];
        return data;
    }

    private static double ParseDouble(string value, double fallback)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) ||
               double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed)
            ? parsed
            : fallback;
    }

    private static double NormalizeDegrees(double degrees)
    {
        degrees %= 360.0;
        return degrees < 0.0 ? degrees + 360.0 : degrees;
    }
}
