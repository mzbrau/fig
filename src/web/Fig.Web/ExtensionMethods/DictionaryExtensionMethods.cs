using Fig.Contracts.Settings;
using Fig.Web.Models.Setting;

namespace Fig.Web.ExtensionMethods;

public static class DictionaryExtensionMethods
{
    public static List<ChangeModel> ToChangeModelList(
        this Dictionary<SettingClientConfigurationModel, List<SettingDataContract>> dictionary)
    {
        var result = new List<ChangeModel>();
        foreach (var client in dictionary)
        {
            foreach (var setting in client.Value)
            {
                result.Add(new ChangeModel(client.Key.Name, setting.Name, GetSettingValue(setting)));
            }
        }

        return result;
    }

    private static string? GetSettingValue(SettingDataContract setting)
    {
        var value = setting.Value;

        if (value is DataGridSettingDataContract)
            return "Updated Data Grid";

        return value?.GetValue()?.ToString();
    }
}