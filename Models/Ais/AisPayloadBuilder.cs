using NMEASender.Wpf.Models.Core;

namespace NMEASender.Wpf.Models.Ais;

public static class AisPayloadBuilder
{
    public static string BuildPositionPayload(int mmsi, double latitude, double longitude, double sogKnots, double cog, double heading, DateTime time)
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

    public static string BuildStaticPayload(TrafficShipData ship)
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

    private static double NormalizeDegrees(double degrees)
    {
        degrees %= 360.0;
        return degrees < 0.0 ? degrees + 360.0 : degrees;
    }
}
