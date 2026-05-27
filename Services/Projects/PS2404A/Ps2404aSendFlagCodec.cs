using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;

namespace NMEASender.Wpf.Services.Projects.PS2404A;

public sealed class PS2404ASendFlagCodec : BaseProjectSendFlagCodec
{
    public override ProjectType ProjectType => ProjectType.PS2404A;

    public override ulong DefaultRawSendFlag(ulong defaultRawValue)
    {
        return 255UL;
    }

    public override NmeaSendFlag Decode(ulong rawFlag)
    {
        NmeaSendFlag mapped = 0;

        Map(rawFlag, RawFlag.Rmc, NmeaSendFlag.Rmc, ref mapped);
        Map(rawFlag, RawFlag.Gga, NmeaSendFlag.Gga, ref mapped);
        Map(rawFlag, RawFlag.Ttm, NmeaSendFlag.Ttm, ref mapped);
        Map(rawFlag, RawFlag.Vtg, NmeaSendFlag.Vtg, ref mapped);
        Map(rawFlag, RawFlag.Zda, NmeaSendFlag.Zda, ref mapped);
        Map(rawFlag, RawFlag.Hdt, NmeaSendFlag.Hdt, ref mapped);
        Map(rawFlag, RawFlag.Gpvbw, NmeaSendFlag.Vbw, ref mapped);
        Map(rawFlag, RawFlag.Rot, NmeaSendFlag.Rot, ref mapped);
        Map(rawFlag, RawFlag.Rsa, NmeaSendFlag.Rsa, ref mapped);
        Map(rawFlag, RawFlag.Mwv, NmeaSendFlag.Mwv, ref mapped);
        Map(rawFlag, RawFlag.Hdg, NmeaSendFlag.Hdg, ref mapped);
        Map(rawFlag, RawFlag.Vdm, NmeaSendFlag.Vdm, ref mapped);
        Map(rawFlag, RawFlag.Vhw, NmeaSendFlag.Vhw, ref mapped);
        Map(rawFlag, RawFlag.Dpt, NmeaSendFlag.Dpt, ref mapped);
        Map(rawFlag, RawFlag.Dbt, NmeaSendFlag.Dbt, ref mapped);
        Map(rawFlag, RawFlag.Dtm, NmeaSendFlag.Dtm, ref mapped);
        Map(rawFlag, RawFlag.Gll, NmeaSendFlag.Gll, ref mapped);
        Map(rawFlag, RawFlag.Vdr, NmeaSendFlag.Vdr, ref mapped);
        Map(rawFlag, RawFlag.Ttd, NmeaSendFlag.Ttd, ref mapped);
        Map(rawFlag, RawFlag.Vdo, NmeaSendFlag.Vdo, ref mapped);
        Map(rawFlag, RawFlag.Ths, NmeaSendFlag.Ths, ref mapped);
        Map(rawFlag, RawFlag.Cur, NmeaSendFlag.Cur, ref mapped);
        Map(rawFlag, RawFlag.Mws, NmeaSendFlag.Mws, ref mapped);
        Map(rawFlag, RawFlag.Mwh, NmeaSendFlag.Mwh, ref mapped);
        Map(rawFlag, RawFlag.Gpdtm, NmeaSendFlag.Gpdtm, ref mapped);
        Map(rawFlag, RawFlag.Htd, NmeaSendFlag.Htd, ref mapped);
        Map(rawFlag, RawFlag.Vdvbw, NmeaSendFlag.Vdvbw, ref mapped);

        if ((rawFlag & RawFlag.Rpm) == RawFlag.Rpm)
        {
            mapped |= NmeaSendFlag.RpmPort | NmeaSendFlag.RpmStbd;
        }

        return mapped;
    }

    public override ulong Encode(NmeaSendFlag flag)
    {
        ulong rawFlag = 0;
        MapBack(flag, NmeaSendFlag.Rmc, RawFlag.Rmc, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Gga, RawFlag.Gga, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Ttm, RawFlag.Ttm, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Vtg, RawFlag.Vtg, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Zda, RawFlag.Zda, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Hdt, RawFlag.Hdt, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Vbw, RawFlag.Gpvbw, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Rot, RawFlag.Rot, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Rsa, RawFlag.Rsa, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Mwv, RawFlag.Mwv, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Hdg, RawFlag.Hdg, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Vdm, RawFlag.Vdm, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Vhw, RawFlag.Vhw, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Dpt, RawFlag.Dpt, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Dbt, RawFlag.Dbt, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Dtm, RawFlag.Dtm, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Gll, RawFlag.Gll, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Vdr, RawFlag.Vdr, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Ttd, RawFlag.Ttd, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Vdo, RawFlag.Vdo, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Ths, RawFlag.Ths, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Cur, RawFlag.Cur, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Mws, RawFlag.Mws, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Mwh, RawFlag.Mwh, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Gpdtm, RawFlag.Gpdtm, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Htd, RawFlag.Htd, ref rawFlag);
        MapBack(flag, NmeaSendFlag.Vdvbw, RawFlag.Vdvbw, ref rawFlag);

        if ((flag & (NmeaSendFlag.RpmPort | NmeaSendFlag.RpmStbd)) != 0)
        {
            rawFlag |= RawFlag.Rpm;
        }

        return rawFlag;
    }

    private static void Map(ulong source, ulong bit, NmeaSendFlag target, ref NmeaSendFlag result)
    {
        if ((source & bit) == bit)
        {
            result |= target;
        }
    }

    private static void MapBack(NmeaSendFlag source, NmeaSendFlag bit, ulong targetBit, ref ulong result)
    {
        if ((source & bit) == bit)
        {
            result |= targetBit;
        }
    }

    private static class RawFlag
    {
        public const ulong Rmc = 0x00000001;
        public const ulong Gga = 0x00000002;
        public const ulong Ttm = 0x00000004;
        public const ulong Vtg = 0x00000008;
        public const ulong Zda = 0x00000010;
        public const ulong Hdt = 0x00000020;
        public const ulong Gpvbw = 0x00000040;
        public const ulong Rot = 0x00000080;
        public const ulong Rsa = 0x00000100;
        public const ulong Rpm = 0x00000200;
        public const ulong Mwv = 0x00000400;
        public const ulong Hdg = 0x00000800;
        public const ulong Vdm = 0x00001000;
        public const ulong Vhw = 0x00002000;
        public const ulong Dpt = 0x00004000;
        public const ulong Dbt = 0x00008000;
        public const ulong Dtm = 0x00010000;
        public const ulong Gll = 0x00020000;
        public const ulong Vdr = 0x00040000;
        public const ulong Ttd = 0x00080000;
        public const ulong Vdo = 0x00100000;
        public const ulong Ths = 0x00200000;
        public const ulong Cur = 0x00400000;
        public const ulong Mws = 0x00800000;
        public const ulong Mwh = 0x01000000;
        public const ulong Gpdtm = 0x02000000;
        public const ulong Htd = 0x04000000;
        public const ulong Vdvbw = 0x08000000;
    }
}
