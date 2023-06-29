using Fig.Api.SettingVerification.Plugin;
using Fig.Api.SettingVerification.Sdk;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fig.Api.SettingVerification.ExtensionMethods;

public static class VerificationPluginsExtensionMethods
{
    public static void AddSettingVerificationPlugins(this IServiceCollection services)
    {
        services.AddSingleton<IVerificationPluginFactory>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<VerificationPluginFactory>>();
            return new VerificationPluginFactory(GetPluginVerifiers(logger));
        });
    }

    private static IEnumerable<ISettingPluginVerifier> GetPluginVerifiers(ILogger<VerificationPluginFactory> logger)
    {
        var pluginsDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");
        logger.LogInformation($"Reading setting verification plugins from plugins directory ({pluginsDirectory})...");

        if (!Directory.Exists(pluginsDirectory))
        {
            logger.LogWarning("No plugin directory found. No verifiers will be loaded.");
            yield break;
        }

        foreach (var pluginDir in Directory.GetDirectories(pluginsDirectory))
        {
            var dirName = Path.GetFileName(pluginDir);
            var pluginFile = Path.Combine(pluginDir, dirName + ".dll");
            var loader = PluginLoader.CreateFromAssemblyFile(pluginFile,
                // this ensures that the plugin resolves to the same version of DependencyInjection
                // and ASP.NET Core that the current app uses
                new[]
                {
                    typeof(ISettingPluginVerifier),
                    typeof(VerificationResult)
                });
            foreach (var type in loader.LoadDefaultAssembly()
                         .GetTypes()
                         .Where(t => typeof(ISettingPluginVerifier).IsAssignableFrom(t) && !t.IsAbstract))
            {
                logger.LogInformation($"Creating verifier from type {type.Name}");
                ISettingPluginVerifier? verifier = null;
                try
                {
                    verifier = (ISettingPluginVerifier?) Activator.CreateInstance(type);
                    logger.LogInformation($"{type.Name} loaded successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error when trying to load {type.Name}. {ex}", ex);
                }

                if (verifier != null)
                    yield return verifier;
            }
        }
    }
}