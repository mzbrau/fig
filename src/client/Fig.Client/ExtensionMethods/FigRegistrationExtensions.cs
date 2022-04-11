using System;
using System.Threading.Tasks;
using Fig.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fig.Client.ExtensionMethods
{
    public static class FigRegistrationExtensions
    {
        public static async Task<IServiceCollection> AddFig<TService, TImplementation>(
            this IServiceCollection services, 
            FigOptions options = null, 
            Action<string> logger = null)
            where TService : class
            where TImplementation : SettingsBase, TService
        {
            if (options == null)
            {
                options = new FigOptions();
                options.ReadUriFromEnvironmentVariable("FIG_API_ADDRESS");
            }

            var theLogger = logger ?? Console.WriteLine;
            var provider = new FigConfigurationProvider(options, theLogger);
            var settings = await provider.Initialize<TImplementation>();

            services.AddSingleton<TService>(a => settings);

            return services;
        }
    }
}