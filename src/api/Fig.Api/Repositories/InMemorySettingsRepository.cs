using Fig.Api.BusinessEntities;

namespace Fig.Api.Repositories;

public class InMemorySettingsRepository : ISettingsRepository
{
    private readonly List<SettingsClientBusinessEntity> _clients = new();
    
    public SettingsClientBusinessEntity? GetRegistration(string name)
    {
        return _clients.FirstOrDefault(a => a.Name == name && a.Instance == null);
    }

    public string RegisterSettings(SettingsClientBusinessEntity settings)
    {
        settings.Id = Guid.NewGuid().ToString();
        _clients.Add(settings);
        return settings.Id;
    }

    public IEnumerable<SettingsClientBusinessEntity> GetAllSettings()
    {
        return _clients;
    }

    public SettingsClientBusinessEntity? GetClient(string id, string? hostname = null, string? username = null, string? instance = null)
    {
        return _clients.FirstOrDefault(a => a.Id == id);
    }
}