using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Models.SharedMemory;
using NMEASender.Wpf.Services.Interfaces.Config;
using NMEASender.Wpf.Services.Interfaces.IO;
using NMEASender.Wpf.Services.Interfaces.Projects;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace NMEASender.Wpf.Services.IO;

public sealed class SharedMemoryProviderService : ISharedMemoryProviderService
{
    private const string OwnShipMapName = "STR_OWNSHIP_DATA";
    private readonly INmeaSenderConfigService _config;
    private readonly IReadOnlyDictionary<ProjectType, IProjectSharedMemoryExtensionReader> _extensionReaders;
    private MemoryMappedFile? _ownShipMemory;
    private MemoryMappedViewAccessor? _ownShipView;
    private readonly Dictionary<int, TrafficShipMemory> _trafficShipViews = new();

    public SharedMemoryProviderService(
        INmeaSenderConfigService config,
        IEnumerable<IProjectSharedMemoryExtensionReader> extensionReaders)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        if (extensionReaders is null)
        {
            throw new ArgumentNullException(nameof(extensionReaders));
        }

        _extensionReaders = extensionReaders
            .GroupBy(reader => reader.ProjectType)
            .ToDictionary(group => group.Key, group => group.First());
    }

    public bool TryRead(out NmeaDataDto data, out string error)
    {
        data = new NmeaDataDto();
        error = string.Empty;

        if (!EnsureOwnShipOpen(out error))
        {
            return false;
        }

        try
        {
            MemoryMappedViewAccessor view = _ownShipView!;
            if (view.Capacity < Marshal.SizeOf<OwnShipDataNative>())
            {
                error = $"STR_OWNSHIP_DATA size is too small ({view.Capacity} bytes).";
                ResetOwnShipHandle();
                return false;
            }

            OwnShipDataNative own = ReadStruct<OwnShipDataNative>(view);
            ushort trafficCount = own.m_wTrafficCount;
            data = ReadOwnShip(own);
            ApplyProjectExtension(view, data);
            data.UsesTrafficShipData = true;
            data.TrafficShips = ReadTrafficShips(trafficCount, data.CurrentSet, data.CurrentDrift);
            if (data.IsFinished)
            {
                error = "IOS simulation is finished.";
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or UnauthorizedAccessException)
        {
            error = ex.Message;
            ResetOwnShipHandle();
            return false;
        }
    }

    public void Dispose()
    {
        ResetOwnShipHandle();
    }

    private bool EnsureOwnShipOpen(out string error)
    {
        error = string.Empty;
        if (_ownShipView is not null)
        {
            return true;
        }

        try
        {
            _ownShipMemory = MemoryMappedFile.OpenExisting(OwnShipMapName, MemoryMappedFileRights.Read);
            _ownShipView = _ownShipMemory.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            return true;
        }
        catch (FileNotFoundException)
        {
            error = $"{OwnShipMapName} shared memory was not found.";
        }
        catch (UnauthorizedAccessException ex)
        {
            error = $"{OwnShipMapName} access denied: {ex.Message}";
        }
        catch (IOException ex)
        {
            error = $"{OwnShipMapName} open failed: {ex.Message}";
        }

        ResetOwnShipHandle();
        return false;
    }

    private static NmeaDataDto ReadOwnShip(OwnShipDataNative own)
    {
        double gpsLatitude = SafeDouble(own.GpsLatitude);
        double gpsLongitude = SafeDouble(own.GpsLongitude);
        double ownLatitude = SafeDouble(own.OwnLatitude);
        double ownLongitude = SafeDouble(own.OwnLongitude);

        if (!IsValidCoordinate(gpsLatitude, gpsLongitude) && IsValidCoordinate(ownLatitude, ownLongitude))
        {
            gpsLatitude = ownLatitude;
            gpsLongitude = ownLongitude;
        }

        if (!IsValidCoordinate(ownLatitude, ownLongitude) && IsValidCoordinate(gpsLatitude, gpsLongitude))
        {
            ownLatitude = gpsLatitude;
            ownLongitude = gpsLongitude;
        }

        double longitudinalSpeed = SafeDouble(own.OwnshipLongVel);
        double lateralSpeed = SafeDouble(own.OwnshipLatVel);
        double speedKnots = Math.Sqrt(longitudinalSpeed * longitudinalSpeed + lateralSpeed * lateralSpeed)
            * 3600.0 / NmeaConstants.NauticalMileMeters;

        return new NmeaDataDto
        {
            Time = ToSimulationDateTime(SafeDouble(own.SimulationTime)),
            Latitude = gpsLatitude,
            Longitude = gpsLongitude,
            OwnLatitude = ownLatitude,
            OwnLongitude = ownLongitude,
            OwnshipPositionX = SafeDouble(own.m_OwnshipPos.x),
            OwnshipPositionY = SafeDouble(own.m_OwnshipPos.y),
            SpeedKnots = speedKnots,
            Heading = NormalizeDegrees(SafeDouble(own.OwnshipHeading)),
            GyroHeading = NormalizeDegrees(SafeDouble(own.GyroHeading)),
            MagneticVariation = SafeDouble(own.MagneticVariation),
            CurrentDrift = SafeDouble(own.CurrentDrift),
            CurrentSet = NormalizeDegrees(SafeDouble(own.CurrentSet)),
            LongitudinalSpeedMps = longitudinalSpeed,
            LateralSpeedMps = lateralSpeed,
            OwnTurningRate = SafeDouble(own.OwnTurningRate),
            RudderPort = SafeDouble(own.RudderValue1),
            RudderStbd = SafeDouble(own.RudderValue0),
            RpmPort = SafeDouble(own.Rpm0),
            RpmStbd = SafeDouble(own.Rpm1),
            PitchPort = SafeDouble(own.Pitch0),
            PitchStbd = SafeDouble(own.Pitch1),
            EngineCommandPort = SafeDouble(own.EngineCommand0),
            EngineCommandStbd = SafeDouble(own.EngineCommand1),
            WindDirection = NormalizeDegrees(SafeDouble(own.WindDirection)),
            WindRelativeDirection = NormalizeDegrees(SafeDouble(own.WindRelativeDirection)),
            WindSpeedMps = SafeDouble(own.WindSpeed),
            WindRelativeSpeedMps = SafeDouble(own.WindRelativeSpeed),
            WaterDepth = SafeDouble(own.WaterDepth),
            WaveDirection = NormalizeDegrees(SafeDouble(own.m_WaveDirection)),
            WaveHeight = SafeDouble(own.m_WaveHeight),
            OwnshipDraft = SafeDouble(own.OwnshipDraft),
            OwnshipLength = SafeDouble(own.m_OwnshipLength),
            HeightTide = SafeDouble(own.HeightTide),
            DatumOffsetLatitude = SafeDouble(own.m_OriginLatLon.x),
            DatumOffsetLongitude = SafeDouble(own.m_OriginLatLon.y),
            ThrustCommandBow = SafeDouble(own.ThrustCommand0),
            ThrustCommandStern = SafeDouble(own.ThrustCommand1),
            ThrusterThrustBow = SafeDouble(own.ThrusterThrust0),
            ThrusterThrustStern = SafeDouble(own.ThrusterThrust1),
            OwnshipPitch = SafeDouble(own.OwnshipPitch),
            OwnshipRoll = SafeDouble(own.OwnshipRoll),
            Mmsi = own.AisMmsi > 0 ? own.AisMmsi : 440000001,
            IsFinished = own.Finish != 0,
            FailGps = own.FailGps != 0,
            FailGyro = own.FailGyro != 0,
            FailLog = own.FailLog != 0,
            FailEcho = own.FailEcho != 0,
            SimulationTimeSeconds = SafeDouble(own.SimulationTime)
        };
    }

    private void ApplyProjectExtension(MemoryMappedViewAccessor view, NmeaDataDto data)
    {
        if (_extensionReaders.TryGetValue(_config.ProjectType, out IProjectSharedMemoryExtensionReader? reader))
        {
            reader.Apply(view, Marshal.SizeOf<OwnShipDataNative>(), data);
        }
    }

    private void ResetOwnShipHandle()
    {
        _ownShipView?.Dispose();
        _ownShipMemory?.Dispose();
        _ownShipView = null;
        _ownShipMemory = null;
        ResetTrafficShipHandles();
    }

    private List<TrafficShipData> ReadTrafficShips(ushort tshipCnt, double currentSet, double currentDrift)
    {
        // Match legacy behavior: reopen traffic ship shared memory every read cycle.
        // Some producers recreate these mappings frequently, which can stale cached handles.
        ResetTrafficShipHandles();

        List<TrafficShipData> ships = new List<TrafficShipData>();

        for (int index = 0; index < tshipCnt; index++)
        {
            if (!EnsureTrafficShipOpen(index, out TrafficShipMemory memory))
            {
                continue;
            }

            try
            {
                if (memory.View.Capacity < Marshal.SizeOf<TShipNative>())
                {
                    ResetTrafficShipHandle(index);
                    continue;
                }

                TrafficShipData ship = ReadTrafficShip(ReadStruct<TShipNative>(memory.View), currentSet, currentDrift);
                ship.SharedIndex = index;
                if (IsValidCoordinate(ship.Latitude, ship.Longitude))
                {
                    ships.Add(ship);
                }
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or UnauthorizedAccessException)
            {
                ResetTrafficShipHandle(index);
            }
        }

        return ships;
    }

    private bool EnsureTrafficShipOpen(int index, out TrafficShipMemory memory)
    {
        if (_trafficShipViews.TryGetValue(index, out memory!))
        {
            return true;
        }

        try
        {
            MemoryMappedFile map = MemoryMappedFile.OpenExisting($"STR_TSHIP_DATA_#{index}", MemoryMappedFileRights.Read);
            MemoryMappedViewAccessor view = map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            memory = new TrafficShipMemory(map, view);
            _trafficShipViews[index] = memory;
            return true;
        }
        catch (Exception ex) when (ex is FileNotFoundException or IOException or UnauthorizedAccessException)
        {
            memory = null!;
            return false;
        }
    }

    private static TrafficShipData ReadTrafficShip(TShipNative shipData, double currentSet, double currentDrift)
    {
        double courseOverGround = NormalizeDegrees(SafeDouble(shipData.m_TrafficHeading));
        double longitudinalSpeed = SafeDouble(shipData.m_LongVel);
        double lateralSpeed = SafeDouble(shipData.m_LatVel);
        double driftLateralSpeed = -currentDrift * Math.Sin((currentSet - courseOverGround) * NmeaConstants.ToRadians);
        double trueHeading = NormalizeDegrees(Math.Atan2(driftLateralSpeed, longitudinalSpeed) * NmeaConstants.ToDegrees + courseOverGround);
        return new TrafficShipData
        {
            IsAisEnabled = shipData.m_stAISInfo.m_bEnable != 0,
            Mmsi = shipData.m_stAISInfo.m_nMMSI,
            ImoNumber = shipData.m_stAISInfo.m_nIMONo,
            ShipName = ReadAscii(shipData.m_szShipName),
            Destination = ReadAscii(shipData.m_stAISInfo.m_szDestination),
            CallSign = ReadAscii(shipData.m_stAISInfo.m_szCallSign),
            Draft = shipData.m_stAISInfo.m_nDraft,
            Length = SafeDouble(shipData.m_Length),
            Beam = SafeDouble(shipData.m_Beam),
            Latitude = SafeDouble(shipData.m_TShipLatLon.x),
            Longitude = SafeDouble(shipData.m_TShipLatLon.y),
            PositionX = SafeDouble(shipData.m_TrafficPos.x),
            PositionY = SafeDouble(shipData.m_TrafficPos.y),
            Heading = trueHeading,
            CourseOverGround = courseOverGround,
            LongitudinalSpeedMps = longitudinalSpeed,
            LateralSpeedMps = lateralSpeed,
            TurningRate = SafeDouble(shipData.m_TurningRate)
        };
    }

    private void ResetTrafficShipHandle(int index)
    {
        if (!_trafficShipViews.Remove(index, out TrafficShipMemory? memory))
        {
            return;
        }

        memory.Dispose();
    }

    private void ResetTrafficShipHandles()
    {
        foreach (TrafficShipMemory? memory in _trafficShipViews.Values)
        {
            memory.Dispose();
        }

        _trafficShipViews.Clear();
    }

    private static DateTime ToSimulationDateTime(double seconds)
    {
        if (!double.IsFinite(seconds))
        {
            return DateTime.Now;
        }

        long unixSeconds = (long)(seconds + 0.001);
        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTime.Now;
        }
    }

    private static T ReadStruct<T>(MemoryMappedViewAccessor view) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] buffer = new byte[size];
        view.ReadArray(0, buffer, 0, size);
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    private static string ReadAscii(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return string.Empty;
        }

        int end = Array.IndexOf(bytes, (byte)0);
        if (end < 0)
        {
            end = bytes.Length;
        }

        return Encoding.ASCII.GetString(bytes, 0, end);
    }

    private static double SafeDouble(double value)
    {
        return double.IsFinite(value) ? value : 0.0;
    }

    private static bool IsValidCoordinate(double latitude, double longitude)
    {
        return double.IsFinite(latitude) &&
               double.IsFinite(longitude) &&
               Math.Abs(latitude) <= 90.0 &&
               Math.Abs(longitude) <= 180.0;
    }

    private static double NormalizeDegrees(double degrees)
    {
        if (!double.IsFinite(degrees))
        {
            return 0.0;
        }

        degrees %= 360.0;
        return degrees < 0.0 ? degrees + 360.0 : degrees;
    }


    private sealed class TrafficShipMemory : IDisposable
    {
        public TrafficShipMemory(MemoryMappedFile map, MemoryMappedViewAccessor view)
        {
            Map = map;
            View = view;
        }

        public MemoryMappedFile Map { get; }

        public MemoryMappedViewAccessor View { get; }

        public void Dispose()
        {
            View.Dispose();
            Map.Dispose();
        }
    }
}
