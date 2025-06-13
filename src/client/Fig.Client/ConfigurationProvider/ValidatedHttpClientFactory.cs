using Microsoft.Extensions.Http;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Polly;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly.Timeout;

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

        _logger.LogInformation("Multiple Fig URI's configured, Selecting the first valid one");
        foreach (var apiUri in apiUris)
        {
            var client = CreateHttpClient(apiUri);
            {
                try
                {
                    _logger.LogDebug("Validating Fig address {ApiUri}", apiUri);
                    HttpResponseMessage response = await client.GetAsync("_health");
                    response.EnsureSuccessStatusCode();

                    _logger.LogDebug("Validating of Fig address {ApiUri} was successful", apiUri);
                    return client;
                }
                catch (HttpRequestException)
                {
                    _logger.LogDebug("Validating of Fig address {ApiUri} failed", apiUri);
                    client.Dispose();
                }
                catch (TaskCanceledException)
                {
                    _logger.LogDebug("Validating of Fig address {ApiUri} failed", apiUri);
                    client.Dispose();
                }
            }
        }

        _logger.LogDebug("All Fig addresses failed validation. Using first supplied address {ApiUri}", apiUris[0]);
        return CreateHttpClient(apiUris[0]);
    }

    private HttpClient CreateHttpClient(string apiUri)
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(5);
        var policyWrap = Policy.WrapAsync(retryPolicy, timeoutPolicy);

        ServicePoint servicePoint = ServicePointManager.FindServicePoint(new Uri(apiUri));
        servicePoint.ConnectionLeaseTimeout = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;

        var handler = new PolicyHttpMessageHandler(policyWrap)
        {
            InnerHandler = new HttpClientHandler()
        };
        
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(apiUri),
        };
    }
}
