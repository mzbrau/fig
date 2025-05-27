using System;
using Microsoft.Extensions.Configuration;

namespace Fig.Client.IntegrationTest;

public class ReloadableConfigurationSource<T> : IConfigurationSource
{
    public ConfigReloader<T> ConfigReloader { get; set; } = default!;
    
    public Type SettingsType { get; set; } = default!;
    
    public T? InitialConfiguration { get; set; }

    public string? SectionNameOverride { get; set; }
    
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new ReloadableConfigurationProvider<T>(this, SectionNameOverride);
    }
}