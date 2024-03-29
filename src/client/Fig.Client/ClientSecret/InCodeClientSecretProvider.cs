﻿using Microsoft.Extensions.Logging;

namespace Fig.Client.ClientSecret;

public class InCodeClientSecretProvider : IClientSecretProvider
{
    private readonly ILogger<InCodeClientSecretProvider> _logger;
    private readonly string _clientSecret;

    public InCodeClientSecretProvider(ILogger<InCodeClientSecretProvider> logger, string clientSecret)
    {
        _logger = logger;
        _clientSecret = clientSecret;
    }

    public string GetSecret(string clientName)
    {
        _logger.LogWarning("Using in-code client secret provider. This is not recommended for production use.");
        return _clientSecret;
    }
}