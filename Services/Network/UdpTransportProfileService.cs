using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Services.Interfaces.Network;
using NMEASender.Wpf.Services.Interfaces.Projects;

namespace NMEASender.Wpf.Services.Network;

public sealed class UdpTransportProfileService : IUdpTransportProfileService
{
    private readonly IReadOnlyDictionary<ProjectType, IProjectUdpTransportProfileStore> _projectStores;
    private readonly IProjectUdpTransportProfileStore _fallbackStore;

    public UdpTransportProfileService(IEnumerable<IProjectUdpTransportProfileStore> projectStores)
    {
        if (projectStores is null)
        {
            throw new ArgumentNullException(nameof(projectStores));
        }

        List<IProjectUdpTransportProfileStore> stores = projectStores.ToList();
        if (stores.Count == 0)
        {
            throw new InvalidOperationException("At least one UDP transport profile store must be registered.");
        }

        _projectStores = stores
            .GroupBy(store => store.ProjectType)
            .ToDictionary(group => group.Key, group => group.First());

        _fallbackStore = _projectStores.TryGetValue(ProjectType.PS000, out IProjectUdpTransportProfileStore? ps000Store)
            ? ps000Store
            : stores[0];
    }

    public UdpTransportOptions Load(ProjectType projectType, string configDirectory, int fallbackPort)
    {
        return Resolve(projectType).Load(configDirectory, fallbackPort);
    }

    public void Save(ProjectType projectType, string configDirectory, UdpTransportOptions options)
    {
        Resolve(projectType).Save(configDirectory, options);
    }

    private IProjectUdpTransportProfileStore Resolve(ProjectType projectType)
    {
        return _projectStores.TryGetValue(projectType, out IProjectUdpTransportProfileStore? store)
            ? store
            : _fallbackStore;
    }
}
