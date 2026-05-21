using System.Globalization;
using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services.Projects;

public abstract class BaseProjectNmeaSentenceBuilder : IProjectNmeaSentenceBuilder
{
    protected static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    protected const double CppKnotsDivisor = 0.515;

    private static readonly NmeaTalkerProfile FallbackTalkerProfile = new(
        GenericTalkerId: "--",
        VbwTalkerId: "--",
        GnssTalkerId: "--",
        HeadingTalkerId: "--",
        CompassTalkerId: "--",
        AisTalkerId: "--");

    public abstract ProjectType ProjectType { get; }

    protected virtual NmeaTalkerProfile TalkerProfile => FallbackTalkerProfile;

    public IReadOnlyList<string> Build(NmeaSentenceId id, NmeaDataDto data, NmeaBuildOptions options)
    {
        data.Time = data.Time == default ? DateTime.Now : data.Time;
        NmeaDerivedData derived = NmeaDerivedData.From(data);
        IReadOnlyList<string> rawSentences = BuildRawSentences(id, data, derived, options);

        return ApplyTalkerProfile(rawSentences, id, TalkerProfile, options.UseHdmOutput);
    }

    public byte Checksum(string body)
    {
        return ComputeChecksum(body);
    }

    public virtual string BuildVtgSentence(
        double gyroHeading,
        double magneticVariation,
        double waterSpeedKnots,
        double waterSpeedKmh,
        NmeaBuildOptions options)
    {
        double magneticHeading = NormalizeDegrees(gyroHeading + magneticVariation);
        string rawSentence = Full(string.Create(
            Invariant,
            $"GPVTG,{gyroHeading:0.0},T,{magneticHeading:0.0},M,{waterSpeedKnots:0.0},N,{waterSpeedKmh:0.0},K"));

        return ApplyTalkerProfile(One(rawSentence), NmeaSentenceId.Vtg, TalkerProfile, options.UseHdmOutput)[0];
    }

    protected virtual IReadOnlyList<string> BuildRawSentences(
        NmeaSentenceId id,
        NmeaDataDto data,
        NmeaDerivedData derived,
        NmeaBuildOptions options)
    {
        return id switch
        {
            NmeaSentenceId.STR => One(BuildStr(data)),
            NmeaSentenceId.Gga => One(BuildGga(data)),
            NmeaSentenceId.Gll => One(BuildGll(data)),
            NmeaSentenceId.Rmc => One(BuildRmc(data, derived)),
            NmeaSentenceId.Vtg => One(BuildVtg(data, derived)),
            NmeaSentenceId.Zda => One(BuildZda(data)),
            NmeaSentenceId.Hdt => One(BuildHdt(data)),
            NmeaSentenceId.Vbw => One(BuildVbw(derived)),
            NmeaSentenceId.Rot => One(BuildRot(data)),
            NmeaSentenceId.Rsa => One(BuildRsa(data)),
            NmeaSentenceId.RpmPort => One(BuildRpmPort(data)),
            NmeaSentenceId.RpmStbd => One(BuildRpmStbd(data)),
            NmeaSentenceId.Mwv => One(BuildMwv(data, options.TrueWind)),
            NmeaSentenceId.Hdg => One(BuildHdg(data, options.UseHdmOutput)),
            NmeaSentenceId.Dpt => One(BuildDpt(data)),
            NmeaSentenceId.Dbt => One(BuildDbt(data)),
            NmeaSentenceId.Etl => BuildEtl(data),
            NmeaSentenceId.Cur => One(BuildCur(data)),
            NmeaSentenceId.Mda => One(BuildMda(data)),
            NmeaSentenceId.Trc => BuildTrc(data),
            NmeaSentenceId.Trd => BuildTrd(data),
            NmeaSentenceId.Hpm => One(BuildHpm(data)),
            NmeaSentenceId.Hrm => One(BuildHrm(data)),
            NmeaSentenceId.Vdm => BuildVdm(data),
            NmeaSentenceId.Vdo => One(BuildVdo(data)),
            _ => Array.Empty<string>()
        };
    }

    protected static string BuildStr(NmeaDataDto data)
    {
        string simulationTimeText = ParseSimulationTimeToText(data);
        string body = string.Create(Invariant, $"--STR,{simulationTimeText},{data.WaveDirection:0.0},{data.WaveHeight:0.0}");
        return Full(body);
    }

