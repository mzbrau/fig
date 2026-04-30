using System.Globalization;
using System.Security.Cryptography;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Contracts;
using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json;

namespace Fig.Api.Converters;

public class ClientExportConverter : IClientExportConverter
{
    private readonly IEncryptionService _encryptionService;
    private readonly ISettingConverter _settingConverter;

    public ClientExportConverter(IEncryptionService encryptionService, ISettingConverter settingConverter)
    {
        _encryptionService = encryptionService;
        _settingConverter = settingConverter;
    }

    public SettingClientExportDataContract Convert(SettingClientBusinessEntity client)
    {
        return new SettingClientExportDataContract(client.Name,
            client.Description,
            client.ClientSecret,
            client.Instance,
            client.Settings
                .OrderBy(a => a.Name).Select(Convert).ToList());
    }    public SettingClientValueExportDataContract ConvertValueOnly(SettingClientBusinessEntity client, bool excludeEnvironmentSpecific = false)
    {
        var settings = client.Settings.OrderBy(a => a.Name).AsEnumerable();
        
        if (excludeEnvironmentSpecific)
        {
            settings = settings.Where(s => s.EnvironmentSpecific != true);
        }
        
        return new SettingClientValueExportDataContract(
            client.Name,
            client.Instance,
            settings.Select(ConvertValueOnlySetting).ToList());
    }

    private SettingValueExportDataContract ConvertValueOnlySetting(SettingBusinessEntity setting)
    {
        var value = setting.Value?.GetValue();
        var isEncrypted = false;
        if (setting.IsSecret && value is not null)
        {
            value = _encryptionService.Encrypt(System.Convert.ToString(value, CultureInfo.InvariantCulture));
            isEncrypted = true;
        }
        else
        {
            var dataGridDefinition = setting.GetDataGridDefinition();
            if (dataGridDefinition is not null && dataGridDefinition.Columns.Any(a => a.IsSecret))
            {
                var dataGridValue = value as List<Dictionary<string, object?>>;
                foreach (var column in dataGridDefinition.Columns.Where(a => a.IsSecret))
                {
                    foreach (var row in dataGridValue ?? [])
                    {
                        if (row.TryGetValue(column.Name, out var columnValue) && columnValue is not null)
                        {
                            row[column.Name] = _encryptionService.Encrypt(System.Convert.ToString(columnValue, CultureInfo.InvariantCulture));
                        }
                    }
                }
                isEncrypted = true;
            }
        }
        
        return new SettingValueExportDataContract(
            setting.Name,
            value,
            isEncrypted,
            setting.IsExternallyManaged ? true : null,
            setting.InitOnlyExport == true ? true : null);
    }

    public SettingClientBusinessEntity Convert(SettingClientExportDataContract client, string? customDecryptionKey = null)
    {
        return new SettingClientBusinessEntity
        {
            Name = client.Name,
            Description = client.Description,
            ClientSecret = client.ClientSecret,
            Instance = client.Instance,
            LastRegistration = DateTime.UtcNow,
            Settings = client.Settings.Select(s => Convert(s, customDecryptionKey)).ToList()
        };
    }

    private SettingBusinessEntity Convert(SettingExportDataContract setting, string? customDecryptionKey = null)
    {
        var dataGridDefinition = setting.DataGridDefinitionJson is null
            ? null
            : JsonConvert.DeserializeObject<DataGridDefinitionDataContract>(setting.DataGridDefinitionJson);
        SettingValueBaseBusinessEntity? value;
        if (setting is { IsEncrypted: true, Value: StringSettingDataContract strValue })
        {
            value = _settingConverter.Convert(GetDecryptedValue(strValue, setting.ValueType, setting.Name, customDecryptionKey));
        }
        else 
        {
            if (setting.IsEncrypted && dataGridDefinition is not null && dataGridDefinition.Columns.Any(a => a.IsSecret))
            {
                var dataGridValue = setting.Value?.GetValue() as List<Dictionary<string, object?>>;
                foreach (var column in dataGridDefinition.Columns.Where(a => a.IsSecret))
                {
                    foreach (var row in dataGridValue ?? [])
                    {
                        if (row.TryGetValue(column.Name, out var columnValue) && columnValue is not null)
                        {
                            row[column.Name] = DecryptColumnValue(columnValue.ToString(), customDecryptionKey, setting.Name, column.Name);
                        }
                    }
                }
            }
            
            value = _settingConverter.Convert(setting.Value);
        }

        return new SettingBusinessEntity
        {
            Name = setting.Name,
            Description = setting.Description,
            IsSecret = setting.IsSecret,
            ValueType = setting.ValueType,
            Value = value,
            DefaultValue = _settingConverter.Convert(setting.DefaultValue),
            JsonSchema = setting.JsonSchema,
            ValidationRegex = setting.ValidationRegex,
            ValidationExplanation = setting.ValidationExplanation,
            ValidValues = setting.ValidValues,
            Group = setting.Group,
            DisplayOrder = setting.DisplayOrder,
            Advanced = setting.Advanced,
            LookupTableKey = setting.LookupTableKey,
            EditorLineCount = setting.EditorLineCount,
            DataGridDefinitionJson = setting.DataGridDefinitionJson,
            EnablesSettings = setting.EnablesSettings,
            SupportsLiveUpdate = setting.SupportsLiveUpdate,
            LastChanged = setting.LastChanged,
            CategoryColor = setting.CategoryColor,
            CategoryName = setting.CategoryName,
            DisplayScript = setting.DisplayScript,
            IsExternallyManaged = setting.IsExternallyManaged,
            Classification = setting.Classification,
            EnvironmentSpecific = setting.EnvironmentSpecific,
            LookupKeySettingName = setting.LookupKeySettingName,
            Indent = setting.Indent,
            DependsOnProperty = setting.DependsOnProperty,
            DependsOnValidValues = setting.DependsOnValidValues,
            InitOnlyExport = setting.InitOnlyExport,
            MigrateFrom = setting.MigrateFrom,
            Heading = setting.Heading != null ? new HeadingDataContract(
                setting.Heading.Text,
                setting.Heading.Color,
                setting.Heading.Advanced) : null
        };
    }

