using Fig.Api.Converters;
using Fig.Api.Utils;
using Fig.Common;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json;

namespace Fig.Api.DataImport;

public class DeferredSettingApplier : IDeferredSettingApplier
{
    private readonly ISettingConverter _settingConverter;

    public DeferredSettingApplier(ISettingConverter settingConverter)
    {
        _settingConverter = settingConverter;
    }
    
    public List<ChangedSetting> ApplySettings(SettingClientBusinessEntity client, DeferredClientImportBusinessEntity data)
    {
        var settings = JsonConvert.DeserializeObject<List<SettingValueExportDataContract>>(data.SettingValuesAsJson, JsonSettings.FigDefault);
        return ApplySettings(client, settings);
    }

    public List<ChangedSetting> ApplySettings(SettingClientBusinessEntity client, List<SettingValueExportDataContract> settings)
    {
        var timeChangesMade = DateTime.UtcNow;
        var changes = new List<ChangedSetting>();
        foreach (var setting in client.Settings)
        {
            var match = settings.FirstOrDefault(a => a.Name == setting.Name);
            
            if (match?.Value != null &&
                (match.Value?.GetType()).FigPropertyType() == setting.ValueType.FigPropertyType() &&
                JsonConvert.SerializeObject(match.Value, JsonSettings.FigDefault) != JsonConvert.SerializeObject(setting.Value?.GetValue(), JsonSettings.FigDefault))
            {
                var newValue = GetNewValue(match.Value);
                changes.Add(new ChangedSetting(setting.Name,
                    setting.Value,
                    newValue,
                    setting.IsSecret));
                setting.Value = newValue;
                setting.LastChanged = timeChangesMade;
            }
        }

        return changes;

        SettingValueBaseBusinessEntity? GetNewValue(object? match)
        {
            if (match is null)
                return null;

            var dataContract = ValueDataContractFactory.CreateContract(match, match.GetType());
            return _settingConverter.Convert(dataContract);
        }
    }
}