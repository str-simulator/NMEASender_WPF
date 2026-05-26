namespace NMEASender.Wpf.Models;

public sealed record NmeaBuildOptions(
    bool TrueWind,
    bool UseHdmOutput,
    ProjectType ProjectType);
