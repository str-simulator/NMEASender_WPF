using NMEASender.Wpf.Models.SharedMemory;
using System.Runtime.InteropServices;

namespace NMEASender.Wpf.Models.Projects.PS2404A.SharedMemory;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct PS2404AKoseExtensionNative
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] m_PitchTransv;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int[] m_bDieselStart;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int[] m_bDieselRun;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_DieselCmd;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_DieselRPM;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_DieselShaft;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int[] m_bTurbineStart;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int[] m_bTurbineRun;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_TurbineCmd;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_TurbineRPM;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_TurbineShaft;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public int[] m_bThrusterExist;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] m_ThrusterRPM;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_FuelTendency;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int[] m_iEngineSel;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int[] m_iManeuverMode;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int[] m_bClutchDiesel;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int[] m_bClutchDieselAck;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int[] m_bClutchTurbine;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int[] m_bClutchTurbineAck;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int[] m_bSemiMode;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public byte[] m_bySemiDieselRPM;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public byte[] m_bySemiLongPitch;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public byte[] m_bySemiTranPitch;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public byte[] m_byVSPEngSpeed;
    public double m_MaxDieselRPM;
    public double m_MaxTurbineRPM;
    public int m_bLanding;
    public int m_bTakeoff;
    public int bShot;
    public int bSink;
    public int bLifeTube;
    public int bLifeBoat;
    public int bLifeFlare;
    public int bBoatFlare;
    public int bSonobuoy;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public ShipGun[] stOwnShipGun;
    public LynxParking m_stLynxParking;
    public int lViewPoint;
    public int nViewShipNum;
    public int nZoomInOut;
    public int m_bFinStabilizerOn;
    public ushort m_wNFUCommand;
    public byte m_bySteeringSel;
    public ushort m_wSteeringMode;
    public double m_ComdAutoCourse;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public ushort[] m_wZDir;
    public double dServerRatio;
    public double dServerWaterDepth;
    public byte bCheckDepthEdit;
    public byte bDepthInstructor;
    public double m_dKOSESOG;
    public double m_dKOSECOG;
    public int m_nKOSEMode;

    public double KoseSogKnots => m_dKOSESOG;
    public double KoseCog => m_dKOSECOG;
    public int KoseMode => m_nKOSEMode;
}
