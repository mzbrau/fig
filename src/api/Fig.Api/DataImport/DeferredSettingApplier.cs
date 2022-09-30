using Fig.Api.Utils;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.DataImport;

public class DeferredSettingApplier : IDeferredSettingApplier
{
    public List<ChangedSetting> ApplySettings(SettingClientBusinessEntity client, DeferredClientImportBusinessEntity data)
    {
        var settings = JsonConvert.DeserializeObject<List<SettingValueExportDataContract>>(data.SettingValuesAsJson);
        return ApplySettings(client, settings);
    }

    public List<ChangedSetting> ApplySettings(SettingClientBusinessEntity client, List<SettingValueExportDataContract> settings)
    {
        var changes = new List<ChangedSetting>();
        foreach (var setting in client.Settings)
        {
            var match = settings.FirstOrDefault(a => a.Name == setting.Name);
            if (match != null && 
                ((Type?) match.Value?.GetType())?.FigPropertyType() == setting.ValueType.FigPropertyType() &&
                JsonConvert.SerializeObject(match.Value) != JsonConvert.SerializeObject(setting.Value))
            {
                changes.Add(new ChangedSetting(setting.Name, setting.Value, match.Value, setting.ValueType,
                    setting.IsSecret));
                setting.Value = match.Value;
            }
        }

        return changes;
    }
}