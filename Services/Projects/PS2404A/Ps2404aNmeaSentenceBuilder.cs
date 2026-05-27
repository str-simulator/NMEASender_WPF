using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Services.Projects;

namespace NMEASender.Wpf.Services.Projects.PS2404A;

public sealed class PS2404ANmeaSentenceBuilder : BaseProjectNmeaSentenceBuilder
{
    private static readonly IReadOnlyDictionary<NmeaSentenceId, string> TalkerOverrides = new Dictionary<NmeaSentenceId, string>
    {
        [NmeaSentenceId.Cur] = "WI",
        [NmeaSentenceId.Mwv] = "WI",
        [NmeaSentenceId.Mws] = "WI",
        [NmeaSentenceId.Mwh] = "WI",
        [NmeaSentenceId.Rot] = "HE",
        [NmeaSentenceId.Hdt] = "HE",
        [NmeaSentenceId.Ths] = "HE",
        [NmeaSentenceId.Rsa] = "AG",
        [NmeaSentenceId.RpmPort] = "HI",
        [NmeaSentenceId.RpmStbd] = "HI",
        [NmeaSentenceId.Vbw] = "GP",
        [NmeaSentenceId.Gpdtm] = "GP",
        [NmeaSentenceId.Dpt] = "SD",
        [NmeaSentenceId.Dbt] = "DP",
        [NmeaSentenceId.Vdr] = "VD",
        [NmeaSentenceId.Vhw] = "VD",
        [NmeaSentenceId.Dtm] = "VD",
        [NmeaSentenceId.Vdvbw] = "VD"
    };

    private static readonly NmeaTalkerProfile Ps2404aTalkerProfile = new(
        GenericTalkerId: "--",
        VbwTalkerId: "--",
        GnssTalkerId: "GP",
        HeadingTalkerId: "HE",
        CompassTalkerId: "HC",
        AisTalkerId: "AI",
        TalkerOverrides: TalkerOverrides);

    public override ProjectType ProjectType => ProjectType.PS2404A;

    protected override NmeaTalkerProfile TalkerProfile => Ps2404aTalkerProfile;

    public override string BuildVtgSentence(
        double gyroHeading,
        double magneticVariation,
        double waterSpeedKnots,
        double waterSpeedKmh,
        NmeaBuildOptions options)
    {
        double magneticHeading = NormalizeDegrees(gyroHeading + magneticVariation);
        string rawSentence = Full(string.Create(
            Invariant,
            $"GPVTG,{gyroHeading:0.0},T,{magneticHeading:0.0},M,{waterSpeedKnots:0.0},N,{waterSpeedKmh:0.0},K,A"));

        return ApplyTalkerProfile(One(rawSentence), NmeaSentenceId.Vtg, TalkerProfile, options.UseHdmOutput)[0];
    }

    protected override IReadOnlyList<string> BuildRawSentences(
        NmeaSentenceId id,
        NmeaDataDto data,
        NmeaDerivedData derived,
        NmeaBuildOptions options)
    {
        return id switch
        {
            NmeaSentenceId.Gga => One(BuildPs2404aGga(data)),
            NmeaSentenceId.Gll => One(BuildPs2404aGll(data)),
            NmeaSentenceId.Rmc => One(BuildPs2404aRmc(data, derived)),
            NmeaSentenceId.Vtg => One(BuildPs2404aVtg(data, derived)),
            NmeaSentenceId.Vdvbw => One(BuildPs2404aVdvbw(data)),
            NmeaSentenceId.RpmPort => One(BuildPs2404aRpmPort(data)),
            NmeaSentenceId.RpmStbd => One(BuildPs2404aRpmStbd(data)),
            NmeaSentenceId.Dpt => One(BuildPs2404aDpt(data)),
            NmeaSentenceId.Vhw => One(BuildPs2404aVhw(data, derived)),
            NmeaSentenceId.Vdr => One(BuildPs2404aVdr(data)),
            NmeaSentenceId.Dtm => One(BuildPs2404aDtm(data)),
            NmeaSentenceId.Gpdtm => One(BuildPs2404aGpdtm(data)),
            NmeaSentenceId.Ths => One(BuildPs2404aThs(data)),
            NmeaSentenceId.Mws => One(BuildPs2404aMws(data)),
            NmeaSentenceId.Mwh => One(BuildPs2404aMwh(data)),
            NmeaSentenceId.Htd => One(BuildPs2404aHtd(data)),
            NmeaSentenceId.Ttm => BuildPs2404aTtm(data),
            _ => base.BuildRawSentences(id, data, derived, options)
        };
    }

    private static string BuildPs2404aGga(NmeaDataDto data)
    {
        (string Value, char Hemisphere) lat = FormatPs2404aLatitude(data.Latitude);
        (string Value, char Hemisphere) lon = FormatPs2404aLongitude(data.Longitude);
        string body = string.Create(Invariant, $"GPGGA,{TimeOfDay(data.Time, true)},{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},1,05,02.5,,M,,M,,");
        return Full(body);
    }

