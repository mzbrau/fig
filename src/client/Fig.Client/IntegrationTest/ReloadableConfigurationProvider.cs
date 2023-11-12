using System;
using Fig.Client.Configuration;
using Fig.Client.Parsers;
using Newtonsoft.Json;

namespace Fig.Client.IntegrationTest;

public class ReloadableConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider, IDisposable
{
    private readonly ReloadableConfigurationSource _source;

    public ReloadableConfigurationProvider(ReloadableConfigurationSource source)
    {
        _source = source;
        _source.ConfigReloader.ConfigurationUpdated += OnConfigurationUpdated;

        var settings = _source.InitialConfiguration ?? (SettingsBase)Activator.CreateInstance(source.SettingsType);
        UpdateSettings(settings);
    }

    private void OnConfigurationUpdated(object sender, ConfigurationUpdatedEventArgs args)
    {
        UpdateSettings(args.Settings);
    }

    private void UpdateSettings(SettingsBase settings)
    {
        var configurationSections = settings.GetConfigurationSections();
        var value = JsonConvert.SerializeObject(settings);
        var parser = new JsonValueParser();
        foreach (var kvp in parser.ParseJsonValue(value))
        {
            CustomConfigurationSection? configurationSection = null;
            if (configurationSections.TryGetValue(kvp.Key, out var section))
                configurationSection = section;
            
            Data[kvp.Key] = kvp.Value;
            if (!string.IsNullOrEmpty(configurationSection?.SectionName))
            {
                // If the configuration setting value is set, we set it in both places.
                Data[$"{configurationSection!.SectionName}:{configurationSection.SettingNameOverride ?? kvp.Key}"] = kvp.Value;
            }
        }
    }

    public void Dispose()
    {
        _source.ConfigReloader.ConfigurationUpdated -= OnConfigurationUpdated;
    }
}