using Microsoft.Extensions.Http;
using Polly.Extensions.Http;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Polly;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fig.Client.ConfigurationProvider;

public class ValidatedHttpClientFactory
{
    private readonly ILogger<ValidatedHttpClientFactory> _logger;

    public ValidatedHttpClientFactory(ILogger<ValidatedHttpClientFactory> logger)
    {
        _logger = logger;
    }
    
    public async Task<HttpClient> CreateClient(List<string>? apiUris)
    {
        if (apiUris == null || apiUris.Count == 0)
        {
            throw new ArgumentException("List of Fig API URLs cannot be empty or null.");
        }

        if (apiUris.Count == 1)
        {
            return CreateHttpClient(apiUris[0]);
        }

        _logger.LogInformation("Multiple Fig URI's configured. Selecting the first valid one.");
        foreach (var apiUri in apiUris)
        {
            var client = CreateHttpClient(apiUri);
            {
                try
                {
                    _logger.LogDebug("Validating Fig address {apiUri}", apiUri);
                    HttpResponseMessage response = await client.GetAsync("_health");
                    response.EnsureSuccessStatusCode();

                    _logger.LogDebug("Validating of Fig address {apiUri} was successful", apiUri);
                    return client;
                }
                catch (HttpRequestException)
                {
                    _logger.LogDebug("Validating of Fig address {apiUri} failed", apiUri);
                    client.Dispose();
                }
            }
        }

        _logger.LogDebug("All Fig addresses failed validation. Using first supplied address {apiUri}", apiUris[0]);
        return CreateHttpClient(apiUris[0]);
    }

    private HttpClient CreateHttpClient(string apiUri)
    {
        AsyncRetryPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        var socketHandler = new StandardSocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) };
        var pollyHandler = new PolicyHttpMessageHandler(retryPolicy)
        {
            InnerHandler = socketHandler,
        };

        return new HttpClient(pollyHandler)
        {
            BaseAddress = new Uri(apiUri),
            Timeout = TimeSpan.FromSeconds(5)
        };
    }
}