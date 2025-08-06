using System;
using Fig.Client.Testing.Integration;
using Microsoft.Extensions.Configuration;

namespace Fig.Client.Testing.Extensions;

public static class FigConfigurationExtensionMethods
{
    public static IConfigurationBuilder AddIntegrationTestConfiguration<T>(this IConfigurationBuilder builder, ConfigReloader<T>? configReloader = null, T? initialConfiguration = null)
        where T : SettingsBase
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        
        var source = new ReloadableConfigurationSource<T>
        {
            ConfigReloader = configReloader ?? new ConfigReloader<T>(),
            SettingsType = typeof(T),
            InitialConfiguration = initialConfiguration
        };
        builder.Add(source);
        return builder;
    }
}