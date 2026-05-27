using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Services.Interfaces.Projects;
using NMEASender.Wpf.Services.Interfaces.Transmission;

namespace NMEASender.Wpf.Services.Transmission;

public sealed class NmeaSentenceBuilderService : INmeaSentenceBuilderService
{
    private readonly IReadOnlyDictionary<ProjectType, IProjectNmeaSentenceBuilder> _projectBuilders;
    private readonly IProjectNmeaSentenceBuilder _fallbackBuilder;

    public NmeaSentenceBuilderService(IEnumerable<IProjectNmeaSentenceBuilder> projectBuilders)
    {
        if (projectBuilders is null)
        {
            throw new ArgumentNullException(nameof(projectBuilders));
        }

        List<IProjectNmeaSentenceBuilder> builderList = projectBuilders.ToList();
        if (builderList.Count == 0)
        {
            throw new InvalidOperationException("At least one project NMEA builder must be registered.");
        }

        _projectBuilders = builderList
            .GroupBy(builder => builder.ProjectType)
            .ToDictionary(group => group.Key, group => group.First());

        _fallbackBuilder = _projectBuilders.TryGetValue(ProjectType.PS000, out IProjectNmeaSentenceBuilder? ps000Builder)
            ? ps000Builder
            : builderList[0];
    }

    public IReadOnlyList<string> Build(NmeaSentenceId id, NmeaDataDto data, NmeaBuildOptions options)
    {
        return Resolve(options.ProjectType).Build(id, data, options);
    }

    public byte Checksum(string body)
    {
        return _fallbackBuilder.Checksum(body);
    }

    public string BuildVtgSentence(double gyroHeading, double magneticVariation, double waterSpeedKnots, double waterSpeedKmh, NmeaBuildOptions options)
    {
        return Resolve(options.ProjectType).BuildVtgSentence(
            gyroHeading,
            magneticVariation,
            waterSpeedKnots,
            waterSpeedKmh,
            options);
    }

    private IProjectNmeaSentenceBuilder Resolve(ProjectType projectType)
    {
        return _projectBuilders.TryGetValue(projectType, out IProjectNmeaSentenceBuilder? builder)
            ? builder
            : _fallbackBuilder;
    }
}
