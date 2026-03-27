using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Authentication;
using Fig.Mcp.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Fig.Mcp.ApiClient;

public class FigAuthHandler : DelegatingHandler
{
    private const string AuthEndpoint = "users/authenticate";

    private readonly IOptions<McpSettings> _settings;
    private readonly ILogger<FigAuthHandler> _logger;

    private string? _token;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    public FigAuthHandler(IOptions<McpSettings> settings, ILogger<FigAuthHandler> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (IsAuthRequest(request))
            return await base.SendAsync(request, cancellationToken);

        await EnsureTokenAsync(cancellationToken);
        SetAuthHeader(request);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 — refreshing authentication token");
            InvalidateCachedToken();
            await RefreshTokenAsync(cancellationToken);

            using var retryRequest = await CloneRequestAsync(request);
            SetAuthHeader(retryRequest);
            response = await base.SendAsync(retryRequest, cancellationToken);
        }

        return response;
    }

    private static bool IsAuthRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.AbsolutePath.TrimEnd('/').EndsWith(
            AuthEndpoint, StringComparison.OrdinalIgnoreCase) == true;
    }

    private void SetAuthHeader(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    private void InvalidateCachedToken()
    {
        _token = null;
        _tokenExpiry = DateTime.MinValue;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        if (original.Content != null)
        {
            var contentBytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }

    private async Task EnsureTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry)
            return;

        await RefreshTokenAsync(ct);
    }

    private async Task RefreshTokenAsync(CancellationToken ct)
    {
        await _authLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring the lock
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry)
                return;

            _logger.LogInformation("Authenticating with Fig API as '{Username}'",
                _settings.Value.Username);

            var authRequest = new AuthenticateRequestDataContract(
                _settings.Value.Username,
                _settings.Value.Password);

            var json = JsonConvert.SerializeObject(authRequest, JsonSettings.FigDefault);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var baseUrl = _settings.Value.FigApiBaseUrl.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/{AuthEndpoint}")
            {
                Content = content
            };

            using var response = await base.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"Fig API authentication failed with status {(int)response.StatusCode}: {errorBody}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var authResponse = JsonConvert.DeserializeObject<AuthenticateResponseDataContract>(
                responseBody, JsonSettings.FigDefault)
                ?? throw new InvalidOperationException(
                    "Fig API authentication returned a null response.");

            _token = authResponse.Token;
            _tokenExpiry = GetTokenExpiry(authResponse.Token);

            _logger.LogInformation(
                "Successfully authenticated with Fig API. Token expires at {Expiry:u}",
                _tokenExpiry);
        }
        finally
        {
            _authLock.Release();
        }
    }

    private DateTime GetTokenExpiry(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            // Refresh slightly before actual expiry to avoid edge-case failures
            return jwt.ValidTo.AddMinutes(-1);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to parse JWT expiry — defaulting to 15 minutes");
            return DateTime.UtcNow.AddMinutes(15);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _authLock.Dispose();

        base.Dispose(disposing);
    }
}