    protected static string BuildGga(NmeaDataDto data)
    {
        var lat = FormatLatitude(data.Latitude);
        var lon = FormatLongitude(data.Longitude);
        string body = string.Create(Invariant, $"GPGGA,{TimeOfDay(data.Time, true)},{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},1,05,02.5,,M,,M,,");
        return Full(body);
    }

    protected static string BuildGll(NmeaDataDto data)
    {
        var lat = FormatLatitude(data.Latitude);
        var lon = FormatLongitude(data.Longitude);
        string body = string.Create(Invariant, $"GPGLL,{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},{TimeOfDay(data.Time, true)},A");
        return Full(body);
    }

    protected static string BuildRmc(NmeaDataDto data, NmeaDerivedData derived)
    {
        var lat = FormatLatitude(data.Latitude);
        var lon = FormatLongitude(data.Longitude);
        string body = string.Create(
            Invariant,
            $"GPRMC,{TimeOfDay(data.Time, true)},A,{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},{derived.SpeedOverGroundKnots:00.0},{derived.CourseOverGround:000.0},{data.Time:ddMMyy},,");
        return Full(body);
    }

    protected static string BuildVtg(NmeaDataDto data, NmeaDerivedData derived)
    {
        string body = string.Create(
            Invariant,
            $"GPVTG,{data.GyroHeading:0.0},T,{derived.MagneticHeading:0.0},M,{derived.WaterSpeedKnots:0.0},N,{derived.WaterSpeedKmh:0.0},K");
        return Full(body);
    }

    protected static string BuildZda(NmeaDataDto data)
    {
        string body = string.Create(Invariant, $"GPZDA,{data.Time:HHmmss},{data.Time:dd},{data.Time:MM},{data.Time:yyyy},-9,00");
        return Full(body);
    }

