using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Fig.Common.NetStandard.Constants;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Web.Models.Authentication;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
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

    public HttpService(
        IHttpClientFactory httpClientFactory,
        NavigationManager navigationManager,
        ILocalStorageService localStorageService,
        NotificationService notificationService,
        INotificationFactory notificationFactory)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.FigApi);
        _httpClient.Timeout = TimeSpan.FromHours(1);
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
        _notificationService = notificationService;
        _notificationFactory = notificationFactory;
        Console.WriteLine($"Initializing httpservice with API address {_httpClient.BaseAddress}");
    }

    public string BaseAddress => _httpClient.BaseAddress?.ToString() ?? "Unknown";

    public async Task<T?> Get<T>(string uri, bool showNotifications = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        return await SendRequest<T>(request, showNotifications);
    }

    public async Task<T?> GetAnonymous<T>(string uri, bool showNotifications = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        return await SendRequest<T>(request, showNotifications, addJwtHeader: false);
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

        // Add Accept-Encoding header for compression support (GZip only)
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
                HandleUnauthorizedResponse(request);
                return;
            }

            if (await HandleErrorResponse(response, true))
                return;
        }
        catch (OperationCanceledException ex)
        {
            HandleCanceledRequest(request, ex, true);
        }
    }

    private async Task<T?> SendRequest<T>(HttpRequestMessage request, bool showNotifications = true, bool addJwtHeader = true)
    {
        await AddJwtHeader(request, addJwtHeader);

        try
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(100));
            using var response = await _httpClient.SendAsync(request, tokenSource.Token);

            Console.WriteLine($"Request ({request.Method}) to {request.RequestUri} got response {response.StatusCode}");

            // auto logout on 401 response
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                HandleUnauthorizedResponse(request);
                return default;
            }

            if (await HandleErrorResponse(response, showNotifications))
                return default;

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
                HandleUnauthorizedResponse(request);
                return default;
            }

            if (await HandleErrorResponse(response, showNotifications))
                return default;

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

    private async Task AddJwtHeader(HttpRequestMessage request, bool addJwtHeader = true)
    {
        if (!addJwtHeader)
            return;

        // add jwt auth header if user is logged in and request is to the api url
        var user = await _localStorageService.GetItem<AuthenticatedUserModel>("user");
        var isApiUrl = !request.RequestUri?.IsAbsoluteUri == true;
        if (user != null && isApiUrl)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
    }

    private void HandleUnauthorizedResponse(HttpRequestMessage request)
    {
        if (request.Headers.Authorization is null)
            return;

        var currentUri = new Uri(_navigationManager.Uri);
        if (!currentUri.AbsolutePath.Contains("/account/login", StringComparison.OrdinalIgnoreCase))
        {
            _navigationManager.NavigateTo("account/logout");
        }
    }

    private async Task<bool> HandleErrorResponse(HttpResponseMessage response, bool showNotifications)
    {
        if (response.IsSuccessStatusCode)
            return false;

        var message = $"The API returned {(int)response.StatusCode} {response.ReasonPhrase}";
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(content))
            {
                var error = TryDeserializeErrorResult(content);
                if (error is not null)
                {
                    Console.WriteLine($"Error response: {error}");
                    message = error.Message;
                }
                else
                {
                    Console.WriteLine($"Error response ({response.StatusCode}): {content}");
                    message = content;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception when processing error response. {e}");
        }

        if (showNotifications)
            _notificationService.Notify(_notificationFactory.Failure("Server Side Error", message));

        return true;
    }

    private static ErrorResultDataContract? TryDeserializeErrorResult(string content)
    {
        try
        {
            return JsonConvert.DeserializeObject<ErrorResultDataContract>(content, JsonSettings.FigDefault);
        }
        catch (JsonException)
        {
            return null;
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