    private static string BuildPs2404aGll(NmeaDataDto data)
    {
        (string Value, char Hemisphere) lat = FormatPs2404aLatitude(data.Latitude);
        (string Value, char Hemisphere) lon = FormatPs2404aLongitude(data.Longitude);
        string body = string.Create(Invariant, $"GPGLL,{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},{TimeOfDay(data.Time, true)},A,A");
        return Full(body);
    }

    private static string BuildPs2404aRmc(NmeaDataDto data, NmeaDerivedData derived)
    {
        (string Value, char Hemisphere) lat = FormatPs2404aLatitude(data.Latitude);
        (string Value, char Hemisphere) lon = FormatPs2404aLongitude(data.Longitude);
        bool useKose = data.KoseMode == 4;
        double sog = useKose ? data.KoseSogKnots : derived.SpeedOverGroundKnots;
        double cog = useKose ? NormalizeDegrees(data.KoseCog) : derived.CourseOverGround;
        string body = string.Create(
            Invariant,
            $"GPRMC,{TimeOfDay(data.Time, true)},A,{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},{sog:00.000},{cog:000.00},{data.Time:ddMMyy},{data.MagneticVariation:0.00},{lon.Hemisphere},M");
        return Full(body);
    }

    private static string BuildPs2404aVtg(NmeaDataDto data, NmeaDerivedData derived)
    {
        bool useKose = data.KoseMode == 4;
        double trueCourse = useKose ? NormalizeDegrees(data.KoseCog) : data.GyroHeading;
        double magneticCourse = useKose
            ? NormalizeDegrees(data.KoseCog + data.MagneticVariation)
            : derived.MagneticHeading;
        double speedKnots = useKose ? data.KoseSogKnots : derived.WaterSpeedKnots;
        double speedKmh = useKose ? data.KoseSogKnots * 1.852 : derived.WaterSpeedKmh;

        string body = string.Create(
            Invariant,
            $"GPVTG,{trueCourse:0.0},T,{magneticCourse:0.0},M,{speedKnots:0.0},N,{speedKmh:0.0},K,A");
        return Full(body);
    }

