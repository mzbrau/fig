using Fig.Contracts.Settings;
using Fig.Web.Models.Clients;
using Fig.Web.Models.Setting;

namespace Fig.Web.ExtensionMethods;

public static class DictionaryExtensionMethods
{
    public static List<ChangeModel> ToChangeModelList(
        this Dictionary<SettingClientConfigurationModel, List<SettingDataContract>> dictionary,
        List<ClientRunSessionModel> runSessions)
    {
        var result = new List<ChangeModel>();
        foreach (var client in dictionary)
        {
            var runSessionsToBeApplied = GetRunSessionsToBeApplied(client.Key, runSessions);
            foreach (var setting in client.Value)
            {
                result.Add(new ChangeModel(client.Key.Name, setting.Name, GetSettingValue(setting), runSessionsToBeApplied));
            }
        }

        return result;
    }

    private static string GetRunSessionsToBeApplied(SettingClientConfigurationModel client, List<ClientRunSessionModel> runSessions)
    {
        var matchingSessions = runSessions.Where(a => a.Name == client.Name && a.Instance == client.Instance).ToList();
        return $"{matchingSessions.Count(a => a.LiveReload)} / {matchingSessions.Count}";
    }

    private static string? GetSettingValue(SettingDataContract setting)
    {
        var value = setting.Value;

        if (value is DataGridSettingDataContract)
            return "Updated Data Grid";

        return value?.GetValue()?.ToString();
    }
}