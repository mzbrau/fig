using Humanizer;

namespace Fig.Web.Models.Setting;

public class SettingSearchModel
{
    private readonly ISetting _setting;
    
    public SettingSearchModel(string clientName, string? instanceName, ISetting setting)
    {
        ClientName = clientName;
        InstanceName = instanceName ?? string.Empty;
        SettingName = setting.DisplayName;
        CategoryColor = setting.CategoryColor;
        Description = setting.Description.ToString().Truncate(80);
        var valueType = setting.ValueType;
        var underlyingType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        SettingType = underlyingType.Name;
        ScrollId = $"{clientName}-{instanceName}-{setting.Name}";
        _setting = setting;
    }

    public string ClientName { get; }

    public string InstanceName { get; }
    
    public string SettingName { get; }

    public string CategoryColor { get; }

    public string Description { get; }

    public string SettingType { get; }

    public string SettingValue => _setting.StringValue.Truncate(100);
    
    public string ScrollId { get; }

    public SettingClientConfigurationModel Parent => _setting.Parent;
}