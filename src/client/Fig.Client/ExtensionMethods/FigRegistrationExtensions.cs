using System;
using System.Threading.Tasks;
using Fig.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fig.Client.ExtensionMethods
{
    public static class FigRegistrationExtensions
    {
        public static async Task<IServiceCollection> AddFig<TService, TImplementation>(
            this IServiceCollection services,
            ILogger logger,
            Action<FigOptions>? options = null)
            where TService : class
            where TImplementation : SettingsBase, TService
        {
            new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("fig").Bind(options);

            services.Configure(options);

            var figOptions = new FigOptions();
            options?.Invoke(figOptions);

            if (figOptions.ApiUri == null)
            {
                figOptions.ReadUriFromEnvironmentVariable();
            }

            var provider = new FigConfigurationProvider(logger, figOptions);
            var settings = await provider.Initialize<TImplementation>();

            services.AddSingleton<TService>(a => settings);

            return services;
        }
    }
}