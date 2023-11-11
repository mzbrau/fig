using System;
using System.Linq;
using Fig.Client.Attributes;
using Fig.Client.Configuration;
using Fig.Client.ExtensionMethods;
using Fig.Client.Parsers;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Fig.Client.IntegrationTest;

public class ReloadableConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider, IDisposable
{
    private readonly ReloadableConfigurationSource _source;

    public ReloadableConfigurationProvider(ReloadableConfigurationSource source)
    {
        _source = source;
        _source.ConfigReloader.ConfigurationUpdated += OnConfigurationUpdated;

        var settings = _source.InitialValue ?? (SettingsBase)Activator.CreateInstance(source.SettingsType);
        UpdateSettings2(settings);
    }

    private void OnConfigurationUpdated(object sender, ConfigurationUpdatedEventArgs args)
    {
        UpdateSettings2(args.Settings);
    }

    private void UpdateSettings2(SettingsBase settings)
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
                Data[$"{configurationSection!.SectionName}:{kvp.Key}"] = value;
            }
        }
    }

    private void UpdateSettings(SettingsBase settings)
    {
        var configurationSections = settings.GetConfigurationSections();
        foreach (var property in settings.GetType().GetProperties()
                     .Where(a => a.PropertyType.IsSupportedBaseType()))
        {
            CustomConfigurationSection? configurationSection = null;
            if (configurationSections.TryGetValue(property.Name, out var section))
                configurationSection = section;

            var value = property.GetValue(settings)?.ToString() ?? string.Empty;

            Data[property.Name] = value;
            if (!string.IsNullOrEmpty(configurationSection?.SectionName))
            {
                // If the configuration setting value is set, we set it in both places.
                Data[$"{configurationSection!.SectionName}:{configurationSection.SettingNameOverride ?? property.Name}"] = value;
            }
        }

        var props = settings.GetType().GetProperties();
        
        foreach (var property in settings.GetType().GetProperties()
                     .Where(a => !a.PropertyType.IsSupportedBaseType() && a.GetCustomAttributes(false).Any(b => b is SettingAttribute)))
        {
            CustomConfigurationSection? configurationSection = null;
            if (configurationSections.TryGetValue(property.Name, out var section))
                configurationSection = section;

            var test = property.GetType().IsSupportedBaseType();
            
            var value = JsonSerializer.Serialize(property.GetValue(settings));
            var parser = new JsonValueParser();
            foreach (var kvp in parser.ParseJsonValue(value))
            {
                var key = ConfigurationPath.Combine(property.Name, kvp.Key);
                Data[key] = kvp.Value;
                if (!string.IsNullOrEmpty(configurationSection?.SectionName))
                {
                    // If the configuration setting value is set, we set it in both places.
                    Data[$"{configurationSection!.SectionName}:{key}"] = value;
                }
            }
        }

        OnReload();
    }

    public void Dispose()
    {
        _source.ConfigReloader.ConfigurationUpdated -= OnConfigurationUpdated;
    }
}