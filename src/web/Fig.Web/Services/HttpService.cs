using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Fig.Common.NetStandard.Constants;
using Fig.Common.NetStandard.Json;
using Fig.Web.Models.Authentication;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Radzen;

namespace Fig.Web.Services;

public class HttpService : IHttpService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorageService;
    private readonly NotificationService _notificationService;
    private readonly INotificationFactory _notificationFactory;
    private readonly NavigationManager _navigationManager;
    private readonly WebAuthMode _authenticationMode;
    private readonly IServiceProvider _serviceProvider;

    public HttpService(
        IHttpClientFactory httpClientFactory,
        NavigationManager navigationManager,
        ILocalStorageService localStorageService,
        NotificationService notificationService,
        INotificationFactory notificationFactory,
        IOptions<WebSettings> webSettings,
        IServiceProvider serviceProvider)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.FigApi);
        _httpClient.Timeout = TimeSpan.FromHours(1);
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
        _notificationService = notificationService;
        _notificationFactory = notificationFactory;
        _authenticationMode = webSettings.Value.Authentication.Mode;
        _serviceProvider = serviceProvider;
        Console.WriteLine($"Initializing httpservice with API address {_httpClient.BaseAddress}");
    }

    public string BaseAddress => _httpClient.BaseAddress?.ToString() ?? "Unknown";

    public async Task<T?> Get<T>(string uri, bool showNotifications = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        return await SendRequest<T>(request, showNotifications);
    }

    /// <summary>
    /// Gets data from the API using streaming for large responses to avoid WASM memory issues
    /// </summary>
    public async Task<T?> GetLarge<T>(string uri, bool showNotifications = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        return await SendRequestStreaming<T>(request, showNotifications);
    }

    public async Task Post(string uri, object value)
    {
        var request = CreateRequest(HttpMethod.Post, uri, value);
        await SendRequest(request);
    }

    public async Task<T?> Post<T>(string uri, object value)
    {
        var request = CreateRequest(HttpMethod.Post, uri, value);
        return await SendRequest<T>(request);
    }

    public async Task Put(string uri, object? value, int? timeoutOverrideSec = null)
    {
        var request = CreateRequest(HttpMethod.Put, uri, value);
        await SendRequest(request, timeoutOverrideSec);
    }

    public async Task<T?> Put<T>(string uri, object? value)
    {
        var request = CreateRequest(HttpMethod.Put, uri, value);
        return await SendRequest<T>(request);
    }

    public async Task Delete(string uri)
    {
        var request = CreateRequest(HttpMethod.Delete, uri);
        await SendRequest(request);
    }

    public async Task<T?> Delete<T>(string uri)
    {
        var request = CreateRequest(HttpMethod.Delete, uri);
        return await SendRequest<T>(request);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri, object? value = null)
    {
        var request = new HttpRequestMessage(method, uri);

        // Add Accept-Encoding header for Brotli compression support
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

        if (value != null)
            request.Content = new StringContent(JsonConvert.SerializeObject(value, JsonSettings.FigDefault),
                Encoding.UTF8, "application/json");

        return request;
    }

    private async Task SendRequest(HttpRequestMessage request, int? timeoutOverrideSec = null)
    {
        await AddJwtHeader(request);

        try
        {
            // send request
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutOverrideSec ?? 100));
            using var response = await _httpClient.SendAsync(request, tokenSource.Token);

            // auto logout on 401 response
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Only navigate to logout if we're not already on the login page
                var currentUri = new Uri(_navigationManager.Uri);
                if (!currentUri.AbsolutePath.Contains("/account/login", StringComparison.OrdinalIgnoreCase))
                {
                    _navigationManager.NavigateTo("account/logout");
                }
                return;
            }

            await ThrowErrorResponse(response);
        }
        catch (OperationCanceledException ex)
        {
            HandleCanceledRequest(request, ex, true);
        }
    }

    private async Task<T?> SendRequest<T>(HttpRequestMessage request, bool showNotifications = true)
    {
        await AddJwtHeader(request);

        try
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(100));
            using var response = await _httpClient.SendAsync(request, tokenSource.Token);

            Console.WriteLine($"Request ({request.Method}) to {request.RequestUri} got response {response.StatusCode}");

            // auto logout on 401 response
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Only navigate to logout if we're not already on the login page
                var currentUri = new Uri(_navigationManager.Uri);
                if (!currentUri.AbsolutePath.Contains("/account/login", StringComparison.OrdinalIgnoreCase))
                {
                    _navigationManager.NavigateTo("account/logout");
                }
                return default;
            }

            await ThrowErrorResponse(response);

            var stringContent = await response.Content.ReadAsStringAsync(tokenSource.Token);

            // Avoid logging large responses that can cause I/O errors in WASM
            Console.WriteLine(stringContent.Length <= 10240 // Only log responses under 1KB
                ? $"Response json was: {stringContent}"
                : $"Response received (size: {stringContent.Length:N0} characters) - content too large to log");

            return JsonConvert.DeserializeObject<T>(stringContent, JsonSettings.FigDefault);
        }
        catch (OperationCanceledException ex)
        {
            HandleCanceledRequest(request, ex, showNotifications);
            return default;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error when making request {ex.Message}");
            if (showNotifications)
                _notificationService.Notify(_notificationFactory.Failure("Request Failed",
                    "Could not contact the API"));
            return default;
        }
    }

    private async Task<T?> SendRequestStreaming<T>(HttpRequestMessage request, bool showNotifications = true)
    {
        await AddJwtHeader(request);

        try
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(100));
            using var response = await _httpClient.SendAsync(request, tokenSource.Token);

            Console.WriteLine($"Request ({request.Method}) to {request.RequestUri} got response {response.StatusCode}");

            // auto logout on 401 response
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Only navigate to logout if we're not already on the login page
                var currentUri = new Uri(_navigationManager.Uri);
                if (!currentUri.AbsolutePath.Contains("/account/login", StringComparison.OrdinalIgnoreCase))
                {
                    _navigationManager.NavigateTo("account/logout");
                }
                return default;
            }

            await ThrowErrorResponse(response);

            // Handle streaming response
            await using var stream = await response.Content.ReadAsStreamAsync(tokenSource.Token);
            using var reader = new StreamReader(stream);
            await using var jsonReader = new JsonTextReader(reader);
            var serializer = JsonSerializer.Create(JsonSettings.FigDefault);
            return serializer.Deserialize<T>(jsonReader);
        }
        catch (OperationCanceledException ex)
        {
            HandleCanceledRequest(request, ex, showNotifications);
            return default;
        }
        catch (IOException ex) when (ex.Message.Contains("I/O error"))
        {
            Console.WriteLine($"WASM I/O error when processing large response: {ex.Message}");
            if (showNotifications)
                _notificationService.Notify(_notificationFactory.Failure("Memory Error",
                    "Response too large for WASM client. Try reducing data size or use server-side processing."));
            return default;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error when making request {ex.Message}");
            if (showNotifications)
                _notificationService.Notify(_notificationFactory.Failure("Request Failed",
                    "Could not contact the API"));
            return default;
        }
        catch (OutOfMemoryException ex)
        {
            Console.WriteLine($"Out of memory when processing large response: {ex.Message}");
            if (showNotifications)
                _notificationService.Notify(_notificationFactory.Failure("Memory Error",
                    "Response too large for available memory. Try reducing data size."));
            return default;
        }
    }

    private async Task AddJwtHeader(HttpRequestMessage request)
    {
        var isApiUrl = !request.RequestUri?.IsAbsoluteUri == true;
        if (!isApiUrl)
            return;

        if (_authenticationMode == WebAuthMode.Keycloak)
        {
            var accessTokenProvider = _serviceProvider.GetService<IAccessTokenProvider>();
            if (accessTokenProvider != null)
            {
                var tokenResult = await accessTokenProvider.RequestAccessToken();
                if (tokenResult.TryGetToken(out var token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
                }
            }

            return;
        }

        var user = await _localStorageService.GetItem<AuthenticatedUserModel>("user");
        if (user != null && !string.IsNullOrWhiteSpace(user.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
    }

    private async Task ThrowErrorResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                Dictionary<string, string>? error =
                    await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                foreach (var item in error ?? new Dictionary<string, string>())
                    Console.WriteLine($"{item.Key} -> {item.Value}");

                const string messageKey = "Message";
                if (error?.TryGetValue(messageKey, out var message) is true)
                {
                    Console.WriteLine($"Throwing exception with message {message}");
                    _notificationService.Notify(_notificationFactory.Failure("Server Side Error", message));
                    throw new Exception(message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception when processing error. {e}");
                _notificationService.Notify(_notificationFactory.Failure("Failed Processing Server Side Error",
                    e.Message));
                throw;
            }
        }
    }

    private void HandleCanceledRequest(HttpRequestMessage request, OperationCanceledException ex, bool showNotifications)
    {
        Console.WriteLine($"Request ({request.Method}) to {request.RequestUri} was canceled: {ex.Message}");
        if (showNotifications)
        {
            _notificationService.Notify(_notificationFactory.Failure("Request Cancelled",
                "The request timed out or was cancelled."));
        }
    }
}
