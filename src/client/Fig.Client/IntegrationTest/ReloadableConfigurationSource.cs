using System;
using Microsoft.Extensions.Configuration;

namespace Fig.Client.IntegrationTest;

public class ReloadableConfigurationSource : IConfigurationSource
{
    public ConfigReloader ConfigReloader { get; set; } = default!;
    
    public Type SettingsType { get; set; } = default!;
    
    public SettingsBase? InitialValue { get; set; }
    
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new ReloadableConfigurationProvider(this);
    }
}