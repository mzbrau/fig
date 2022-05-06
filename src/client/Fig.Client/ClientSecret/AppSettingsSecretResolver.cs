using System;
using Fig.Client.Configuration;
using Fig.Client.Exceptions;
using Microsoft.Extensions.Logging;

namespace Fig.Client.ClientSecret
{
    public class AppSettingsSecretResolver : ISecretResolver
    {
        private readonly IFigOptions _options;

        public AppSettingsSecretResolver(IFigOptions options)
        {
            _options = options;
        }
        
        public string ResolveSecret()
        {
            if (_options.ClientSecret is null)
                throw new FigConfigurationException(
                    "No client secret provided in appSettings.json file or fig options class.");

            if (!Guid.TryParse(_options.ClientSecret, out _))
                throw new FigConfigurationException("Client secret must be a Guid");

            return _options.ClientSecret;
        }
    }
}