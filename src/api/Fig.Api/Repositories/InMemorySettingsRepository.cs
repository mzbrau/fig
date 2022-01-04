using Fig.Api.BusinessEntities;

namespace Fig.Api.Repositories;

public class InMemorySettingsRepository : ISettingsRepository
{
    private readonly List<SettingsClientBusinessEntity> _clients = new();

    public SettingsClientBusinessEntity? GetRegistration(string name)
    {
        return _clients.FirstOrDefault(a => a.Name == name && a.Instance == null);
    }

    public void RegisterSettings(SettingsClientBusinessEntity settings)
    {
        _clients.Add(settings);
    }

    public IEnumerable<SettingsClientBusinessEntity> GetAllSettings()
    {
        return _clients;
    }

    public SettingsClientBusinessEntity? GetClient(string name, string? instance = null)
    {
        return _clients.FirstOrDefault(a => a.Name == name && a.Instance == instance);
    }
}