    protected static string BuildHdt(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"HEHDT,{data.GyroHeading:000.0},T"));
    }

    protected static string BuildVbw(NmeaDerivedData derived)
    {
        string body = string.Create(
            Invariant,
            $"--VBW,{derived.WaterLongitudinalKnots:0.0},{derived.WaterLateralKnots:0.0},A,{derived.LongitudinalKnots:0.0},{derived.LateralKnots:0.0},A");
        return Full(body);
    }

    protected static string BuildRot(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--ROT,{data.OwnTurningRate * 60.0:0.0},A"));
    }

    protected static string BuildRsa(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--RSA,{data.RudderStbd * -1.0:0.0},A,{data.RudderPort * -1.0:0.0},A"));
    }

    protected static string BuildRpmPort(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--RPM,E,0.0,{data.RpmPort:0.0},{data.PitchPort:0.0},A"));
    }

    protected static string BuildRpmStbd(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--RPM,E,1.0,{data.RpmStbd:0.0},{data.PitchStbd:0.0},A"));
    }

    protected static string BuildMwv(NmeaDataDto data, bool trueWind)
    {
        if (trueWind)
        {
            return Full(string.Create(Invariant, $"--MWV,{data.WindDirection:0.0},T,{data.WindSpeedMps * 1.94384449:0.0},N,A"));
        }

        return Full(string.Create(Invariant, $"--MWV,{data.WindRelativeDirection:0.0},R,{data.WindRelativeSpeedMps * 1.94384449:0.0},N,A"));
    }

    protected static string BuildHdg(NmeaDataDto data, bool useHdmOutput)
    {
        double magnetic = NormalizeDegrees(data.Heading + data.MagneticVariation);
        if (useHdmOutput)
        {
            return Full(string.Create(Invariant, $"--HDM,{magnetic:0},M"));
        }

        return Full(string.Create(Invariant, $"HCHDG,{magnetic:0.0},0.0,E,0.0,E"));
    }

    protected static string BuildDpt(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--DPT,{data.WaterDepth:0.0},{data.WaterDepth - data.OwnshipDraft:0.0},,"));
    }

    protected static string BuildDbt(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--DBT,{data.WaterDepth * 3.2808:0.0},f,{data.WaterDepth:0.0},M,{data.WaterDepth * 0.5468:0.0},F"));
    }

    protected static IReadOnlyList<string> BuildEtl(NmeaDataDto data)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new[]
        {
            Full(string.Create(Invariant, $"--ETL,{utcNow:HHmmss}.00,O,{Telegraph(data.EngineCommandPort):00},30,B,0")),
            Full(string.Create(Invariant, $"--ETL,{utcNow:HHmmss}.00,O,{Telegraph(data.EngineCommandStbd):00},30,B,1"))
        };
    }

    protected static string BuildCur(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--CUR,A,0,1.0,{data.HeightTide:0.0},{data.CurrentSet:0.0},T,{data.CurrentDrift * 3600.0 / NmeaConstants.NauticalMileMeters:0.0},1.0,{data.WaterDepth:0.0},T,P"));
    }

    protected static string BuildMda(NmeaDataDto data)
    {
        var (water, air, humidity) = MonthlyWeather(data.Time.Month);
        double windKnots = data.WindSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
        string body = string.Create(
            Invariant,
            $"--MDA,412.1,I,1.0,B,{air:0.0},C,{water:0.0},C,{humidity:0.0},2.3,55.0,C,{data.WindDirection:0.0},T,{data.WindRelativeDirection:0.0},M,{windKnots:0.0},N,{data.WindSpeedMps:0.0},M");
        return Full(body);
    }

    protected static IReadOnlyList<string> BuildTrc(NmeaDataDto data)
    {
        return new[]
        {
            Full(string.Create(Invariant, $"--TRC,1,{data.ThrustCommandBow * 200.0:0.0},P,100.0,P,,B,R")),
            Full(string.Create(Invariant, $"--TRC,0,{data.ThrustCommandStern * 200.0:0.0},P,100.0,P,,B,R"))
        };
    }

    protected static IReadOnlyList<string> BuildTrd(NmeaDataDto data)
    {
        return new[]
        {
            Full(string.Create(Invariant, $"--TRD,1,{data.ThrusterThrustBow * 200.0:0.0},P,100,P,,")),
            Full(string.Create(Invariant, $"--TRD,0,{data.ThrusterThrustStern * 200.0:0.0},P,100,P,,"))
        };
    }

    protected static string BuildHpm(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--HPM,{data.OwnshipPitch:0.0},0.5,{data.OwnshipPitch:0.0},,A,,,,,,,,C"));
    }

    protected static string BuildHrm(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--HRM,{data.OwnshipRoll:0.0},0.5,{data.OwnshipRoll:0.0},{data.OwnshipRoll:0.0},A,,,,,"));
    }

    protected static IReadOnlyList<string> BuildVdm(NmeaDataDto data)
    {
        if (!data.UsesTrafficShipData)
        {
            return Array.Empty<string>();
        }

        List<string> sentences = new();
        foreach (TrafficShipData ship in data.TrafficShips)
        {
            if (!ship.IsAisEnabled || ship.Mmsi <= 0 || !IsValidCoordinate(ship.Latitude, ship.Longitude))
            {
                continue;
            }

            double speedOverGround = Math.Sqrt(ship.LongitudinalSpeedMps * ship.LongitudinalSpeedMps + ship.LateralSpeedMps * ship.LateralSpeedMps) / CppKnotsDivisor;
            sentences.Add(AisPosition("AIVDM", ship.Mmsi, ship.Latitude, ship.Longitude, speedOverGround, ship.CourseOverGround, ship.Heading, data.Time));
            sentences.Add(AisStatic("AIVDM", ship));
        }

        return sentences;
    }

    protected static string BuildVdo(NmeaDataDto data)
    {
        double courseOverGround = NormalizeDegrees(data.Heading);
        double speedOverGround = Math.Sqrt(data.LongitudinalSpeedMps * data.LongitudinalSpeedMps + data.LateralSpeedMps * data.LateralSpeedMps) / CppKnotsDivisor;
        double headingLateral = -data.CurrentDrift * Math.Sin((data.CurrentSet - courseOverGround) * NmeaConstants.ToRadians);
        double trueHeading = NormalizeDegrees(Math.Atan2(headingLateral, data.LongitudinalSpeedMps) * NmeaConstants.ToDegrees + courseOverGround);
        return AisPosition("AIVDO", data.Mmsi, data.OwnLatitude, data.OwnLongitude, speedOverGround, courseOverGround, trueHeading, data.Time);
    }

    protected static string ParseSimulationTimeToText(NmeaDataDto data)
    {
        if (TryFromUnixSeconds(data.SimulationTimeSeconds, out DateTime simulationTime))
        {
            return simulationTime.ToString("yyyy-MM-dd HH:mm:ss", Invariant);
        }

        DateTime fallback = data.Time == default ? DateTime.Now : data.Time;
        return fallback.ToString("yyyy-MM-dd HH:mm:ss", Invariant);
    }

    protected static bool TryFromUnixSeconds(double seconds, out DateTime localDateTime)
    {
        localDateTime = default;
        if (!double.IsFinite(seconds) || seconds <= 0.0)
        {
            return false;
        }

        try
        {
            long unixSeconds = (long)(seconds + 0.001);
            localDateTime = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime;
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    protected static string AisPosition(
        string talker,
        int mmsi,
        double latitude,
        double longitude,
        double speedOverGround,
        double courseOverGround,
        double heading,
        DateTime time)
    {
        string payload = AisPayloadBuilder.BuildPositionPayload(mmsi, latitude, longitude, speedOverGround, courseOverGround, heading, time);
        return Full($"{talker},1,1,,A,{payload},0", ais: true);
    }

    protected static string AisStatic(string talker, TrafficShipData ship)
    {
        string payload = AisPayloadBuilder.BuildStaticPayload(ship);
        string sequence = ship.SharedIndex >= 0 ? ((ship.SharedIndex + 1) % 10).ToString(Invariant) : "0";
        return Full($"{talker},1,1,{sequence},A,{payload},0", ais: true);
    }

    protected static int Telegraph(double command)
    {
        command = Math.Clamp(command, -1.0, 1.0);
        if (command == 0.0)
        {
            return 0;
        }

        if (command > 0.0)
        {
            return command <= 0.2 ? 1 :
                command <= 0.4 ? 2 :
                command <= 0.6 ? 3 :
                command <= 0.8 ? 4 : 5;
        }

        return command >= -0.2 ? 11 :
            command >= -0.4 ? 12 :
            command >= -0.6 ? 13 :
            command >= -0.8 ? 14 : 15;
    }

    protected static string Full(string body, bool ais = false)
    {
        string prefix = ais ? "!" : "$";
        return string.Create(Invariant, $"{prefix}{body}*{ComputeChecksum(body):X2}\r\n");
    }

    protected static IReadOnlyList<string> One(string value)
    {
        return new[] { value };
    }

    protected static string TimeOfDay(DateTime time, bool includeFractions)
    {
        return includeFractions
            ? string.Create(Invariant, $"{time:HHmmss}.00")
            : string.Create(Invariant, $"{time:HHmmss}");
    }

    protected static (string Value, char Hemisphere) FormatLatitude(double latitude)
    {
        return FormatPosition(latitude, latitude < 0.0 ? 'S' : 'N');
    }

    protected static (string Value, char Hemisphere) FormatLongitude(double longitude)
    {
        return FormatPosition(longitude, longitude < 0.0 ? 'W' : 'E');
    }

    protected static (string Value, char Hemisphere) FormatPosition(double degrees, char hemisphere)
    {
        return FormatPositionWithDigits(degrees, hemisphere, degreeDigits: 0, minuteDigits: 4);
    }

    protected static (string Value, char Hemisphere) FormatPositionWithDigits(
        double degrees,
        char hemisphere,
        int degreeDigits,
        int minuteDigits)
    {
        double absolute = Math.Abs(degrees);
        int wholeDegrees = (int)absolute;
        double minutes = (absolute - wholeDegrees) * 60.0;

        string degreeFormat = degreeDigits > 0
            ? new string('0', degreeDigits)
            : "0";
        string minuteFormat = "00." + new string('0', Math.Max(0, minuteDigits));

        return ($"{wholeDegrees.ToString(degreeFormat, Invariant)}{minutes.ToString(minuteFormat, Invariant)}", hemisphere);
    }

    protected static double NormalizeDegrees(double degrees)
    {
        degrees %= 360.0;
        return degrees < 0.0 ? degrees + 360.0 : degrees;
    }

    protected static (double Water, double Air, double Humidity) MonthlyWeather(int month)
    {
        return month switch
        {
            1 => (6.7, 3.8, 50.2),
            2 => (5.3, 3.3, 62.6),
            3 => (5.1, 2.9, 69.8),
            4 => (7.1, 8.7, 72.2),
            5 => (10.1, 14.2, 69.2),
            6 => (16.3, 18.5, 73.7),
            7 => (20.7, 24.3, 82.4),
            8 => (25.4, 27.1, 71.6),
            9 => (23.0, 24.9, 70.6),
            10 => (20.0, 17.2, 68.9),
            11 => (17.3, 14.5, 52.3),
            12 => (12.8, 9.4, 49.6),
            _ => (6.0, 8.0, 55.0)
        };
    }

    protected static bool IsValidCoordinate(double latitude, double longitude)
    {
        return double.IsFinite(latitude) &&
               double.IsFinite(longitude) &&
               Math.Abs(latitude) <= 90.0 &&
               Math.Abs(longitude) <= 180.0;
    }

    protected static double EstimateDistanceNm(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = (lat2 - lat1) * NmeaConstants.ToRadians;
        double dLon = (lon2 - lon1) * NmeaConstants.ToRadians;
        double sinLat = Math.Sin(dLat / 2.0);
        double sinLon = Math.Sin(dLon / 2.0);
        double a = sinLat * sinLat +
                   Math.Cos(lat1 * NmeaConstants.ToRadians) *
                   Math.Cos(lat2 * NmeaConstants.ToRadians) *
                   sinLon * sinLon;
        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(Math.Max(0.0, 1.0 - a)));
        return (6371000.0 * c) / NmeaConstants.NauticalMileMeters;
    }

    protected static double EstimateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        double phi1 = lat1 * NmeaConstants.ToRadians;
        double phi2 = lat2 * NmeaConstants.ToRadians;
        double dLon = (lon2 - lon1) * NmeaConstants.ToRadians;

        double y = Math.Sin(dLon) * Math.Cos(phi2);
        double x = Math.Cos(phi1) * Math.Sin(phi2) - Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(dLon);
        return NormalizeDegrees(Math.Atan2(y, x) * NmeaConstants.ToDegrees);
    }

    protected static byte ComputeChecksum(string body)
    {
        byte checksum = 0;
        foreach (char ch in body)
        {
            checksum ^= (byte)ch;
        }

        return checksum;
    }

    protected static IReadOnlyList<string> ApplyTalkerProfile(
        IReadOnlyList<string> sentences,
        NmeaSentenceId sentenceId,
        NmeaTalkerProfile talkerProfile,
        bool useHdmOutput)
    {
        if (sentences.Count == 0)
        {
            return sentences;
        }

        string targetTalkerId = NormalizeTalkerId(talkerProfile.ResolveTalkerId(sentenceId, useHdmOutput));
        if (targetTalkerId == "--")
        {
            return sentences;
        }

        return sentences
            .Select(sentence => ReplaceTalkerId(sentence, targetTalkerId))
            .ToArray();
    }

    private static string ReplaceTalkerId(string sentence, string talkerId)
    {
        if (string.IsNullOrWhiteSpace(sentence) || talkerId.Length != 2)
        {
            return sentence;
        }

        int start = sentence[0] is '$' or '!' ? 1 : 0;
        int commaIndex = sentence.IndexOf(',', start);
        int starIndex = sentence.IndexOf('*', start);
        if (starIndex < 0)
        {
            return sentence;
        }

        int tokenEnd = commaIndex >= 0 ? commaIndex : starIndex;
        if (tokenEnd <= start || tokenEnd - start < 5 || starIndex <= tokenEnd)
        {
            return sentence;
        }

        string token = sentence.Substring(start, tokenEnd - start);
        string formatter = token.Length >= 3 ? token.Substring(2) : token;
        if (formatter.Length != 3)
        {
            return sentence;
        }

        string updatedBody = $"{talkerId}{formatter}{sentence.Substring(tokenEnd, starIndex - tokenEnd)}";
        char prefix = start == 1 ? sentence[0] : '$';
        return string.Create(Invariant, $"{prefix}{updatedBody}*{ComputeChecksum(updatedBody):X2}\r\n");
    }

    private static string NormalizeTalkerId(string talkerId)
    {
        if (string.IsNullOrWhiteSpace(talkerId))
        {
            return "--";
        }

        string normalized = talkerId.Trim().ToUpperInvariant();
        return normalized.Length >= 2 ? normalized[..2] : "--";
    }
}
