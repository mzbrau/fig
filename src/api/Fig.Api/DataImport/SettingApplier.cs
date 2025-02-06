using System.Globalization;
using Fig.Api.Converters;
using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.DataImport;

public class SettingApplier : ISettingApplier
{
    private readonly ISettingConverter _settingConverter;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SettingApplier> _logger;

    public SettingApplier(ISettingConverter settingConverter, IEncryptionService encryptionService, ILogger<SettingApplier> logger)
    {
        _settingConverter = settingConverter;
        _encryptionService = encryptionService;
        _logger = logger;
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
            DecryptValue(match);
            
            if (match?.Value != null &&
                !AreJsonEquivalence(match.Value, setting.Value?.GetValue()))
            {
                try
                {
                    var dataContract = ValueDataContractFactory.CreateContract(match.Value, setting.ValueType);
                    var newValue = _settingConverter.Convert(dataContract);
                    changes.Add(new ChangedSetting(setting.Name,
                        setting.Value,
                        newValue,
                        setting.IsSecret,
                        setting.GetDataGridDefinition()));
                    setting.Value = newValue;
                    setting.LastChanged = timeChangesMade;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to import new value for {SettingName}", setting.Name);
                }
            }
        }

        return changes;
    }

    private void DecryptValue(SettingValueExportDataContract? settingValue)
    {
        if (settingValue is null || !settingValue.IsEncrypted)
            return;

        settingValue.Value = _encryptionService.Decrypt(System.Convert.ToString(settingValue.Value, CultureInfo.InvariantCulture));
    }

    private bool AreJsonEquivalence<T>(T a, T b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;
        
        var aJson = JsonConvert.SerializeObject(a, JsonSettings.FigDefault);
        var bJson = JsonConvert.SerializeObject(b, JsonSettings.FigDefault);

        return aJson == bJson;
    }
}