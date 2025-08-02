using Fig.Common.NetStandard.Scripting;
using Fig.Web.Models.Setting;

namespace Fig.Web.Scripting;

/// <summary>
/// Adapter class to make SettingClientConfigurationModel compatible with IScriptableClient
/// </summary>
public class ScriptableClientAdapter : IScriptableClient
{
    private readonly SettingClientConfigurationModel _client;

    public ScriptableClientAdapter(SettingClientConfigurationModel client)
    {
        _client = client;
    }

    public Guid Id => _client.Id;
    
    public string Name => _client.Name;
    
    public List<IScriptableSetting> Settings => 
        _client.Settings.Cast<IScriptableSetting>().ToList();
}