    private SettingExportDataContract Convert(SettingBusinessEntity setting)
    {
        var dataGridDefinition = setting.GetDataGridDefinition();
        var value = _settingConverter.Convert(setting.Value, setting.HasSchema());
        var isEncrypted = false;
        if (setting.IsSecret && value?.GetValue() is not null)
        {
            var encryptedValue = _encryptionService.Encrypt(System.Convert.ToString(value.GetValue(), CultureInfo.InvariantCulture));
            value = new StringSettingDataContract(encryptedValue);
            isEncrypted = true;
        }
        else if (dataGridDefinition is not null && dataGridDefinition.Columns.Any(a => a.IsSecret))
        {
            var dataGridValue = value?.GetValue() as List<Dictionary<string, object?>>;
            foreach (var column in dataGridDefinition.Columns.Where(a => a.IsSecret))
            {
                foreach (var row in dataGridValue ?? [])
                {
                    if (row.TryGetValue(column.Name, out var columnValue) && columnValue is not null)
                    {
                        row[column.Name] = _encryptionService.Encrypt(System.Convert.ToString(columnValue, CultureInfo.InvariantCulture));
                    }
                }
            }
            isEncrypted = true;
        }return new SettingExportDataContract(
            setting.Name,
            setting.Description,
            setting.IsSecret,
            setting.ValueType ?? typeof(object),
            value,
            _settingConverter.Convert(setting.DefaultValue, setting.HasSchema(), dataGridDefinition),
            isEncrypted,
            setting.JsonSchema,
            setting.ValidationRegex,
            setting.ValidationExplanation,
            setting.ValidValues,
            setting.Group,
            setting.DisplayOrder,
            setting.Advanced,
            setting.LookupTableKey,
            setting.EditorLineCount,
            setting.DataGridDefinitionJson,
            setting.EnablesSettings,
            setting.SupportsLiveUpdate,
            setting.LastChanged,
            setting.CategoryColor,
            setting.CategoryName,
            setting.DisplayScript,
            setting.IsExternallyManaged,
            setting.Classification,
            setting.EnvironmentSpecific,
            setting.LookupKeySettingName,
            setting.Indent,
            setting.DependsOnProperty,
            setting.DependsOnValidValues,
            setting.Heading != null ? new HeadingExportDataContract(
                setting.Heading.Text,
                setting.Heading.Color,
                setting.Heading.Advanced) : null,
            setting.InitOnlyExport,
            setting.MigrateFrom);
    }

    private SettingValueBaseDataContract? GetDecryptedValue(StringSettingDataContract settingValue, Type type, string settingName, string? customDecryptionKey = null)
    {
        try
        {
            var value = _encryptionService.DecryptForImport(settingValue.Value, customDecryptionKey);
            return value is null ? null : ValueDataContractFactory.CreateContract(value, type);
        }
        catch (Exception ex) when (ex is InvalidPasswordException or CryptographicException or FormatException)
        {
            throw new InvalidPasswordException($"Unable to decrypt password for setting {settingName}", ex);
        }
    }

    private string? DecryptColumnValue(string? encryptedValue, string? customDecryptionKey, string settingName, string columnName)
    {
        if (encryptedValue is null)
            return null;

        try
        {
            return _encryptionService.DecryptForImport(encryptedValue, customDecryptionKey);
        }
        catch (Exception ex) when (ex is InvalidPasswordException or CryptographicException or FormatException)
        {
            throw new InvalidPasswordException($"Unable to decrypt column '{columnName}' in setting '{settingName}'", ex);
        }
    }
}
