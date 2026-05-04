using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Fig.Client.DefaultValue;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingMigrations;
using Fig.Contracts.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Client.Migration;

internal class MigrateFromMigrationConverter
{
    private static readonly JsonSerializerSettings SafeJsonSettings = new()
    {
        Culture = CultureInfo.InvariantCulture,
        TypeNameHandling = TypeNameHandling.None
    };

    private readonly IDataGridDefaultValueProvider _dataGridDefaultValueProvider;

    public MigrateFromMigrationConverter()
        : this(new DataGridDefaultValueProvider())
    {
    }

    internal MigrateFromMigrationConverter(IDataGridDefaultValueProvider dataGridDefaultValueProvider)
    {
        _dataGridDefaultValueProvider = dataGridDefaultValueProvider;
    }

    public List<SettingMigrationResultDataContract> Convert(
        SettingsClientDefinitionDataContract settings,
        IEnumerable<SettingMigrationRequestDataContract> requests)
    {
        var targetSettings = settings.Settings
            .Where(setting => setting.MigrateFromMigrationMethodInfo is not null)
            .ToDictionary(setting => setting.Name, StringComparer.Ordinal);

        var results = new List<SettingMigrationResultDataContract>();
        foreach (var request in requests)
        {
            if (!targetSettings.TryGetValue(request.TargetSettingName, out var targetSetting))
                continue;

            if (request.SourceIsSecret && !request.TargetIsSecret)
            {
                throw new InvalidOperationException(
                    $"Custom MigrateFrom migration from secret setting '{request.SourceSettingName}' " +
                    $"to non-secret setting '{request.TargetSettingName}' is not allowed.");
            }

            var method = targetSetting.MigrateFromMigrationMethodInfo!;
            var migratedValue = InvokeMigrationMethod(method, request.SourceValue);
            var contractValue = ConvertResultToContract(migratedValue, targetSetting);

            results.Add(new SettingMigrationResultDataContract(
                request.SourceSettingName,
                request.TargetSettingName,
                request.Instance,
                contractValue,
                request.SourceValueFingerprint));
        }

        return results;
    }

    private static object? InvokeMigrationMethod(MethodInfo method, SettingValueBaseDataContract? sourceValue)
    {
        var parameterType = method.GetParameters()[0].ParameterType;
        var parameter = ConvertParameter(sourceValue?.GetValue(), parameterType);

        try
        {
            return method.Invoke(null, [parameter]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"MigrateFrom migration method '{method.Name}' failed: {ex.InnerException.Message}",
                ex.InnerException);
        }
    }

    private SettingValueBaseDataContract ConvertResultToContract(object? migratedValue, SettingDefinitionDataContract targetSetting)
    {
        if (targetSetting.DataGridDefinition is not null)
        {
            if (migratedValue is List<Dictionary<string, object?>> rows)
                return new DataGridSettingDataContract(rows);

            var dataGridValue = _dataGridDefaultValueProvider.Convert(migratedValue, targetSetting.DataGridDefinition.Columns);
            if (dataGridValue is null)
            {
                throw new InvalidOperationException(
                    $"Migration result for setting '{targetSetting.Name}' could not be converted to a data grid value.");
            }

            return new DataGridSettingDataContract(dataGridValue);
        }

        if (!string.IsNullOrWhiteSpace(targetSetting.JsonSchema))
        {
            var json = migratedValue as string ??
                       JsonConvert.SerializeObject(migratedValue, Formatting.Indented, SafeJsonSettings);
            return new JsonSettingDataContract(json);
        }

        var targetType = targetSetting.ValueType ?? migratedValue?.GetType();
        if (targetType is null)
            return new StringSettingDataContract(null);

        return ValueDataContractFactory.CreateContract(migratedValue, targetType);
    }

    private static object? ConvertParameter(object? value, Type parameterType)
    {
        if (parameterType == typeof(object))
            return value;

        if (value is null)
        {
            if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null)
                throw new InvalidOperationException($"Cannot pass null to non-nullable migration parameter type {parameterType.FullName}.");

            return null;
        }

        var targetType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        if (targetType.IsInstanceOfType(value))
            return value;

        if (value is JToken token)
            return token.ToObject(targetType, JsonSerializer.Create(SafeJsonSettings));

        if (targetType.IsEnum())
        {
            var enumStringValue = System.Convert.ToString(value, CultureInfo.InvariantCulture)!;
            try
            {
                return Enum.Parse(targetType, enumStringValue, ignoreCase: true);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException(
                    $"Cannot convert value '{enumStringValue}' to enum type '{targetType.FullName}'. " +
                    $"Valid values are: {string.Join(", ", Enum.GetNames(targetType))}");
            }
        }

        if (value is string stringValue)
        {
            if (targetType == typeof(string))
                return stringValue;

            var figPropertyType = targetType.FigPropertyType();
            if (figPropertyType is not FigPropertyType.Unsupported and not FigPropertyType.DataGrid and not FigPropertyType.StringList)
                return ConvertSimpleValue(stringValue, targetType);

            return JsonConvert.DeserializeObject(stringValue, targetType, SafeJsonSettings);
        }

        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(value.GetType()))
            return converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);

        return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static object? ConvertSimpleValue(string value, Type targetType)
    {
        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(typeof(string)))
            return converter.ConvertFromInvariantString(value);

        return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}
