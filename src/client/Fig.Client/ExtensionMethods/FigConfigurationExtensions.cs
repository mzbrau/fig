using Microsoft.Extensions.Configuration;
using System;
using Fig.Client.Configuration;
using Fig.Client.ConfigurationProvider;

namespace Fig.Client.ExtensionMethods;

public static class FigConfigurationExtensions
{
    public static IConfigurationBuilder AddFig<T>(this IConfigurationBuilder builder, Action<FigOptions> configure)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = new FigOptions();
        configure(options);

        var remoteBuilder = new FigConfigurationBuilder(builder, options, typeof(T));
        return remoteBuilder;
    }
}