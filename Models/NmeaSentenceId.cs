namespace NMEASender.Wpf.Models;

[Flags]
public enum NmeaSendFlag : uint
{
    Rmc = 0x00000001,
    Gga = 0x00000002,
    Gll = 0x00000004,
    Vtg = 0x00000008,
    Zda = 0x00000010,
    Hdt = 0x00000020,
    Vbw = 0x00000040,
    Rot = 0x00000080,
    Rsa = 0x00000100,
    RpmPort = 0x00000200,
    Mwv = 0x00000400,
    Hdg = 0x00000800,
    Vdm = 0x00001000,
    RpmStbd = 0x00002000,
    Dpt = 0x00004000,
    Dbt = 0x00008000,
    Etl = 0x00010000,
    Cur = 0x00020000,
    Mda = 0x00040000,
    Trc = 0x00080000,
    Trd = 0x00100000,
    Hpm = 0x00200000,
    Hrm = 0x00400000,
    Ttm = 0x00800000,
    Vdo = 0x01000000,
    STR = 0x10000000,
    Rpm = RpmPort // legacy alias
}

public enum NmeaSentenceId
{
    Gga,
    Gll,
    Rmc,
    Vtg,
    Zda,
    Hdt,
    Vbw,
    Rot,
    Rsa,
    RpmPort,
    RpmStbd,
    Mwv,
    Hdg,
    Dpt,
    Dbt,
    Etl,
    Cur,
    Mda,
    Trc,
    Trd,
    Hpm,
    Hrm,
    Vdm,
    Vdo,
    STR
}
