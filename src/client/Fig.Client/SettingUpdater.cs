using System;
using Microsoft.Extensions.DependencyInjection;

namespace Fig.Client;

public class SettingUpdater : ISettingUpdater
{
    private readonly IServiceProvider _serviceProvider;

    public SettingUpdater(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ISettingUpdater<TSettings> For<TSettings>() where TSettings : SettingsBase
    {
        return _serviceProvider.GetRequiredService<ISettingUpdater<TSettings>>();
    }
}
