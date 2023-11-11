using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Fig.Client.IntegrationTest;

public class ReloadableConfigurationBuilder : IConfigurationBuilder
{
    private readonly IConfigurationBuilder _configurationBuilder;
    private readonly ConfigReloader _configReloader;
    private readonly Type _settingsType;

    public IDictionary<string, object> Properties => _configurationBuilder.Properties;

    public IList<IConfigurationSource> Sources => _configurationBuilder.Sources;
    
    public ReloadableConfigurationBuilder(IConfigurationBuilder configurationBuilder, ConfigReloader configReloader, Type settingsType)
    {
        _configurationBuilder = configurationBuilder ?? throw new ArgumentNullException(nameof(configurationBuilder));
        _configReloader = configReloader ?? throw new ArgumentNullException(nameof(configReloader));
        _settingsType = settingsType;
    }

    public IConfigurationBuilder Add(IConfigurationSource source)
    {
        return _configurationBuilder.Add(source);
    }

    public IConfigurationRoot Build()
    {
        var source = new ReloadableConfigurationSource
        {
            ConfigReloader = _configReloader,
            SettingsType = _settingsType
        };
        
        Add(source);

        return _configurationBuilder.Build();
    }
}