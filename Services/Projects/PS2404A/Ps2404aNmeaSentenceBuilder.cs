using NMEASender.Wpf.Models.Ais;
using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Models.Projects.PS2404A;

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
        [NmeaSentenceId.Hdg] = "HE",
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
        NmeaBuildOptions options,
        string talkerId)
    {
        double magneticHeading = gyroHeading + magneticVariation;
        string rawSentence = Full(string.Create(
            Invariant,
            $"GPVTG,{gyroHeading:0.0},T,{magneticHeading:0.0},M,{waterSpeedKnots:0.0},N,{waterSpeedKmh:0.0},K,A"));

        return ApplyTalkerId(One(rawSentence), talkerId)[0];
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
            NmeaSentenceId.Zda => One(BuildPs2404aZda()),
            NmeaSentenceId.Hdt => One(BuildPs2404aHdt(data)),
            NmeaSentenceId.Vbw => One(BuildPs2404aVbw(data)),
            NmeaSentenceId.Rot => One(BuildPs2404aRot(data)),
            NmeaSentenceId.Rsa => One(BuildPs2404aRsa(data)),
            NmeaSentenceId.Vdvbw => One(BuildPs2404aVdvbw(data)),
            NmeaSentenceId.RpmPort => One(BuildPs2404aRpmPort(data)),
            NmeaSentenceId.RpmStbd => One(BuildPs2404aRpmStbd(data)),
            NmeaSentenceId.Mwv => One(BuildPs2404aMwv(data, options.TrueWind)),
            NmeaSentenceId.Hdg => One(BuildPs2404aHdg(data)),
            NmeaSentenceId.Dpt => One(BuildPs2404aDpt(data)),
            NmeaSentenceId.Dbt => One(BuildPs2404aDbt(data)),
            NmeaSentenceId.Cur => One(BuildPs2404aCur(data)),
            NmeaSentenceId.Vhw => One(BuildPs2404aVhw(data, derived)),
            NmeaSentenceId.Vdr => One(BuildPs2404aVdr(data)),
            NmeaSentenceId.Dtm => One(BuildPs2404aDtm(data)),
            NmeaSentenceId.Gpdtm => One(BuildPs2404aGpdtm(data)),
            NmeaSentenceId.Ths => One(BuildPs2404aThs(data)),
            NmeaSentenceId.Mws => One(BuildPs2404aMws(data)),
            NmeaSentenceId.Mwh => One(BuildPs2404aMwh(data)),
            NmeaSentenceId.Htd => One(BuildPs2404aHtd(data)),
            NmeaSentenceId.Ttm => BuildPs2404aTtm(data),
            NmeaSentenceId.Vdm => BuildPs2404aVdm(data),
            NmeaSentenceId.Vdo => One(BuildPs2404aVdo(data)),
            _ => base.BuildRawSentences(id, data, derived, options)
        };
    }

    private static string BuildPs2404aGga(NmeaDataDto data)
    {
        DateTime utcNow = DateTime.UtcNow;
        (string Value, char Hemisphere) lat = FormatPs2404aLatitude(data.OwnLatitude);
        (string Value, char Hemisphere) lon = FormatPs2404aLongitude(data.OwnLongitude);
        string body = string.Create(Invariant, $"GPGGA,{TimeOfDay(utcNow, true)},{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},1,05,02.5,,M,,M,,");
        return Full(body);
    }

    private static string BuildPs2404aGll(NmeaDataDto data)
    {
        DateTime utcNow = DateTime.UtcNow;
        (string Value, char Hemisphere) lat = FormatPs2404aLatitude(data.OwnLatitude);
        (string Value, char Hemisphere) lon = FormatPs2404aLongitude(data.OwnLongitude);
        string body = string.Create(Invariant, $"GPGLL,{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},{TimeOfDay(utcNow, true)},A,A");
        return Full(body);
    }

    private static string BuildPs2404aRmc(NmeaDataDto data, NmeaDerivedData derived)
    {
        DateTime utcNow = DateTime.UtcNow;
        (string Value, char Hemisphere) lat = FormatPs2404aLatitude(data.OwnLatitude);
        (string Value, char Hemisphere) lon = FormatPs2404aLongitude(data.OwnLongitude);
        bool useKsoe = IsKsoeEngineRudderMode(data);
        double sog = useKsoe ? data.KsoeSogKnots : CalculatePs2404aSpeedOverGroundKnots(data);
        double cog = useKsoe ? data.KsoeCog : CalculatePs2404aCourseOverGround(data);
        string body = string.Create(
            Invariant,
            $"GPRMC,{TimeOfDay(utcNow, true)},A,{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},{sog:0.000},{cog:00.00},{utcNow:ddMMyy},{data.MagneticVariation:0.00},{lon.Hemisphere},M");
        return Full(body);
    }

    private static string BuildPs2404aVtg(NmeaDataDto data, NmeaDerivedData derived)
    {
        bool useKsoe = IsKsoeEngineRudderMode(data);
        double trueCourse = useKsoe ? data.KsoeCog : CalculatePs2404aCourseOverGround(data);
        double magneticCourse = useKsoe ? trueCourse : trueCourse + data.MagneticVariation;
        double speedKnots = useKsoe ? data.KsoeSogKnots : CalculatePs2404aSpeedOverGroundKnots(data);
        double speedKmh = speedKnots * 1.852;

        string body = string.Create(
            Invariant,
            $"GPVTG,{trueCourse:0.0},T,{magneticCourse:0.0},M,{speedKnots:0.0},N,{speedKmh:0.0},K,A");
        return Full(body);
    }

    private static string BuildPs2404aZda()
    {
        DateTime utcNow = DateTime.UtcNow;
        return Full(string.Create(Invariant, $"GPZDA,{utcNow:HHmmss},{utcNow:dd},{utcNow:MM},{utcNow:yyyy},-9,00"));
    }

    private static string BuildPs2404aHdt(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"HEHDT,{data.GyroHeading:000.0},T"));
    }

    private static string BuildPs2404aVbw(NmeaDataDto data)
    {
        Ps2404aVbwValues values = CalculatePs2404aVbwValues(data);
        string body = string.Create(
            Invariant,
            $"GPVBW,{values.WaterLongitudinalKnots:0.0},{values.WaterLateralKnots:0.0},A,{values.LongitudinalKnots:0.0},{values.LateralKnots:0.0},A");
        return Full(body);
    }

    private static string BuildPs2404aRot(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"HEROT,{data.OwnTurningRate * 60.0:0.0},A"));
    }

    private static string BuildPs2404aRsa(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"AGRSA,{data.RudderStbd * -1.0:0.0},A,{data.RudderPort * -1.0:0.0},A"));
    }

    private static string BuildPs2404aRpmPort(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"HIRPM,E,0,{data.RpmPort:0.0},0.0,A"));
    }

    private static string BuildPs2404aRpmStbd(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"HIRPM,E,1,{data.RpmStbd:0.0},0.0,A"));
    }

    private static string BuildPs2404aMwv(NmeaDataDto data, bool trueWind)
    {
        if (trueWind)
        {
            return Full(string.Create(Invariant, $"WIMWV,{data.WindDirection:0.0},T,{data.WindSpeedMps * 1.94384449:0.0},N,A"));
        }

        return Full(string.Create(Invariant, $"WIMWV,{data.WindRelativeDirection:0.0},R,{data.WindRelativeSpeedMps * 1.94384449:0.0},N,A"));
    }

    private static string BuildPs2404aHdg(NmeaDataDto data)
    {
        double magnetic = data.Heading + data.MagneticVariation;
        return Full(string.Create(Invariant, $"HEHDM,{magnetic:0},M"));
    }

    private static string BuildPs2404aDpt(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"SDDPT,{data.WaterDepth:0.0},{data.WaterDepth - data.OwnshipDraft:0.0}"));
    }

    private static string BuildPs2404aDbt(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"DPDBT,{data.WaterDepth * 3.2808:0.0},f,{data.WaterDepth:0.0},M,{data.WaterDepth * 0.5468:0.0},F"));
    }

    private static string BuildPs2404aCur(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"WICUR,A,0,1.0,{data.HeightTide:0.0},{data.CurrentSet:0.0},T,{data.CurrentDrift * 3600.0 / NmeaConstants.NauticalMileMeters:0.0},1.0,{data.WaterDepth:0.0},T,P"));
    }

    private static string BuildPs2404aVhw(NmeaDataDto data, NmeaDerivedData derived)
    {
        double magneticHeading = NormalizeDegrees(data.GyroHeading + data.MagneticVariation);
        bool useKsoe = IsKsoeEngineRudderMode(data);
        double speedKnots = useKsoe ? data.KsoeSogKnots : derived.WaterSpeedKnots;
        double speedKmh = useKsoe ? data.KsoeSogKnots * 1.852 : derived.WaterSpeedKmh;
        return Full(string.Create(
            Invariant,
            $"VDVHW,{data.GyroHeading:0.0},T,{magneticHeading:0.0},M,{speedKnots:0.0},N,{speedKmh:0.0},K"));
    }

    private static bool IsKsoeEngineRudderMode(NmeaDataDto data)
    {
        return data.KsoeMode == PS2404AKsoeModes.EngineAndRudder;
    }

    private static string BuildPs2404aVdr(NmeaDataDto data)
    {
        double driftKnots = data.CurrentDrift * 1.94384449;
        return Full(string.Create(Invariant, $"VDVDR,{data.CurrentSet:0.0},T,{data.CurrentSet + data.MagneticVariation:0.0},M.{driftKnots:0.0},N"));
    }

    private static string BuildPs2404aDtm(NmeaDataDto data)
    {
        (string Value, char Hemisphere) lat = FormatPs2404aDtmLatitude(data.OwnLatitude);
        (string Value, char Hemisphere) lon = FormatPs2404aDtmLongitude(data.OwnLongitude);
        return Full(string.Create(Invariant, $"VDDTM,W84,,{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},,W84"));
    }

    private static string BuildPs2404aGpdtm(NmeaDataDto data)
    {
        (string Value, char Hemisphere) lat = FormatPs2404aGpdtmLatitude(data.DatumOffsetLatitude);
        (string Value, char Hemisphere) lon = FormatPs2404aGpdtmLongitude(data.DatumOffsetLongitude);
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
        Ps2404aVbwValues values = CalculatePs2404aVbwValues(data);
        return Full(string.Create(
            Invariant,
            $"VDVBW,{values.WaterLongitudinalKnots:0.0},{values.WaterLateralKnots:0.0},A,{values.LongitudinalKnots:0.0},{values.LateralKnots:0.0},A,{values.SternWaterSpeedKnots:0.0},A,{values.SternGroundSpeedKnots:0.0},A"));
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
            if (!IsValidCoordinate(ship.Latitude, ship.Longitude))
            {
                continue;
            }

            double distanceNm = Math.Sqrt(
                Math.Pow(ship.PositionX - data.OwnshipPositionX, 2.0) +
                Math.Pow(ship.PositionY - data.OwnshipPositionY, 2.0)) / NmeaConstants.NauticalMileMeters;
            double direction = Math.Atan2(
                ship.PositionX - data.OwnshipPositionX,
                ship.PositionY - data.OwnshipPositionY) * NmeaConstants.ToDegrees;
            double bearing = NormalizeLegacyLoop(direction);
            double speedKnots = Math.Sqrt(ship.LongitudinalSpeedMps * ship.LongitudinalSpeedMps + ship.LateralSpeedMps * ship.LateralSpeedMps)
                                * 3600.0 / NmeaConstants.NauticalMileMeters;
            double course = NormalizeLegacyLoop(Math.Atan2(ship.LateralSpeedMps, ship.LongitudinalSpeedMps) * NmeaConstants.ToDegrees + ship.CourseOverGround);
            double ownSpeedKnots = Math.Sqrt(data.LateralSpeedMps * data.LateralSpeedMps + data.LongitudinalSpeedMps * data.LongitudinalSpeedMps)
                                   * 3600.0 / NmeaConstants.NauticalMileMeters;
            double ownCourse = Math.Atan2(data.LateralSpeedMps, data.LongitudinalSpeedMps) * NmeaConstants.ToDegrees + data.Heading;
            (double Tcpa, double Dcpa) cpa = CalculatePs2404aCpa(distanceNm, direction, ownSpeedKnots, ownCourse, speedKnots, course);
            double dcpa = cpa.Dcpa >= 1.0 ? cpa.Dcpa : cpa.Dcpa * NmeaConstants.NauticalMileMeters / 0.9144;
            int targetNumber = ship.SharedIndex >= 0 ? ship.SharedIndex : index;
            string name = string.IsNullOrWhiteSpace(ship.ShipName) ? $"T{index + 1:00}" : ship.ShipName.Trim();
            DateTime utcNow = DateTime.UtcNow;
            string utc = string.Create(Invariant, $"{utcNow:HHmmss}.{utcNow.Millisecond / 10:00}");
            string body = string.Create(
                Invariant,
                $"RATTM,{targetNumber:00},{distanceNm:0.0},{bearing:0.0},T,{speedKnots:0.0},{course:0.0},T,{dcpa:0.0},{cpa.Tcpa:0.0},K,{name},T,R,{utc},A");
            sentences.Add(Full(body));
        }

        return sentences;
    }

    private static IReadOnlyList<string> BuildPs2404aVdm(NmeaDataDto data)
    {
        if (!data.UsesTrafficShipData || data.TrafficShips.Count == 0)
        {
            return Array.Empty<string>();
        }

        List<string> sentences = new();
        foreach (TrafficShipData ship in data.TrafficShips)
        {
            if (ship.Mmsi == 0 || !IsValidCoordinate(ship.Latitude, ship.Longitude))
            {
                continue;
            }

            double speedOverGround = Math.Sqrt(ship.LongitudinalSpeedMps * ship.LongitudinalSpeedMps + ship.LateralSpeedMps * ship.LateralSpeedMps) / KnotsDivisor;
            double courseOverGround = NormalizeSingleTurn(ship.CourseOverGround);
            double headingLateral = -data.CurrentDrift * Math.Sin((data.CurrentSet - courseOverGround) * NmeaConstants.ToRadians);
            double heading = NormalizeSingleTurn(Math.Atan2(headingLateral, ship.LongitudinalSpeedMps) * NmeaConstants.ToDegrees + courseOverGround);
            string payload = AisPayloadBuilder.BuildLegacyPositionPayload(
                ship.Mmsi,
                ship.Latitude,
                ship.Longitude,
                speedOverGround,
                courseOverGround,
                heading,
                ship.TurningRate,
                DateTime.UtcNow);
            sentences.Add(Full($"AIVDM,1,1,,A,{payload},0", ais: true));
        }

        return sentences;
    }

    private static string BuildPs2404aVdo(NmeaDataDto data)
    {
        const int Ps2404aOwnMmsi = 240100001;

        double speedOverGround = Math.Sqrt(data.LongitudinalSpeedMps * data.LongitudinalSpeedMps + data.LateralSpeedMps * data.LateralSpeedMps) / KnotsDivisor;
        double courseOverGround = NormalizeSingleTurn(data.Heading);
        double headingLateral = -data.CurrentDrift * Math.Sin((data.CurrentSet - courseOverGround) * NmeaConstants.ToRadians);
        double heading = NormalizeSingleTurn(Math.Atan2(headingLateral, data.LongitudinalSpeedMps) * NmeaConstants.ToDegrees + courseOverGround);
        double rateOfTurn = data.OwnTurningRate * 60.0;
        string payload = AisPayloadBuilder.BuildLegacyPositionPayload(
            Ps2404aOwnMmsi,
            data.OwnLatitude,
            data.OwnLongitude,
            speedOverGround,
            courseOverGround,
            heading,
            rateOfTurn,
            DateTime.UtcNow);
        return Full($"AIVDO,1,1,,A,{payload},0", ais: true);
    }

    private static Ps2404aVbwValues CalculatePs2404aVbwValues(NmeaDataDto data)
    {
        double heading = data.Heading * NmeaConstants.ToRadians;
        double currentAngle = data.CurrentSet * NmeaConstants.ToRadians;
        double currentLateral = data.CurrentDrift * Math.Sin(currentAngle - heading);
        double rotation = data.OwnshipLength / 2.0 * data.OwnTurningRate * NmeaConstants.ToRadians;
        double waterLongitudinalMps = data.LongitudinalSpeedMps - data.CurrentDrift * Math.Cos((data.Heading - data.CurrentSet) * NmeaConstants.ToRadians);
        double waterLateralMps = data.LateralSpeedMps - data.CurrentDrift * Math.Sin((data.Heading - data.CurrentSet) * NmeaConstants.ToRadians);

        return new Ps2404aVbwValues(
            LongitudinalKnots: data.LongitudinalSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters,
            LateralKnots: -data.LateralSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters,
            WaterLongitudinalKnots: waterLongitudinalMps * 3600.0 / NmeaConstants.NauticalMileMeters,
            WaterLateralKnots: waterLateralMps * 3600.0 / NmeaConstants.NauticalMileMeters,
            SternWaterSpeedKnots: ((data.LateralSpeedMps - currentLateral) - rotation) * 1.94384449,
            SternGroundSpeedKnots: (data.LateralSpeedMps - rotation) * 1.94384449);
    }

    private static double CalculatePs2404aSpeedOverGroundKnots(NmeaDataDto data)
    {
        double longitudinalKnots = data.LongitudinalSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
        double lateralKnots = -data.LateralSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
        return Math.Sqrt(longitudinalKnots * longitudinalKnots + lateralKnots * lateralKnots);
    }

    private static double CalculatePs2404aCourseOverGround(NmeaDataDto data)
    {
        return NormalizeSingleTurn(Math.Atan2(data.LateralSpeedMps, data.LongitudinalSpeedMps) * NmeaConstants.ToDegrees + data.Heading);
    }

    private static (double Tcpa, double Dcpa) CalculatePs2404aCpa(
        double distance,
        double direction,
        double ownSpeed,
        double ownCourse,
        double targetSpeed,
        double targetCourse)
    {
        const double Zero = 1.0e-10;

        if (distance == 0.0)
        {
            return (0.0, 0.0);
        }

        double directionRadians = direction * NmeaConstants.ToRadians;
        double ownCourseRadians = ownCourse * NmeaConstants.ToRadians;
        double targetCourseRadians = targetCourse * NmeaConstants.ToRadians;
        double vc = ownSpeed * Math.Cos(directionRadians - ownCourseRadians) -
                    targetSpeed * Math.Cos(directionRadians - targetCourseRadians);
        double vs = ownSpeed * Math.Sin(directionRadians - ownCourseRadians) -
                    targetSpeed * Math.Sin(directionRadians - targetCourseRadians);

        if (Math.Abs(vc) < Zero)
        {
            vc = 0.0;
        }

        if (Math.Abs(vs) < Zero)
        {
            vs = 0.0;
        }

        double relativeVelocitySquared = vc * vc + vs * vs;
        if (relativeVelocitySquared == 0.0)
        {
            return (0.0, distance);
        }

        return (
            distance * vc / relativeVelocitySquared * 60.0,
            distance * Math.Abs(vs) / Math.Sqrt(relativeVelocitySquared));
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

    private static double NormalizeLegacyLoop(double degrees)
    {
        while (degrees < 0.0)
        {
            degrees += 360.0;
        }

        while (degrees > 360.0)
        {
            degrees -= 360.0;
        }

        return degrees;
    }

    private static (string Value, char Hemisphere) FormatPs2404aLatitude(double latitude)
    {
        return (FormatLegacyLongitude(Math.Abs(latitude)), latitude < 0.0 ? 'S' : 'N');
    }

    private static (string Value, char Hemisphere) FormatPs2404aLongitude(double longitude)
    {
        return (FormatLegacyLatitude(Math.Abs(longitude)), longitude < 0.0 ? 'W' : 'E');
    }

    private static (string Value, char Hemisphere) FormatPs2404aDtmLatitude(double latitude)
    {
        return FormatPs2404aDtmPosition(latitude, latitude < 0.0 ? 'S' : 'N', degreeDigits: 0);
    }

    private static (string Value, char Hemisphere) FormatPs2404aDtmLongitude(double longitude)
    {
        return FormatPs2404aDtmPosition(longitude, longitude < 0.0 ? 'W' : 'E', degreeDigits: 3);
    }

    private static (string Value, char Hemisphere) FormatPs2404aDtmPosition(double value, char hemisphere, int degreeDigits)
    {
        double absolute = Math.Abs(value);
        int degrees = (int)absolute;
        double minutes = (absolute - degrees) * 60.0;
        string degreeText = degreeDigits > 0
            ? degrees.ToString(new string('0', degreeDigits), Invariant)
            : degrees.ToString(Invariant);

        return ($"{degreeText}{minutes.ToString("00.00000", Invariant)}", hemisphere);
    }

    private static (string Value, char Hemisphere) FormatPs2404aGpdtmLatitude(double latitude)
    {
        return (FormatLegacyLatitude(Math.Abs(latitude)), latitude < 0.0 ? 'S' : 'N');
    }

    private static (string Value, char Hemisphere) FormatPs2404aGpdtmLongitude(double longitude)
    {
        return (FormatLegacyLongitude(Math.Abs(longitude)), longitude < 0.0 ? 'W' : 'E');
    }

    private static string FormatLegacyLatitude(double value)
    {
        return FormatLegacyPosition(value, degreeDigits: 3);
    }

    private static string FormatLegacyLongitude(double value)
    {
        return FormatLegacyPosition(value, degreeDigits: 2);
    }

    private static string FormatLegacyPosition(double value, int degreeDigits)
    {
        int degrees = (int)value;
        double minutes = (value - degrees) * 60.0;
        string degreeText = FormatLegacyInteger(degrees, degreeDigits);
        return $"{degreeText}{minutes.ToString("00.00000", Invariant)}";
    }

    private static string FormatLegacyInteger(int value, int width)
    {
        if (value >= 0)
        {
            return value.ToString(new string('0', width), Invariant);
        }

        int digitWidth = Math.Max(1, width - 1);
        return "-" + Math.Abs(value).ToString(new string('0', digitWidth), Invariant);
    }

    private sealed record Ps2404aVbwValues(
        double LongitudinalKnots,
        double LateralKnots,
        double WaterLongitudinalKnots,
        double WaterLateralKnots,
        double SternWaterSpeedKnots,
        double SternGroundSpeedKnots);
}
