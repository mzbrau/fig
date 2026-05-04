using Fig.Contracts.Settings;

namespace Fig.Contracts.SettingMigrations;

public class SettingMigrationResultDataContract
{
    public SettingMigrationResultDataContract(
        string sourceSettingName,
        string targetSettingName,
        string? instance,
        SettingValueBaseDataContract? migratedValue,
        string sourceValueFingerprint)
    {
        SourceSettingName = sourceSettingName;
        TargetSettingName = targetSettingName;
        Instance = instance;
        MigratedValue = migratedValue;
        SourceValueFingerprint = sourceValueFingerprint;
    }

    public string SourceSettingName { get; set; }

    public string TargetSettingName { get; set; }

    public string? Instance { get; set; }

    public SettingValueBaseDataContract? MigratedValue { get; set; }

    public string SourceValueFingerprint { get; set; }
}
