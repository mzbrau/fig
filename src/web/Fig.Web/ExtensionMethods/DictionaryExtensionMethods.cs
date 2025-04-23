using System.Globalization;
using System.Text;
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
                var isValid = client.Key.Settings.FirstOrDefault(a => a.Name == setting.Name)?.IsValid ?? false;
                var isExternallyManaged = client.Key.Settings.FirstOrDefault(a => a.Name == setting.Name)?.IsExternallyManaged ?? false;
                result.Add(new ChangeModel(client.Key.Name, setting.Name, GetSettingValue(setting, client.Key), runSessionsToBeApplied, isValid, isExternallyManaged));
            }
        }

        return result;
    }

    private static string GetRunSessionsToBeApplied(SettingClientConfigurationModel client, List<ClientRunSessionModel> runSessions)
    {
        var matchingSessions = runSessions.Where(a => a.Name == client.Name && a.Instance == client.Instance).ToList();
        return $"{matchingSessions.Count(a => a.LiveReload)} / {matchingSessions.Count}";
    }

    private static string? GetSettingValue(SettingDataContract setting, SettingClientConfigurationModel parent)
    {
        var definition = parent.Settings.FirstOrDefault(a => a.Name == setting.Name);
        if (definition is not null)
        {
            if (definition.IsSecret)
                return "******";

            return definition.GetChangeDiff();
        }
        
        return Convert.ToString(setting.Value?.GetValue(), CultureInfo.InvariantCulture);
    }
    
    public static string ToDataGridStringValue(this List<Dictionary<string, object?>>? value, int rowsCount = 10, bool includeStyling = true)
    {
        var rows = value;
        
        if (rows is null || !rows.Any())
            return "<NOT SET>";

        var builder = new StringBuilder();
        foreach (var row in rows.Take(rowsCount))
        {
            IEnumerable<string> values = row.Values.Select(a =>
            {
                if (a is List<string> list)
                {
                    return string.Join(",", list);
                }
                return  a?.ToString() ?? string.Empty;
            }).ToList();
            
            builder.AppendLine(string.Join(",", values.Select(a => $"[{a}]")));
        }

        var line = includeStyling
            ? $"<span class=\"more-rows-message\">--{rows.Count - rowsCount} more row(s) not shown--</span>"
            : $"--{rows.Count - rowsCount} more row(s) not shown--";
        
        if (rows.Count > rowsCount)
            builder.AppendLine(line);

        return builder.ToString();
    }
}