using Fig.Api.SettingVerification.Sdk;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fig.Api.SettingVerification.ExtensionMethods;

public static class VerificationExtensionMethods
{
    public static void AddSettingVerifiers(this IServiceCollection services)
    {
        services.AddSingleton<IVerificationFactory>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<VerificationFactory>>();
            return new VerificationFactory(GetVerifiers(logger));
        });
    }

    private static IEnumerable<ISettingVerifier> GetVerifiers(ILogger<VerificationFactory> logger)
    {
        var pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "plugins");
        logger.LogInformation($"Reading setting verifications from plugins directory ({pluginsDirectory})...");

        if (!Directory.Exists(pluginsDirectory))
        {
            logger.LogWarning("No plugin directory found. No verifiers will be loaded.");
            yield break;
        }

        yield break;
        
        // TODO: This will be removed as part of removing verifiers.
        foreach (var pluginDir in Directory.GetDirectories(pluginsDirectory))
        {
            var dirName = Path.GetFileName(pluginDir);
            var pluginFile = Path.Combine(pluginDir, dirName + ".dll");
            var loader = PluginLoader.CreateFromAssemblyFile(pluginFile,
                // this ensures that the plugin resolves to the same version of DependencyInjection
                // and ASP.NET Core that the current app uses
                new[]
                {
                    typeof(ISettingVerifier),
                    typeof(VerificationResult)
                });
            foreach (var type in loader.LoadDefaultAssembly()
                         .GetTypes()
                         .Where(t => typeof(ISettingVerifier).IsAssignableFrom(t) && !t.IsAbstract))
            {
                logger.LogInformation($"Creating verifier from type {type.Name}");
                ISettingVerifier? verifier = null;
                try
                {
                    verifier = (ISettingVerifier?) Activator.CreateInstance(type);
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