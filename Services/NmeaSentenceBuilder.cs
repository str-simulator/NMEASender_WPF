using System.Globalization;
using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services;

public sealed record NmeaBuildOptions(bool TrueWind, bool UseHdmOutput = true);

public static class NmeaSentenceBuilder
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private const double CppKnotsDivisor = 0.515;

    public static IReadOnlyList<string> Build(NmeaSentenceId id, NmeaDataDto data, NmeaBuildOptions options)
    {
        data.Time = data.Time == default ? DateTime.Now : data.Time;
        DerivedData derived = DerivedData.From(data);

        return id switch
        {
            NmeaSentenceId.STR => One(STR(data)),
            NmeaSentenceId.Gga => One(Gga(data)),
            NmeaSentenceId.Gll => One(Gll(data)),
            NmeaSentenceId.Rmc => One(Rmc(data, derived)),
            NmeaSentenceId.Vtg => One(Vtg(data, derived)),
            NmeaSentenceId.Zda => One(Zda(data)),
            NmeaSentenceId.Hdt => One(Hdt(data)),
            NmeaSentenceId.Vbw => One(Vbw(derived)),
            NmeaSentenceId.Rot => One(Rot(data)),
            NmeaSentenceId.Rsa => One(Rsa(data)),
            NmeaSentenceId.RpmPort => One(RpmPort(data)),
            NmeaSentenceId.RpmStbd => One(RpmStbd(data)),
            NmeaSentenceId.Mwv => One(Mwv(data, options)),
            NmeaSentenceId.Hdg => One(Hdg(data, options)),
            NmeaSentenceId.Dpt => One(Dpt(data)),
            NmeaSentenceId.Dbt => One(Dbt(data)),
            NmeaSentenceId.Etl => Etl(data),
            NmeaSentenceId.Cur => One(Cur(data)),
            NmeaSentenceId.Mda => One(Mda(data)),
            NmeaSentenceId.Trc => Trc(data),
            NmeaSentenceId.Trd => Trd(data),
            NmeaSentenceId.Hpm => One(Hpm(data)),
            NmeaSentenceId.Hrm => One(Hrm(data)),
            NmeaSentenceId.Vdm => Vdm(data),
            NmeaSentenceId.Vdo => One(Vdo(data)),
            _ => Array.Empty<string>()
        };
    }

    public static byte Checksum(string body)
    {
        byte checksum = 0;
        foreach (char ch in body)
        {
            checksum ^= (byte)ch;
        }

        return checksum;
    }

    public static string BuildVtgSentence(double gyroHeading, double magneticVariation, double waterSpeedKnots, double waterSpeedKmh)
    {
        double magneticHeading = NormalizeDegrees(gyroHeading + magneticVariation);
        string body = string.Create(
            Invariant,
            $"GPVTG,{gyroHeading:0.0},T,{magneticHeading:0.0},M,{waterSpeedKnots:0.0},N,{waterSpeedKmh:0.0},K");
        return Full(body);
    }

    private static string STR(NmeaDataDto data)
    {
        string simulationTimeText = ParseSimulationTimeToText(data);
        string body = string.Create(Invariant, $"--STR,{simulationTimeText},{data.WaveDirection:0.0},{data.WaveHeight:0.0}");
        return Full(body);
    }

    private static string ParseSimulationTimeToText(NmeaDataDto data)
    {
        if (TryFromUnixSeconds(data.SimulationTimeSeconds, out DateTime simulationTime))
        {
            return simulationTime.ToString("yyyy-MM-dd HH:mm:ss", Invariant);
        }

        DateTime fallback = data.Time == default ? DateTime.Now : data.Time;
        return fallback.ToString("yyyy-MM-dd HH:mm:ss", Invariant);
    }

    private static bool TryFromUnixSeconds(double seconds, out DateTime localDateTime)
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

    private static string Gga(NmeaDataDto data)
    {
        var lat = FormatLatitude(data.Latitude);
        var lon = FormatLongitude(data.Longitude);
        string? body = string.Create(Invariant, $"GPGGA,{TimeOfDay(data.Time, true)},{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},1,05,02.5,,M,,M,,");
        return Full(body);
    }

    private static string Gll(NmeaDataDto data)
    {
        var lat = FormatLatitude(data.Latitude);
        var lon = FormatLongitude(data.Longitude);
        string? body = string.Create(Invariant, $"GPGLL,{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},{TimeOfDay(data.Time, true)},A");
        return Full(body);
    }

    private static string Rmc(NmeaDataDto data, DerivedData derived)
    {
        var lat = FormatLatitude(data.Latitude);
        var lon = FormatLongitude(data.Longitude);
        string? body = string.Create(
            Invariant,
            $"GPRMC,{TimeOfDay(data.Time, true)},A,{lat.Value},{lat.Hemisphere},{lon.Value},{lon.Hemisphere},{derived.SpeedOverGroundKnots:00.0},{derived.CourseOverGround:000.0},{data.Time:ddMMyy},,");
        return Full(body);
    }

    private static string Vtg(NmeaDataDto data, DerivedData derived)
    {
        string? body = string.Create(
            Invariant,
            $"GPVTG,{data.GyroHeading:0.0},T,{derived.MagneticHeading:0.0},M,{derived.WaterSpeedKnots:0.0},N,{derived.WaterSpeedKmh:0.0},K");
        return Full(body);
    }

    private static string Zda(NmeaDataDto data)
    {
        string? body = string.Create(Invariant, $"GPZDA,{data.Time:HHmmss},{data.Time:dd},{data.Time:MM},{data.Time:yyyy},-9,00");
        return Full(body);
    }

    private static string Hdt(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"HEHDT,{data.GyroHeading:000.0},T"));
    }

    private static string Vbw(DerivedData derived)
    {
        string? body = string.Create(
            Invariant,
            $"--VBW,{derived.WaterLongitudinalKnots:0.0},{derived.WaterLateralKnots:0.0},V,{derived.LongitudinalKnots:0.0},{derived.LateralKnots:0.0},A");
        return Full(body);
    }

    private static string Rot(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--ROT,{data.OwnTurningRate * 60.0:0.0},A"));
    }

    private static string Rsa(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--RSA,{data.RudderStbd * -1.0:0.0},A,{data.RudderPort * -1.0:0.0},A"));
    }

    private static string RpmPort(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--RPM,E,0.0,{data.RpmPort:0.0},{data.PitchPort:0.0},A"));
    }

    private static string RpmStbd(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--RPM,E,1.0,{data.RpmStbd:0.0},{data.PitchStbd:0.0},A"));
    }

    private static string Mwv(NmeaDataDto data, NmeaBuildOptions options)
    {
        if (options.TrueWind)
        {
            return Full(string.Create(Invariant, $"--MWV,{data.WindDirection:0.0},T,{data.WindSpeedMps * 1.94384449:0.0},N,A"));
        }

        return Full(string.Create(Invariant, $"--MWV,{data.WindRelativeDirection:0.0},R,{data.WindRelativeSpeedMps * 1.94384449:0.0},N,A"));
    }

    private static string Hdg(NmeaDataDto data, NmeaBuildOptions options)
    {
        double magnetic = NormalizeDegrees(data.Heading + data.MagneticVariation);
        if (options.UseHdmOutput)
        {
            return Full(string.Create(Invariant, $"--HDM,{magnetic:0},M"));
        }

        return Full(string.Create(Invariant, $"HCHDG,{magnetic:0.0},0.0,E,0.0,E"));
    }

    private static string Dbt(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--DBT,{data.WaterDepth * 3.2808:0.0},f,{data.WaterDepth:0.0},M,{data.WaterDepth * 0.5468:0.0},F"));
    }

    private static string Dpt(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--DPT,{data.WaterDepth:0.0},{data.WaterDepth - data.OwnshipDraft:0.0},,"));
    }

    private static IReadOnlyList<string> Etl(NmeaDataDto data)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new[]
        {
            Full(string.Create(Invariant, $"--ETL,{utcNow:HHmmss}.00,O,{Telegraph(data.EngineCommandPort):00},30,B,0")),
            Full(string.Create(Invariant, $"--ETL,{utcNow:HHmmss}.00,O,{Telegraph(data.EngineCommandStbd):00},30,B,1"))
        };
    }

    private static string Cur(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--CUR,A,0,1.0,{data.HeightTide:0.0},{data.CurrentSet:0.0},T,{data.CurrentDrift * 3600.0 / NmeaConstants.NauticalMileMeters:0.0},1.0,{data.WaterDepth:0.0},T,P"));
    }

    private static string Mda(NmeaDataDto data)
    {
        var (water, air, humidity) = MonthlyWeather(data.Time.Month);
        double windKnots = data.WindSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
        string? body = string.Create(
            Invariant,
            $"--MDA,412.1,I,1.0,B,{air:0.0},C,{water:0.0},C,{humidity:0.0},2.3,55.0,C,{data.WindDirection:0.0},T,{data.WindRelativeDirection:0.0},M,{windKnots:0.0},N,{data.WindSpeedMps:0.0},M");
        return Full(body);
    }

    private static IReadOnlyList<string> Trc(NmeaDataDto data)
    {
        return new[]
        {
            Full(string.Create(Invariant, $"--TRC,1,{data.ThrustCommandBow * 200.0:0.0},P,100.0,P,,B,R")),
            Full(string.Create(Invariant, $"--TRC,0,{data.ThrustCommandStern * 200.0:0.0},P,100.0,P,,B,R"))
        };
    }

    private static IReadOnlyList<string> Trd(NmeaDataDto data)
    {
        return new[]
        {
            Full(string.Create(Invariant, $"--TRD,1,{data.ThrusterThrustBow * 200.0:0.0},P,100,P,,")),
            Full(string.Create(Invariant, $"--TRD,0,{data.ThrusterThrustStern * 200.0:0.0},P,100,P,,"))
        };
    }

    private static string Hpm(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--HPM,{data.OwnshipPitch:0.0},0.5,{data.OwnshipPitch:0.0},,A,,,,,,,,C"));
    }

    private static string Hrm(NmeaDataDto data)
    {
        return Full(string.Create(Invariant, $"--HRM,{data.OwnshipRoll:0.0},0.5,{data.OwnshipRoll:0.0},{data.OwnshipRoll:0.0},A,,,,,"));
    }

    private static IReadOnlyList<string> Vdm(NmeaDataDto data)
    {
        if (!data.UsesTrafficShipData)
        {
            return Array.Empty<string>();
        }

        List<string> sentences = new List<string>();
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

    private static string Vdo(NmeaDataDto data)
    {
        double courseOverGround = NormalizeDegrees(data.Heading);
        double speedOverGround = Math.Sqrt(data.LongitudinalSpeedMps * data.LongitudinalSpeedMps + data.LateralSpeedMps * data.LateralSpeedMps) / CppKnotsDivisor;
        double headingLateral = -data.CurrentDrift * Math.Sin((data.CurrentSet - courseOverGround) * NmeaConstants.ToRadians);
        double trueHeading = NormalizeDegrees(Math.Atan2(headingLateral, data.LongitudinalSpeedMps) * NmeaConstants.ToDegrees + courseOverGround);
        return AisPosition("AIVDO", data.Mmsi, data.OwnLatitude, data.OwnLongitude, speedOverGround, courseOverGround, trueHeading, data.Time);
    }

    private static string AisPosition(string talker, int mmsi, double latitude, double longitude, double speedOverGround, double courseOverGround, double heading, DateTime time)
    {
        string payload = BuildAisPositionPayload(mmsi, latitude, longitude, speedOverGround, courseOverGround, heading, time);
        return Full($"{talker},1,1,,A,{payload},0", ais: true);
    }

    private static string AisStatic(string talker, TrafficShipData ship)
    {
        string payload = BuildAisStaticPayload(ship);
        string sequence = ship.SharedIndex >= 0 ? ((ship.SharedIndex + 1) % 10).ToString(Invariant) : "0";
        return Full($"{talker},1,1,{sequence},A,{payload},0", ais: true);
    }

    private static int Telegraph(double command)
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

    private static string Full(string body, bool ais = false)
    {
        string prefix = ais ? "!" : "$";
        return string.Create(Invariant, $"{prefix}{body}*{Checksum(body):X2}\r\n");
    }

    private static IReadOnlyList<string> One(string value)
    {
        return new[] { value };
    }

    private static string TimeOfDay(DateTime time, bool includeFractions)
    {
        return includeFractions
            ? string.Create(Invariant, $"{time:HHmmss}.00")
            : string.Create(Invariant, $"{time:HHmmss}");
    }

    private static (string Value, char Hemisphere) FormatLatitude(double latitude)
    {
        return FormatPosition(latitude, latitude < 0.0 ? 'S' : 'N');
    }

    private static (string Value, char Hemisphere) FormatLongitude(double longitude)
    {
        return FormatPosition(longitude, longitude < 0.0 ? 'W' : 'E');
    }

    private static (string Value, char Hemisphere) FormatPosition(double degrees, char hemisphere)
    {
        double absolute = Math.Abs(degrees);
        int wholeDegrees = (int)absolute;
        double minutes = (absolute - wholeDegrees) * 60.0;
        return ($"{wholeDegrees.ToString(Invariant)}{minutes.ToString("00.0000", Invariant)}", hemisphere);
    }

    private static double NormalizeDegrees(double degrees)
    {
        degrees %= 360.0;
        return degrees < 0.0 ? degrees + 360.0 : degrees;
    }

    private static (double Water, double Air, double Humidity) MonthlyWeather(int month)
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

    private static string BuildAisPositionPayload(int mmsi, double latitude, double longitude, double sogKnots, double cog, double heading, DateTime time)
    {
        List<int> bits = new List<int>(168);
        AddUnsigned(bits, 1, 6);
        AddUnsigned(bits, 2, 2);
        AddUnsigned(bits, Math.Clamp(mmsi, 0, 999999999), 30);
        AddUnsigned(bits, 0, 4);
        AddUnsigned(bits, 127, 8);
        AddUnsigned(bits, (int)Math.Clamp(Math.Round(sogKnots * 10.0), 0.0, 1022.0), 10);
        AddUnsigned(bits, 0, 1);
        AddSigned(bits, (int)Math.Round(longitude * 600000.0), 28);
        AddSigned(bits, (int)Math.Round(latitude * 600000.0), 27);
        AddUnsigned(bits, (int)Math.Clamp(Math.Round(NormalizeDegrees(cog) * 10.0), 0.0, 3599.0), 12);
        AddUnsigned(bits, (int)Math.Clamp(Math.Round(NormalizeDegrees(heading)), 0.0, 359.0), 9);
        AddUnsigned(bits, 53, 6);
        AddUnsigned(bits, 0, 4);
        AddUnsigned(bits, 0, 1);
        AddUnsigned(bits, 0, 1);
        AddUnsigned(bits, 0, 2);
        AddUnsigned(bits, 1, 3);
        AddUnsigned(bits, Math.Clamp(time.Hour, 0, 31), 5);
        AddUnsigned(bits, 0, 3);
        AddUnsigned(bits, Math.Clamp(time.Minute, 0, 63), 6);

        return EncodeAisSixBit(bits);
    }

    private static string BuildAisStaticPayload(TrafficShipData ship)
    {
        List<int> bits = new List<int>(360);
        AddUnsigned(bits, 5, 6);
        AddUnsigned(bits, 0, 2);
        AddUnsigned(bits, Math.Clamp(ship.Mmsi, 0, 999999999), 30);
        AddUnsigned(bits, 0, 2);
        AddUnsigned(bits, Math.Max(0, ship.ImoNumber), 30);
        AddAisLegacyText(bits, ship.CallSign, 7, upperCase: false);
        AddAisLegacyText(bits, ship.ShipName, 20, upperCase: true);
        AddUnsigned(bits, 70, 8);
        AddUnsigned(bits, Math.Max(0, (int)(ship.Length / 2.0)), 9);
        AddUnsigned(bits, Math.Max(0, (int)(ship.Length / 2.0)), 9);
        AddUnsigned(bits, Math.Max(0, (int)(ship.Beam / 2.0)), 6);
        AddUnsigned(bits, Math.Max(0, (int)(ship.Beam / 2.0)), 6);
        AddUnsigned(bits, 1, 4);
        AddUnsigned(bits, 0, 4);
        AddUnsigned(bits, 0, 5);
        AddUnsigned(bits, 24, 5);
        AddUnsigned(bits, 60, 6);
        AddUnsigned(bits, Math.Max(0, ship.Draft), 8);
        AddAisLegacyText(bits, ship.Destination, 20, upperCase: false);

        return EncodeAisSixBit(bits);
    }

    private static void AddUnsigned(List<int> bits, long value, int width)
    {
        for (int bit = width - 1; bit >= 0; bit--)
        {
            bits.Add(((value >> bit) & 1) == 1 ? 1 : 0);
        }
    }

    private static void AddSigned(List<int> bits, long value, int width)
    {
        if (value < 0)
        {
            value = (1L << width) + value;
        }

        AddUnsigned(bits, value, width);
    }

    private static void AddAisLegacyText(List<int> bits, string value, int length, bool upperCase)
    {
        string source = upperCase ? (value ?? string.Empty).ToUpperInvariant() : value ?? string.Empty;
        for (int index = 0; index < length; index++)
        {
            char ch = index < source.Length ? source[index] : '\0';
            int code = ch <= 0xFF ? ch : '?';
            int sixBit = code >= 0x40 ? code - 0x40 : code;
            AddUnsigned(bits, sixBit & 0x3F, 6);
        }
    }

    private static string EncodeAisSixBit(IReadOnlyList<int> bits)
    {
        char[] chars = new char[bits.Count / 6];
        for (int index = 0; index < chars.Length; index++)
        {
            int value = 0;
            for (int bit = 0; bit < 6; bit++)
            {
                value = (value << 1) | bits[index * 6 + bit];
            }

            chars[index] = (char)(value < 40 ? value + 48 : value + 56);
        }

        return new string(chars);
    }

    private static bool IsValidCoordinate(double latitude, double longitude)
    {
        return double.IsFinite(latitude) &&
               double.IsFinite(longitude) &&
               Math.Abs(latitude) <= 90.0 &&
               Math.Abs(longitude) <= 180.0;
    }

    private sealed class DerivedData
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

        public static DerivedData From(NmeaDataDto data)
        {
            double course = NormalizeDegrees(Math.Atan2(data.LateralSpeedMps, data.LongitudinalSpeedMps) * NmeaConstants.ToDegrees + data.Heading);
            double longitudinalKnots = data.LongitudinalSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
            double lateralKnots = -data.LateralSpeedMps * 3600.0 / NmeaConstants.NauticalMileMeters;
            double speedOverGroundKnots = Math.Sqrt(longitudinalKnots * longitudinalKnots + lateralKnots * lateralKnots);
            double currentAngle = (data.Heading - data.CurrentSet) * NmeaConstants.ToRadians;
            double waterLongitudinal = data.LongitudinalSpeedMps - data.CurrentDrift * Math.Cos(currentAngle);
            double waterLateral = data.LateralSpeedMps - data.CurrentDrift * Math.Sin(currentAngle);

            return new DerivedData
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
    }
}

