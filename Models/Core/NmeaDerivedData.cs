namespace NMEASender.Wpf.Models.Core;

public sealed class NmeaDerivedData
{
    public double CourseOverGround { get; private init; }

    public double SpeedOverGroundKnots { get; private init; }

    public double LongitudinalKnots { get; private init; }

    public double LateralKnots { get; private init; }

    public double WaterSpeedKnots { get; private init; }

    public double WaterSpeedKmh { get; private init; }

    public double WaterLongitudinalKnots { get; private init; }

    public double WaterLateralKnots { get; private init; }

    public double MagneticHeading { get; private init; }

    public static NmeaDerivedData From(NmeaDataDto data)
    {
        double course = NormalizeDegrees(Math.Atan2(data.LateralSpeedMps, data.LongitudinalSpeedMps) * NmeaConstants.ToDegrees + data.Heading);
        double longitudinalKnots = data.LongitudinalSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
        double lateralKnots = -data.LateralSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
        double speedOverGroundKnots = Math.Sqrt(longitudinalKnots * longitudinalKnots + lateralKnots * lateralKnots);
        double currentAngle = (data.Heading - data.CurrentSet) * NmeaConstants.ToRadians;
        double waterLongitudinal = data.LongitudinalSpeedMps - data.CurrentDrift * Math.Cos(currentAngle);
        double waterLateral = data.LateralSpeedMps - data.CurrentDrift * Math.Sin(currentAngle);

        return new NmeaDerivedData
        {
            CourseOverGround = course,
            SpeedOverGroundKnots = speedOverGroundKnots,
            LongitudinalKnots = longitudinalKnots,
            LateralKnots = lateralKnots,
            WaterSpeedKnots = waterLongitudinal * 3600.0 / NmeaConstants.NauticalMileMeters,
            WaterSpeedKmh = waterLongitudinal * 3600.0 / 1000.0,
            WaterLongitudinalKnots = waterLongitudinal * 3600.0 / NmeaConstants.NauticalMileMeters,
            WaterLateralKnots = waterLateral * 3600.0 / NmeaConstants.NauticalMileMeters,
            MagneticHeading = NormalizeDegrees(data.GyroHeading + data.MagneticVariation)
        };
    }

    private static double NormalizeDegrees(double degrees)
    {
        degrees %= 360.0;
        return degrees < 0.0 ? degrees + 360.0 : degrees;
    }
}
