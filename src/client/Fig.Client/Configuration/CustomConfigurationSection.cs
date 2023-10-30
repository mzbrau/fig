namespace Fig.Client.Configuration;

public class CustomConfigurationSection
{
    public CustomConfigurationSection(string sectionName, string? settingNameOverride)
    {
        SectionName = sectionName;
        SettingNameOverride = settingNameOverride;
    }

    public string SectionName { get; }

    public string? SettingNameOverride { get; }
}