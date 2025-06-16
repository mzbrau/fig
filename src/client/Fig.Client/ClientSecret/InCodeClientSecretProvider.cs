using System.Threading.Tasks;
using Fig.Client.Contracts;
using Microsoft.Extensions.Logging;

namespace Fig.Client.ClientSecret;

public class InCodeClientSecretProvider : IClientSecretProvider
{
    private const string SuppressLogSuffix = "--suppresslog";
    private readonly ILogger<InCodeClientSecretProvider> _logger;
    private readonly string _clientSecret;
    private readonly bool _suppressLog;

    public InCodeClientSecretProvider(ILogger<InCodeClientSecretProvider> logger, string clientSecret)
    {
        _logger = logger;
        _clientSecret = clientSecret;
        
        // This is a special hack that prevents the logs being spammed when the in code client secret has to be used in special circumstances.
        if (clientSecret.EndsWith(SuppressLogSuffix))
        {
            _logger.LogWarning("Using in-code client secret provider. This is not recommended for production use. Further logging has been suppressed");
            _clientSecret = clientSecret.Replace(SuppressLogSuffix, string.Empty);
            _suppressLog = true;
        }
    }

    public string Name => "InCode";

    public bool IsEnabled => true;

    public void AddLogger(ILoggerFactory loggerFactory)
    {
        // Do nothing. Logger already injected.
    }

    public Task<string> GetSecret(string clientName)
    {
        if (!_suppressLog)
        {
            _logger.LogWarning("Using in-code client secret provider. This is not recommended for production use");
        }
        
        return Task.FromResult(_clientSecret);
    }
}