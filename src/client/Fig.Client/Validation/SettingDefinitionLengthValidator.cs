using Fig.Client.Exceptions;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client.Validation;

internal static class SettingDefinitionLengthValidator
{
    public static void ValidateMaxLength(string? value, int maxLength, string fieldLabel)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return;

        throw new InvalidSettingException(
            $"{fieldLabel} exceeds maximum length of {maxLength} characters (was {value.Length}).");
    }

    public static void Validate(SettingDefinitionDataContract setting)
    {
        var max = SettingDefinitionFieldLimits.StandardString;
        var name = setting.Name;

        ValidateMaxLength(name, max, $"Setting name '{name}'");
        ValidateMaxLength(setting.Group, max, $"[Group] on '{name}': GroupName");
        ValidateMaxLength(setting.LookupTableKey, max, $"[LookupTable] on '{name}': LookupTableKey");
        ValidateMaxLength(setting.LookupKeySettingName, max, $"[LookupTable] on '{name}': KeySettingName");
        ValidateMaxLength(setting.CategoryName, max, $"[Category] on '{name}': Name");
        ValidateMaxLength(setting.CategoryColor, max, $"[Category] on '{name}': ColorHex");
        ValidateMaxLength(setting.DependsOnProperty, max, $"[DependsOn] on '{name}': DependsOnProperty");
        ValidateMaxLength(setting.MigrateFrom, max, $"[MigrateFrom] on '{name}': PreviousSettingName");
        ValidateMaxLength(setting.MigrateFromMigrationMethod, max, $"[MigrateFrom] on '{name}': MigrationMethodName");

        if (setting.DataGridDefinition?.Columns == null)
            return;

        foreach (var column in setting.DataGridDefinition.Columns)
        {
            ValidateMaxLength(column.Name, max, $"DataGrid column name '{column.Name}' on '{name}'");
        }
    }

    public static void ValidateClientMetadata(string clientName, string? instance)
    {
        var max = SettingDefinitionFieldLimits.StandardString;
        ValidateMaxLength(clientName, max, "Client name");
        ValidateMaxLength(instance, max, "Client instance");
    }
}
