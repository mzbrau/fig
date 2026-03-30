using System.Globalization;
using System.Security.Cryptography;
using Fig.Api.Converters;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        return ApplySettings(client, settings ?? new List<SettingValueExportDataContract>());
    }

    public List<ChangedSetting> ApplySettings(SettingClientBusinessEntity client, List<SettingValueExportDataContract> settings, string? customDecryptionKey = null)
    {
        var timeChangesMade = DateTime.UtcNow;
        var changes = new List<ChangedSetting>();
        foreach (var setting in client.Settings)
        {
            var match = settings.FirstOrDefault(a => a.Name == setting.Name);
            DecryptValue(match, setting, customDecryptionKey);

            if (match is not null)
            {
                setting.IsExternallyManaged = match.IsExternallyManaged == true;
            }
            
            if (match?.Value != null &&
                !AreJsonEquivalence(match.Value, setting.Value?.GetValue()))
            {
                try
                {
                    var dataContract = ValueDataContractFactory.CreateContract(match.Value, setting.ValueType ?? typeof(object));
                    var newValue = _settingConverter.Convert(dataContract);
                    changes.Add(new ChangedSetting(setting.Name,
                        setting.Value,
                        newValue,
                        setting.IsSecret,
                        setting.GetDataGridDefinition(), false)); // this is only for user updates. Externally managed settings are managed via import.
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

    private void DecryptValue(SettingValueExportDataContract? settingValue, SettingBusinessEntity setting, string? customDecryptionKey = null)
    {
        if (settingValue is null || !settingValue.IsEncrypted)
            return;

        var dataGridDefinition = setting.GetDataGridDefinition();
        if (dataGridDefinition is not null && dataGridDefinition.Columns.Any(a => a.IsSecret))
        {
            DecryptDataGridValue(settingValue, dataGridDefinition, customDecryptionKey);
        }
        else
        {
            var encryptedText = System.Convert.ToString(settingValue.Value, CultureInfo.InvariantCulture);
            
            try
            {
                settingValue.Value = _encryptionService.Decrypt(encryptedText);
            }
            catch (Exception) when (customDecryptionKey is not null)
            {
                settingValue.Value = _encryptionService.DecryptWithCustomKey(encryptedText, customDecryptionKey);
            }
        }
    }

    private void DecryptDataGridValue(SettingValueExportDataContract settingValue, DataGridDefinitionDataContract dataGridDefinition, string? customDecryptionKey = null)
    {
        var rows = settingValue.Value switch
        {
            JArray jArray => jArray.ToObject<List<Dictionary<string, object?>>>(),
            List<Dictionary<string, object?>> list => list,
            _ => null
        };
        
        if (rows is null)
            return;

        foreach (var column in dataGridDefinition.Columns.Where(a => a.IsSecret))
        {
            foreach (var row in rows)
            {
                if (row.TryGetValue(column.Name, out var columnValue) && columnValue is not null)
                {
                    try
                    {
                        row[column.Name] = _encryptionService.Decrypt(columnValue.ToString());
                    }
                    catch (Exception ex) when (customDecryptionKey is not null)
                    {
                        try
                        {
                            row[column.Name] = _encryptionService.DecryptWithCustomKey(columnValue.ToString(), customDecryptionKey);
                        }
                        catch (Exception)
                        {
                            _logger.LogError(ex, "Unable to decrypt column '{ColumnName}' in DataGrid setting '{SettingName}'", column.Name, settingValue.Name);
                            throw new InvalidImportException(
                                $"Unable to decrypt column '{column.Name}' in DataGrid setting '{settingValue.Name}'. " +
                                "It might have been encrypted with a different encryption key.");
                        }
                    }
                    catch (Exception ex) when (ex is CryptographicException or FormatException)
                    {
                        _logger.LogError(ex, "Unable to decrypt column '{ColumnName}' in DataGrid setting '{SettingName}'", column.Name, settingValue.Name);
                        throw new InvalidImportException(
                            $"Unable to decrypt column '{column.Name}' in DataGrid setting '{settingValue.Name}'. " +
                            "It might have been encrypted with a different encryption key.");
                    }
                }
            }
        }
        
        settingValue.Value = rows;
    }

    private bool AreJsonEquivalence<T>(T a, T b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        var aToken = a is JToken at ? at : JToken.FromObject(a);
        var bToken = b is JToken bt ? bt : JToken.FromObject(b);

        return JToken.DeepEquals(aToken, bToken);
    }
}