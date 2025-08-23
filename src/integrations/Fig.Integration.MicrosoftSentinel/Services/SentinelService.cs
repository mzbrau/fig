using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fig.Integration.MicrosoftSentinel.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace Fig.Integration.MicrosoftSentinel.Services;

public class SentinelService : ISentinelService
{
    private const string HttpMethodPost = "POST";
    private const string ContentTypeApplicationJson = "application/json";
    private const string LogsApiPath = "/api/logs";
    private const string ApiVersion = "2016-04-01";
    private const string AuthorizationHeaderFormat = "SharedKey {0}:{1}";
    private const string SentinelUrlFormat = "https://{0}.ods.opinsights.azure.com{1}?api-version={2}";
    private const string DateHeaderFormat = "r";
    private const string HeaderXMsDate = "x-ms-date";
    private const string HeaderLogType = "Log-Type";
    private const string HeaderTimeGeneratedField = "time-generated-field";
    private const string TimeGeneratedFieldValue = "timestamp";
    private const string SignatureStringFormat = "{0}\n{1}\n{2}\n{3}:{4}\n{5}";
    
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<Settings> _settings;
    private readonly ILogger<SentinelService> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public SentinelService(HttpClient httpClient, IOptionsMonitor<Settings> settings, ILogger<SentinelService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        
        // Configure retry policy with exponential backoff
        _retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: _settings.CurrentValue.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(
                    _settings.CurrentValue.RetryDelaySeconds * Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry attempt {RetryCount} for Sentinel API call after {Delay}ms. Reason: {Reason}",
                        retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase);
                });
    }

    public async Task<bool> SendLogAsync(object logData, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = _settings.CurrentValue;
            var jsonData = JsonSerializer.Serialize(logData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var contentLength = Encoding.UTF8.GetByteCount(jsonData);
            var method = HttpMethodPost;
            var contentType = ContentTypeApplicationJson;
            var resource = LogsApiPath;
            var url = string.Format(SentinelUrlFormat, settings.SentinelWorkspaceId, resource, ApiVersion);

            _logger.LogDebug("Sending log to Sentinel. WorkspaceId: {WorkspaceId}, LogType: {LogType}, DataSize: {DataSize} bytes",
                settings.SentinelWorkspaceId, settings.SentinelLogType, contentLength);

            using var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                // Generate fresh timestamp and signature for each retry attempt
                var dateString = DateTime.UtcNow.ToString(DateHeaderFormat);
                
                // Build the signature
                var stringToHash = string.Format(SignatureStringFormat, method, contentLength, contentType, HeaderXMsDate, dateString, resource);
                var signature = BuildSignature(stringToHash, settings.SentinelWorkspaceKey);
                
                // Create the authorization header
                var authorization = string.Format(AuthorizationHeaderFormat, settings.SentinelWorkspaceId, signature);
                
                // Create a new request for each attempt (HttpRequestMessage can only be sent once)
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", authorization);
                request.Headers.Add(HeaderLogType, settings.SentinelLogType);
                request.Headers.Add(HeaderXMsDate, dateString);
                request.Headers.Add(HeaderTimeGeneratedField, TimeGeneratedFieldValue);
                request.Content = new StringContent(jsonData, Encoding.UTF8, contentType);
                
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(settings.SentinelApiTimeoutSeconds));
                
                return await _httpClient.SendAsync(request, cts.Token);
            });

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent log to Sentinel. WorkspaceId: {WorkspaceId}, LogType: {LogType}",
                    settings.SentinelWorkspaceId, settings.SentinelLogType);
                return true;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to send log to Sentinel. Status: {StatusCode}, Response: {Response}",
                response.StatusCode, responseContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending log to Sentinel");
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var testLog = new
            {
                timestamp = DateTime.UtcNow,
                level = "Info",
                message = "Test connection from Fig Microsoft Sentinel Integration",
                source = "Fig.Integration.MicrosoftSentinel",
                eventType = "ConnectionTest"
            };

            var result = await SendLogAsync(testLog, cancellationToken);
            
            if (result)
            {
                _logger.LogInformation("Sentinel connection test successful");
            }
            else
            {
                _logger.LogWarning("Sentinel connection test failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during Sentinel connection test");
            return false;
        }
    }

    private static string BuildSignature(string message, string secret)
    {
        var encoding = Encoding.UTF8;
        var keyByte = Convert.FromBase64String(secret);
        var messageBytes = encoding.GetBytes(message);
        
        using var hmacsha256 = new HMACSHA256(keyByte);
        var hash = hmacsha256.ComputeHash(messageBytes);
        return Convert.ToBase64String(hash);
    }
}