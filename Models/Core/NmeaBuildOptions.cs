using NMEASender.Wpf.Models.Projects;

namespace NMEASender.Wpf.Models.Core;

public sealed record NmeaBuildOptions(
    bool TrueWind,
    bool UseHdmOutput,
    ProjectType ProjectType);
