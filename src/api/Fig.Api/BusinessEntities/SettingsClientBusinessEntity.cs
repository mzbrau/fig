
namespace Fig.Api.BusinessEntities;

public class SettingsClientBusinessEntity
{
    public string Name { get; set; }

    public string ClientSecret { get; set; }

    public string? Instance { get; set; }

    public List<SettingBusinessEntity> Settings { get; set; }

    public SettingsClientBusinessEntity CreateOverride(string? instance)
    {
        return new SettingsClientBusinessEntity
        {
            Name = Name,
            ClientSecret = ClientSecret,
            Instance = instance,
            Settings = Settings.Select(a => a.Clone()).ToList()
        };
    }
}