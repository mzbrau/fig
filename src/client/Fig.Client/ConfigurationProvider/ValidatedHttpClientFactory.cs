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
using Fig.Client.Utils;

using System.Security.Authentication;

namespace Fig.Client.ConfigurationProvider;

public class ValidatedHttpClientFactory
{
    internal const string TimeoutEnvVar = "FIG_API_REQUEST_TIMEOUT_SECONDS";
    
    private readonly ILogger<ValidatedHttpClientFactory> _logger;
    private readonly TimeSpan _requestTimeout;
    private readonly int _retryCount;

    public TimeSpan RequestTimeout => _requestTimeout;

    public ValidatedHttpClientFactory(
        ILogger<ValidatedHttpClientFactory> logger,
        TimeSpan? requestTimeout = null,
        int? retryCount = null,
        bool hasOfflineSettings = true)
    {
        _logger = logger;
        
        var isWindowsService = WindowsServiceDetector.IsRunningAsWindowsService();
        
        // Compute the context-based default (short timeouts when offline settings exist to avoid
        // delaying app startup; longer when no offline settings since the API is the only source)
        var defaultTimeout = hasOfflineSettings
            ? TimeSpan.FromSeconds(5)
            : (isWindowsService ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(60));

        string timeoutSource;
        var envVarTimeout = ReadTimeoutFromEnvironmentVariable();

        if (envVarTimeout.HasValue)
        {
            _requestTimeout = envVarTimeout.Value;
            timeoutSource = TimeoutEnvVar;
            var wouldHaveBeen = requestTimeout ?? defaultTimeout;
            _logger.LogInformation(
                "Fig API request timeout overridden by {EnvVar} environment variable: {Timeout}s (default would have been {DefaultTimeout}s)",
                TimeoutEnvVar, _requestTimeout.TotalSeconds, wouldHaveBeen.TotalSeconds);
        }
        else if (requestTimeout.HasValue)
        {
            _requestTimeout = requestTimeout.Value;
            timeoutSource = "FigOptions.ApiRequestTimeout";
        }
        else
        {
            _requestTimeout = defaultTimeout;
            timeoutSource = "default";
            
            if (!hasOfflineSettings)
            {
                _logger.LogInformation(
                    "No offline settings available. Using extended API timeout: {Timeout}s",
                    _requestTimeout.TotalSeconds);
            }
        }
        
        _retryCount = retryCount ?? (isWindowsService ? 0 : 2);
        
        _logger.LogInformation(
            "Fig API request timeout: {Timeout}s (source: {Source}, retries: {Retries})",
            _requestTimeout.TotalSeconds, timeoutSource, _retryCount);
        
        if (isWindowsService)
        {
            _logger.LogDebug(
                "Running as Windows Service. Using reduced API timeouts (Timeout: {Timeout}s, Retries: {Retries})",
                _requestTimeout.TotalSeconds, _retryCount);
        }
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
        IAsyncPolicy<HttpResponseMessage> policyWrap;
        
        if (_retryCount > 0)
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(_requestTimeout);
            policyWrap = Policy.WrapAsync(retryPolicy, timeoutPolicy);
        }
        else
        {
            // No retries, just timeout policy
            policyWrap = Policy.TimeoutAsync<HttpResponseMessage>(_requestTimeout);
        }

        ServicePoint servicePoint = ServicePointManager.FindServicePoint(new Uri(apiUri));
        servicePoint.ConnectionLeaseTimeout = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;

        var handler = new PolicyHttpMessageHandler(policyWrap)
        {
            InnerHandler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls12
            }
        };
        
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(apiUri),
        };
    }

    private TimeSpan? ReadTimeoutFromEnvironmentVariable()
    {
        var value = Environment.GetEnvironmentVariable(TimeoutEnvVar);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, out var seconds) && seconds > 0)
            return TimeSpan.FromSeconds(seconds);

        _logger.LogWarning(
            "Invalid value '{Value}' for {EnvVar}; expected a positive integer number of seconds. Using configured or default timeout.",
            value, TimeoutEnvVar);
        return null;
    }
}
