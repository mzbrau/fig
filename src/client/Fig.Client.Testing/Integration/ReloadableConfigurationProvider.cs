using System;
using System.Collections.Generic;
using Fig.Client.Configuration;
using Fig.Client.ExtensionMethods;
using Fig.Client.Parsers;
using Newtonsoft.Json;

namespace Fig.Client.Testing.Integration;

public class ReloadableConfigurationProvider<T> : Microsoft.Extensions.Configuration.ConfigurationProvider, IDisposable
{
    private readonly ReloadableConfigurationSource<T> _source;
    private readonly string? _sectionNameOverride;

    public ReloadableConfigurationProvider(ReloadableConfigurationSource<T> source, string? sectionNameOverride = null)
    {
        _source = source;
        _sectionNameOverride = sectionNameOverride;
        _source.ConfigReloader.ConfigurationUpdated += OnConfigurationUpdated;

        var settings = _source.InitialConfiguration ?? (T)Activator.CreateInstance(source.SettingsType);
        UpdateSettings(settings);
    }

    private void OnConfigurationUpdated(object sender, ConfigurationUpdatedEventArgs<T> args)
    {
        UpdateSettings(args.Settings);
    }

    private void UpdateSettings(T settings)
    {
        Dictionary<string, List<CustomConfigurationSection>> configurationSections = new();
        if (settings is SettingsBase settingsBase)
        {
            configurationSections = settingsBase.GetConfigurationSections();
            settingsBase.OverrideCollectionDefaultValues();
        }

        var value = JsonConvert.SerializeObject(settings);
        var parser = new JsonValueParser();
        Data.Clear();
        var sectionOverride = string.IsNullOrWhiteSpace(_sectionNameOverride) ? string.Empty : $"{_sectionNameOverride}:";
        foreach (var kvp in parser.ParseJsonValue(value))
        {
            Data[$"{sectionOverride}{kvp.Key}"] = kvp.Value;

            // Add entries for each configuration section if they exist
            if (configurationSections.TryGetValue(kvp.Key, out var sections) && sections != null)
            {
                foreach (var section in sections)
                {
                    if (!string.IsNullOrEmpty(section.SectionName))
                    {
                        // We set both so that the fig property and the target property are both set correctly
                        Data[$"{section.SectionName}:{section.SettingNameOverride ?? kvp.Key}"] = kvp.Value;
                    }
                }
            }
        }

        Load();
        OnReload();
    }

    public void Dispose()
    {
        _source.ConfigReloader.ConfigurationUpdated -= OnConfigurationUpdated;
    }
}