    private static string BuildPs2404aRpmPort(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"HIRPM,E,0,{data.RpmPort:0.0},0.0,A"));
    }

    private static string BuildPs2404aRpmStbd(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"HIRPM,E,1,{data.RpmStbd:0.0},0.0,A"));
    }

    private static string BuildPs2404aDpt(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"SDDPT,{data.WaterDepth:0.0},{data.WaterDepth - data.OwnshipDraft:0.0}"));
    }

    private static string BuildPs2404aVhw(NmeaDataDto data, NmeaDerivedData derived)
    {
        double magneticHeading = NormalizeDegrees(data.GyroHeading + data.MagneticVariation);
        bool useKose = data.KoseMode == 4;
        double speedKnots = useKose ? data.KoseSogKnots : derived.WaterSpeedKnots;
        double speedKmh = useKose ? data.KoseSogKnots * 1.852 : derived.WaterSpeedKmh;
        return Full(string.Create(
            Invariant,
            $"VDVHW,{data.GyroHeading:0.0},T,{magneticHeading:0.0},M,{speedKnots:0.0},N,{speedKmh:0.0},K"));
    }

    private static string BuildPs2404aVdr(NmeaDataDto data)
    {
        double magneticSet = NormalizeDegrees(data.CurrentSet + data.MagneticVariation);
        double driftKnots = data.CurrentDrift * 1.94384449;
        return Full(string.Create(Invariant, $"VDVDR,{data.CurrentSet:0.0},T,{magneticSet:0.0},M,{driftKnots:0.0},N"));
    }

    private static string BuildPs2404aDtm(NmeaDataDto data)
    {
        (string Value, char Hemisphere) lat = FormatPs2404aLatitude(data.Latitude);
        (string Value, char Hemisphere) lon = FormatPs2404aLongitude(data.Longitude);
        return Full(string.Create(Invariant, $"VDDTM,W84,,{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},,W84"));
    }

    private static string BuildPs2404aGpdtm(NmeaDataDto data)
    {
        (string Value, char Hemisphere) lat = FormatPs2404aLatitude(data.DatumOffsetLatitude);
        (string Value, char Hemisphere) lon = FormatPs2404aLongitude(data.DatumOffsetLongitude);
        return Full(string.Create(Invariant, $"GPDTM,W84,,{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},0.0,W84"));
    }

    private static string BuildPs2404aThs(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"HETHS,{data.Heading:000.0},A"));
    }

    private static string BuildPs2404aMws(NmeaDataDto data)
    {
        double windSpeed = data.WindSpeedMps;
        int beaufortNumber = 0;
        if (windSpeed < 2) beaufortNumber = 0;
        if (windSpeed < 4) beaufortNumber = 1;
        if (windSpeed < 7) beaufortNumber = 2;
        if (windSpeed < 11) beaufortNumber = 3;
        if (windSpeed < 17) beaufortNumber = 4;
        if (windSpeed < 22) beaufortNumber = 5;
        if (windSpeed < 28) beaufortNumber = 6;
        if (windSpeed < 34) beaufortNumber = 7;
        if (windSpeed < 41) beaufortNumber = 8;
        if (windSpeed < 48) beaufortNumber = 9;
        if (windSpeed < 56) beaufortNumber = 10;
        if (windSpeed < 64) beaufortNumber = 11;
        else beaufortNumber = 12;

        return Full(string.Create(Invariant, $"WIMWS,{beaufortNumber,2},{beaufortNumber,2}"));
    }

    private static string BuildPs2404aMwh(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"WIMWH,{data.WaveHeight * 3.28:0.0},f,{data.WaveHeight:0.0},M"));
    }

    private static string BuildPs2404aHtd(NmeaDataDto data)
    {
        string simulationTimeText = ParseSimulationTimeToText(data);
        return Full(string.Create(Invariant, $"--HTD,{simulationTimeText}"));
    }

    private static string BuildPs2404aVdvbw(NmeaDataDto data)
    {
        double heading = data.Heading * NmeaConstants.ToRadians;
        double currentAngle = data.CurrentSet * NmeaConstants.ToRadians;
        double currentLateral = data.CurrentDrift * Math.Sin(currentAngle - heading);
        double rotation = data.OwnshipLength / 2.0 * (data.OwnTurningRate * NmeaConstants.ToRadians);

        double sternGroundSpeed = (data.LateralSpeedMps - rotation) * 1.94384449;
        double sternWaterSpeed = ((data.LateralSpeedMps - currentLateral) - rotation) * 1.94384449;

        double dLongVel = data.LongitudinalSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
        double dLatVel = -data.LateralSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
        double dUw = data.LongitudinalSpeedMps - data.CurrentDrift * Math.Cos((data.Heading - data.CurrentSet) * NmeaConstants.ToRadians);
        double dVw = data.LateralSpeedMps - data.CurrentDrift * Math.Sin((data.Heading - data.CurrentSet) * NmeaConstants.ToRadians);
        double dLongVelW = dUw * 3600.0 / NmeaConstants.NauticalMileMeters;
        double dLatVelW = dVw * 3600.0 / NmeaConstants.NauticalMileMeters;

        return Full(string.Create(
            Invariant,
            $"VDVBW,{dLongVel:0.0},{dLatVel:0.0},A,{dLongVelW:0.0},{dLatVelW:0.0},A,{sternWaterSpeed:0.0},A,{sternGroundSpeed:0.0},A"));
    }

    private static IReadOnlyList<string> BuildPs2404aTtm(NmeaDataDto data)
    {
        if (!data.UsesTrafficShipData || data.TrafficShips.Count == 0)
        {
            return Array.Empty<string>();
        }

        List<string> sentences = new();
        for (int index = 0; index < data.TrafficShips.Count; index++)
        {
            TrafficShipData ship = data.TrafficShips[index];
            if (!ship.IsAisEnabled || !IsValidCoordinate(ship.Latitude, ship.Longitude))
            {
                continue;
            }

            double distanceNm = EstimateDistanceNm(data.OwnLatitude, data.OwnLongitude, ship.Latitude, ship.Longitude);
            double bearing = EstimateBearing(data.OwnLatitude, data.OwnLongitude, ship.Latitude, ship.Longitude);
            double speedKnots = Math.Sqrt(ship.LongitudinalSpeedMps * ship.LongitudinalSpeedMps + ship.LateralSpeedMps * ship.LateralSpeedMps)
                                * 3600.0 / NmeaConstants.NauticalMileMeters;
            string name = string.IsNullOrWhiteSpace(ship.ShipName) ? $"T{index + 1:00}" : ship.ShipName.Trim();
            string utc = $"{data.Time:HHmmss}.00";
            string body = string.Create(
                Invariant,
                $"RATTM,{(index + 1):00},{distanceNm:0.0},{bearing:0.0},T,{speedKnots:0.0},{NormalizeDegrees(ship.CourseOverGround):0.0},T,0.0,0.0,N,{name},L,R,{utc},A");
            sentences.Add(Full(body));
        }

        return sentences;
    }

    private static (string Value, char Hemisphere) FormatPs2404aLatitude(double latitude)
    {
        return FormatPositionWithDigits(latitude, latitude < 0.0 ? 'S' : 'N', degreeDigits: 2, minuteDigits: 5);
    }

    private static (string Value, char Hemisphere) FormatPs2404aLongitude(double longitude)
    {
        return FormatPositionWithDigits(longitude, longitude < 0.0 ? 'W' : 'E', degreeDigits: 3, minuteDigits: 5);
    }
}
