using System.Runtime.InteropServices;

namespace NMEASender.Wpf.Models.SharedMemory;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct PointStruct
{
    public double x;
    public double y;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct PositionStruct
{
    public double x;
    public double y;
    public double z;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct ShipFlag
{
    public short nIndex;
    public short nMast;
    public short nOne;
    public short nTwo;
    public short nThree;
    public short nFour;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct ShipShape
{
    public short nOne;
    public short nTwo;
    public short nThree;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct ShipGun
{
    public double dGunDeg;
    public byte bGunShot;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct LynxParking
{
    public int nShipNo;
    public byte bParking;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct ST_TUG_WINCH_INFO
{
    public double dLineLength;
    public float fLineAngle;
    public double dTension;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] public double[] dTugForce;
    public int nChockState;
    public int bOverload;
    public int bLowLevelAlarm;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct ST_TUG_WINCH_CMD
{
    public int bRunningStart;
    public int bSource;
    public int nSpeed;
    public int nBrake;
    public int nLever;
    public int nChockPos;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct ST_AISINFO
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] public byte[] m_szDestination;
    public int m_nMMSI;
    public int m_nIMONo;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)] public byte[] m_szCallSign;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] public byte[] m_szShipType;
    public int m_nDraft;
    public int m_bEnable;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct ST_WINCH_POINT
{
    public int nWinchPos;
    public double x;
    public double y;
    public double z;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct MLine
{
    public int m_bDisplay;
    public byte m_byTargetType;
    public ushort m_wChockId;
    public ushort m_wTargetId;
    public ushort m_wBollardId;
    public ushort m_wCommand;
    public double m_Tension;
    public PositionStruct m_BollardPos;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct TShipNative
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)] public byte[] m_szShipName;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)] public byte[] m_szShipType;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] m_szDBName;
    public ushort m_wShipType;
    public double m_Length;
    public double m_Beam;
    public byte m_byControlMode;
    public PositionStruct m_TrafficPos;
    public PointStruct m_TShipLatLon;
    public double m_TrafficHeading;
    public double m_TrafficPitch;
    public double m_TrafficRoll;
    public double m_LatVel;
    public double m_LongVel;
    public double m_RudderAngle;
    public double m_TurningRate;
    public int m_bDispShip;
    public int m_bPositionFlag;
    public int m_bSupplyLine;
    public int m_bWhistle;
    public int m_bTowline;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)] public int[] m_bLightNav;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)] public int[] m_bLightSig1;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public int[] m_bLightSig2;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)] public int[] bDeckLight;
    public ShipFlag stShipFlag;
    public ShipShape stShipShape;
    public double m_MaxTugForce;
    public int m_bTugEnable;
    public ushort m_wTugPosition;
    public double m_TugThrust;
    public double m_TugDirection;
    public PositionStruct m_TowlinePoint;
    public ushort m_wTugCommand;
    public ushort m_wTugFlag;
    public int m_bTugCWDir;
    public int bShot;
    public int bSink;
    public int bLifeTube;
    public int bLifeBoat;
    public int bLifeFlare;
    public int bBoatFlare;
    public int bSonobuoy;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public ShipGun[] stTShipGun;
    public LynxParking m_stLynxParking;
    public ushort m_wSimMode;
    public ST_TUG_WINCH_INFO m_stTugInfo;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] public double[] m_dTugForce;
    public ST_AISINFO m_stAISInfo;
    public ST_WINCH_POINT m_stTugWinchPoint;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct OwnShipDataNative
{
    public uint m_dwStructSize;
    public double m_ToleranceRate;
    public uint m_dwNetworkSimId;
    public int m_lPacketId;
    public int m_nIOSType;
    public ushort m_wSimMode;
    public ushort m_wSimSpeed;
    public ushort m_wOutputStep;
    public int m_bFinish;
    public int m_bRunning;
    public long m_timeStart;
    public double m_SimulationTime;
    public uint m_dwSystemTime;
    public double m_TimeStep;
    public uint m_dwTimeStep;
    public uint m_dwHostAddress;
    public int m_bTwinEngine;
    public int m_bBridgeControl;
    public ushort m_wOwnshipType;
    public ushort m_wEngineType;
    public ushort m_wMDevType;
    public double m_MaxAheadRPM;
    public double m_MaxAsternRPM;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)] public byte[] m_szSimulationID;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)] public byte[] m_szHarborName;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)] public byte[] m_szShipName;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)] public byte[] m_szCurrentFile;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)] public byte[] m_szWaveFile;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1000)] public byte[] m_szBollardName;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] m_szVisualScene;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] m_szSituationDisp;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] m_szRadarLandmass;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] m_szOwnshipBow;
    public ushort m_wAnchorCount;
    public ushort m_wTrafficCount;
    public ushort m_wTugCount;
    public ushort m_wNoClients;
    public ushort m_wBollardCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public PositionStruct[] m_AnchorPoint;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)] public PositionStruct[] m_ChockPoint;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] public PositionStruct[] m_BollardPos;
    public PointStruct m_OriginLatLon;
    public int m_bInstructDepth;
    public int m_bInstructWind;
    public int m_bInstructWave;
    public int m_bInstructCurrent;
    public int m_bWindNoise;
    public double m_CmdWaterDepth;
    public double m_WaterDepth;
    public double m_CmdWindSpeed;
    public double m_WindSpeed;
    public double m_WindRelSpeed;
    public double m_CmdWindDir;
    public double m_WindDir;
    public double m_WindRelDir;
    public double m_CmdWaveHeight;
    public double m_WaveHeight;
    public double m_CmdWaveDir;
    public double m_WaveDirection;
    public double m_CmdCurrentSpeed;
    public double m_CurrentDrift;
    public double m_CmdCurrentDir;
    public double m_CurrentSet;
    public double m_CmdCurrentHeight;
    public double m_HeightTide;
    public double m_MagneticVar;
    public double m_GyroHeading;
    public double m_DayTimeSecond;
    public double m_VisibleRange;
    public ushort m_wVisibility;
    public byte m_byDayCondition;
    public int nWeatherParam;
    public int nCloudParam;
    public int nNightParam;
    public int m_bThunder;
    public int m_bBuzzer;
    public int nClashSound;
    public double m_OwnshipLength;
    public double m_OwnshipBeam;
    public double m_OwnshipDraft;
    public int m_bCPP;
    public PositionStruct m_OwnshipPos;
    public PointStruct m_LatLon;
    public PointStruct m_GPSLatLon;
    public double m_OwnTurningRate;
    public double m_OwnshipHeading;
    public double m_OwnshipPitch;
    public double m_OwnshipRoll;
    public double m_OwnshipLatVel;
    public double m_OwnshipLongVel;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] m_LatVel;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_RudderCommand;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_RudderValue;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_EngineCommand;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_EngineValue;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_RPM;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] m_Pitch;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_BucketCommand;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_BucketValue;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)] public MLine[] m_MooringLine;
    public int m_bPositionFlag;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)] public int[] m_bLightNav;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)] public int[] m_bLightSig1;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public int[] m_bLightSig2;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)] public int[] bDeckLight;
    public int cLightIntensity;
    public ShipFlag m_stShipFlag;
    public ShipShape m_stShipShape;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public int[] m_bSpotLight;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] m_SpotBearing;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] m_SpotElevation;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] m_SpotWidth;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public PointStruct[] m_AnchorPos;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public ushort[] m_wAnchStatus;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] m_AnchLength;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] m_AnchorTotalLength;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public int[] m_bThrusterOn;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] m_ThrustCmd;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public double[] m_ThrusterThrust;
    public int m_bOwnshipWhistle;
    public int m_bAccident;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_dDP_ThrusterCmd;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] m_dDP_ThrusterRpm;
    public int m_bDP_Auto_All;
    public int m_bDP_Auto_Surge;
    public int m_bDP_Auto_Sway;
    public int m_bDP_Auto_Yaw;
    public int m_nJoystick_x;
    public int m_nJoystick_y;
    public int m_nJoystick_z;
    public ST_AISINFO m_stAISInfo;
    public ST_TUG_WINCH_CMD m_stTugWinchCmd;
    public ST_TUG_WINCH_INFO m_stTugWinchInfo;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] public double[] dPSAccel;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] public double[] dPSVelocity;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public int[] m_bAlarm;
    public int m_bFailRadar;
    public int m_bFailSteeringWheel;
    public int m_bFailSteeringWheel2;
    public int m_bFailSteerPumpMain;
    public int m_bFailSteerPumpCenter;
    public int m_bFailSteerPumpSub;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public int[] m_bFailEngine;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public int[] m_bFailTurbine;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public int[] m_bFailPitch;
    public int m_bFailGyroAbnormal;
    public int m_bFailGyroFail;
    public int m_bFailGPS;
    public int m_bFailLOG;
    public int m_bFailECHO;

    public double SimulationTime => m_SimulationTime;
    public double WaterDepth => m_WaterDepth;
    public double WindSpeed => m_WindSpeed;
    public double WindRelativeSpeed => m_WindRelSpeed;
    public double WindDirection => m_WindDir;
    public double WindRelativeDirection => m_WindRelDir;
    public double CurrentDrift => m_CurrentDrift;
    public double CurrentSet => m_CurrentSet;
    public double HeightTide => m_HeightTide;
    public double MagneticVariation => m_MagneticVar;
    public double GyroHeading => m_GyroHeading;
    public double OwnshipDraft => m_OwnshipDraft;
    public double OwnLatitude => m_LatLon.x;
    public double OwnLongitude => m_LatLon.y;
    public double GpsLatitude => m_GPSLatLon.x;
    public double GpsLongitude => m_GPSLatLon.y;
    public double OwnTurningRate => m_OwnTurningRate;
    public double OwnshipHeading => m_OwnshipHeading;
    public double OwnshipPitch => m_OwnshipPitch;
    public double OwnshipRoll => m_OwnshipRoll;
    public double OwnshipLatVel => m_OwnshipLatVel;
    public double OwnshipLongVel => m_OwnshipLongVel;
    public double RudderValue => m_RudderValue is { Length: > 0 } ? m_RudderValue[0] : 0.0;
    public double EngineCommand0 => m_EngineCommand is { Length: > 0 } ? m_EngineCommand[0] : 0.0;
    public double EngineCommand1 => m_EngineCommand is { Length: > 1 } ? m_EngineCommand[1] : 0.0;
    public double Rpm0 => m_RPM is { Length: > 0 } ? m_RPM[0] : 0.0;
    public double Rpm1 => m_RPM is { Length: > 1 } ? m_RPM[1] : 0.0;
    public double Pitch0 => m_Pitch is { Length: > 0 } ? m_Pitch[0] : 0.0;
    public double Pitch1 => m_Pitch is { Length: > 1 } ? m_Pitch[1] : 0.0;
    public double ThrustCommand0 => m_ThrustCmd is { Length: > 0 } ? m_ThrustCmd[0] : 0.0;
    public double ThrustCommand1 => m_ThrustCmd is { Length: > 1 } ? m_ThrustCmd[1] : 0.0;
    public double ThrusterThrust0 => m_ThrusterThrust is { Length: > 0 } ? m_ThrusterThrust[0] : 0.0;
    public double ThrusterThrust1 => m_ThrusterThrust is { Length: > 1 } ? m_ThrusterThrust[1] : 0.0;
    public int AisMmsi => m_stAISInfo.m_nMMSI;
    public int Finish => m_bFinish;
    public int FailGyro => m_bFailGyroFail;
    public int FailGps => m_bFailGPS;
    public int FailLog => m_bFailLOG;
    public int FailEcho => m_bFailECHO;
}
