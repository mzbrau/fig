using System;
using Fig.Contracts.Settings;

namespace Fig.Contracts.SettingMigrations;

public class SettingMigrationRequestDataContract
{
    public SettingMigrationRequestDataContract(
        string sourceSettingName,
        string targetSettingName,
        string? instance,
        Type? sourceValueType,
        Type? targetValueType,
        SettingValueBaseDataContract? sourceValue,
        bool sourceIsSecret,
        bool targetIsSecret,
        string sourceValueFingerprint)
    {
        SourceSettingName = sourceSettingName;
        TargetSettingName = targetSettingName;
        Instance = instance;
        SourceValueType = sourceValueType;
        TargetValueType = targetValueType;
        SourceValue = sourceValue;
        SourceIsSecret = sourceIsSecret;
        TargetIsSecret = targetIsSecret;
        SourceValueFingerprint = sourceValueFingerprint;
    }

    public string SourceSettingName { get; set; }

    public string TargetSettingName { get; set; }

    public string? Instance { get; set; }

    public Type? SourceValueType { get; set; }

    public Type? TargetValueType { get; set; }

    public SettingValueBaseDataContract? SourceValue { get; set; }

    public bool SourceIsSecret { get; set; }

    public bool TargetIsSecret { get; set; }

    public string SourceValueFingerprint { get; set; }
}
