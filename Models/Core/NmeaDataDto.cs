namespace NMEASender.Wpf.Models.Core;

public sealed class NmeaDataDto
{
    public DateTime Time { get; set; } = DateTime.Now;
    public double Latitude { get; set; } = 35.0;
    public double Longitude { get; set; } = 129.0;
    public double OwnLatitude { get; set; } = 35.0;
    public double OwnLongitude { get; set; } = 129.0;
    public double OwnshipPositionX { get; set; } = 0.0;
    public double OwnshipPositionY { get; set; } = 0.0;
    public double SpeedKnots { get; set; } = 0.0;
    public double Heading { get; set; } = 0.0;
    public double GyroHeading { get; set; } = 0.0;
    public double MagneticVariation { get; set; } = 0.0;
    public double CurrentDrift { get; set; } = 0.0;
    public double CurrentSet { get; set; } = 0.0;
    public double LongitudinalSpeedMps { get; set; } = 0.0;
    public double LateralSpeedMps { get; set; } = 0.0;
    public double OwnTurningRate { get; set; } = 0.0;
    public double RudderPort { get; set; } = 0.0;
    public double RudderStbd { get; set; } = 0.0;
    public double RpmPort { get; set; } = 0.0;
    public double RpmStbd { get; set; } = 0.0;
    public double PitchPort { get; set; } = 0.0;
    public double PitchStbd { get; set; } = 0.0;
    public double EngineCommandPort { get; set; } = 0.0;
    public double EngineCommandStbd { get; set; } = 0.0;
    public double WindDirection { get; set; } = 0.0;
    public double WindRelativeDirection { get; set; } = 0.0;
    public double WindSpeedMps { get; set; } = 0.0;
    public double WindRelativeSpeedMps { get; set; } = 0.0;
    public double WaterDepth { get; set; } = 10.0;
    public double WaveDirection { get; set; } = 0.0;
    public double WaveHeight { get; set; } = 0.0;
    public double OwnshipDraft { get; set; } = 3.0;
    public double OwnshipLength { get; set; } = 0.0;
    public double HeightTide { get; set; } = 0.0;
    public double DatumOffsetLatitude { get; set; } = 0.0;
    public double DatumOffsetLongitude { get; set; } = 0.0;
    public int KoseMode { get; set; } = 0;
    public double KoseSogKnots { get; set; } = 0.0;
    public double KoseCog { get; set; } = 0.0;
    public double ThrustCommandBow { get; set; } = 0.0;
    public double ThrustCommandStern { get; set; } = 0.0;
    public double ThrusterThrustBow { get; set; } = 0.0;
    public double ThrusterThrustStern { get; set; } = 0.0;
    public double OwnshipPitch { get; set; } = 0.0;
    public double OwnshipRoll { get; set; } = 0.0;
    public int Mmsi { get; set; } = 440000001;
    public bool IsFinished { get; set; }
    public bool FailGps { get; set; }
    public bool FailGyro { get; set; }
    public bool FailLog { get; set; }
    public bool FailEcho { get; set; }
    public bool UsesTrafficShipData { get; set; }
    public List<TrafficShipData> TrafficShips { get; set; } = new();

    public double SimulationTimeSeconds { get; set; }

    public NmeaDataDto Clone()
    {
        return (NmeaDataDto)MemberwiseClone();
    }
}

public sealed class TrafficShipData
{
    public int SharedIndex { get; set; } = -1;
    public bool IsAisEnabled { get; set; }
    public int Mmsi { get; set; }
    public int ImoNumber { get; set; }
    public string ShipName { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string CallSign { get; set; } = string.Empty;
    public int Draft { get; set; }
    public double Length { get; set; }
    public double Beam { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double Heading { get; set; }
    public double CourseOverGround { get; set; }
    public double LongitudinalSpeedMps { get; set; }
    public double LateralSpeedMps { get; set; }
    public double TurningRate { get; set; }
}

public static class NmeaConstants
{
    public const double NauticalMileMeters = 1852.0;
    public const double ToRadians = 0.01745329251994;
    public const double ToDegrees = 57.29577951308;
}
