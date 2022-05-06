using Fig.Client.Configuration;
using Microsoft.Extensions.Logging;

namespace Fig.Client.ClientSecret
{
    public class InCodeSecretResolver : ISecretResolver
    {
        private readonly IFigOptions _options;
        private readonly ILogger _logger;

        public InCodeSecretResolver(IFigOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
        }

        public string ResolveSecret()
        {
            _logger.LogWarning("In code secrets are NOT secure and should never be used in production.");

            return new AppSettingsSecretResolver(_options).ResolveSecret();
        }
    }
}