using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Fig.Common.NetStandard.Constants;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.Constants;
using Fig.Contracts.Diagnostics;
using Fig.Contracts.Json;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.Models.Authentication;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen;

namespace Fig.Web.Services;

public class HttpService : IHttpService
{
    // Reused for large GET deserialize — JsonSerializer is thread-safe for concurrent Deserialize.
    private static readonly JsonSerializer FigHttpSerializer = JsonSerializer.Create(JsonSettings.FigHttp);
    private static readonly JsonSerializer FigWebLoadSerializer = JsonSerializer.Create(FigWebLoadJsonSettings.Instance);

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
        var timed = await GetLargeTimed<T>(uri, showNotifications);
        return timed.Value;
    }

    public async Task<TimedHttpResult<T>> GetLargeTimed<T>(string uri, bool showNotifications = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        return await SendRequestStreamingTimed<T>(request, showNotifications);
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

    public async Task PutOrThrow(string uri, object? value, int? timeoutOverrideSec = null)
    {
        var request = CreateRequest(HttpMethod.Put, uri, value);
        await SendRequestOrThrow(request, timeoutOverrideSec);
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

    private async Task SendRequestOrThrow(HttpRequestMessage request, int? timeoutOverrideSec = null)
    {
        await AddJwtHeader(request);

        try
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutOverrideSec ?? 100));
            using var response = await _httpClient.SendAsync(request, tokenSource.Token);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                HandleUnauthorizedResponse(request);
                throw new UnauthorizedAccessException("The API rejected the request because the current user is not authorized.");
            }

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(await GetErrorResponseMessage(response), null, response.StatusCode);
        }
        catch (OperationCanceledException ex)
        {
            throw new TimeoutException($"Request ({request.Method}) to {request.RequestUri} timed out.", ex);
        }
        catch (HttpRequestException)
        {
            throw;
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

            ShowClientLoadFailureWarnings(response, showNotifications);

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
        var timed = await SendRequestStreamingTimed<T>(request, showNotifications);
        return timed.Value;
    }

    private async Task<TimedHttpResult<T>> SendRequestStreamingTimed<T>(
        HttpRequestMessage request,
        bool showNotifications = true)
    {
        await AddJwtHeader(request);

        try
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(100));
            var requestWatch = Stopwatch.StartNew();
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                tokenSource.Token);
            var requestMs = requestWatch.ElapsedMilliseconds;

            Console.WriteLine($"Request ({request.Method}) to {request.RequestUri} got response {response.StatusCode}");

            // auto logout on 401 response
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                HandleUnauthorizedResponse(request);
                return new TimedHttpResult<T>(default, requestMs, 0);
            }

            if (await HandleErrorResponse(response, showNotifications))
                return new TimedHttpResult<T>(default, requestMs, 0);

            ShowClientLoadFailureWarnings(response, showNotifications);

            var deserializeWatch = Stopwatch.StartNew();
            await using var stream = await response.Content.ReadAsStreamAsync(tokenSource.Token);

            // The browser HTTP stream (ResponseHeadersRead) only supports async reads, but
            // JsonTextReader/StreamReader read synchronously. Buffer the body asynchronously
            // first, then deserialize from memory to avoid net_http_synchronous_reads_not_supported.
            using var buffer = new MemoryStream();
            var bodyReadWatch = Stopwatch.StartNew();
            await stream.CopyToAsync(buffer, tokenSource.Token);
            var bodyReadMs = bodyReadWatch.ElapsedMilliseconds;
            buffer.Position = 0;

            var parseWatch = Stopwatch.StartNew();
            using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);
            using var jsonReader = new JsonTextReader(reader);
            // GET /clients uses FigWebLoad (compact polymorphic values); other endpoints use FigHttp.
            var serializer = IsClientsListUri(request.RequestUri)
                ? FigWebLoadSerializer
                : FigHttpSerializer;
            var value = serializer.Deserialize<T>(jsonReader);
            var parseMs = parseWatch.ElapsedMilliseconds;
            var deserializeMs = deserializeWatch.ElapsedMilliseconds;

            return new TimedHttpResult<T>(value, requestMs, deserializeMs, bodyReadMs, parseMs);
        }
        catch (OperationCanceledException ex)
        {
            HandleCanceledRequest(request, ex, showNotifications);
            return new TimedHttpResult<T>(default, 0, 0);
        }
        catch (IOException ex) when (ex.Message.Contains("I/O error"))
        {
            Console.WriteLine($"WASM I/O error when processing large response: {ex.Message}");
            if (showNotifications)
                _notificationService.Notify(_notificationFactory.Failure("Memory Error",
                    "Response too large for WASM client. Try reducing data size or use server-side processing."));
            return new TimedHttpResult<T>(default, 0, 0);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error when making request {ex.Message}");
            if (showNotifications)
                _notificationService.Notify(_notificationFactory.Failure("Request Failed",
                    "Could not contact the API"));
            return new TimedHttpResult<T>(default, 0, 0);
        }
        catch (OutOfMemoryException ex)
        {
            Console.WriteLine($"Out of memory when processing large response: {ex.Message}");
            if (showNotifications)
                _notificationService.Notify(_notificationFactory.Failure("Memory Error",
                    "Response too large for available memory. Try reducing data size."));
            return new TimedHttpResult<T>(default, 0, 0);
        }
    }

    private void ShowClientLoadFailureWarnings(HttpResponseMessage response, bool showNotifications)
    {
        if (!showNotifications ||
            !response.Headers.TryGetValues(FigHttpHeaders.ClientLoadFailures, out var values))
        {
            return;
        }

        var encodedSummary = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(encodedSummary))
            return;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedSummary));
            var summary = JsonConvert.DeserializeObject<ClientLoadFailureSummaryDataContract>(json, JsonSettings.FigDefault);
            if (summary == null || summary.TotalFailureCount == 0)
                return;

            var failures = summary.Failures ?? Enumerable.Empty<ClientLoadFailureDataContract>();
            var examples = failures
                .Take(5)
                .Select(failure => failure.SettingName is null
                    ? $"{failure.ClientName}{FormatInstance(failure.Instance)}"
                    : $"{failure.ClientName}{FormatInstance(failure.Instance)} -> {failure.SettingName}");
            var detail = $"Loaded all settings that could be decrypted. {summary.TotalFailureCount} client/setting item(s) could not be loaded.";
            var exampleText = string.Join(", ", examples);
            if (!string.IsNullOrWhiteSpace(exampleText))
                detail += $" Affected item(s): {exampleText}.";
            if (summary.Truncated)
                detail += " More affected item details were omitted.";

            _notificationService.Notify(_notificationFactory.Warning("Some settings were not loaded", detail));
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            Console.WriteLine($"Failed to parse client load failure header: {ex.Message}");
        }

        static string FormatInstance(string? instance) =>
            string.IsNullOrWhiteSpace(instance) ? string.Empty : $" ({instance})";
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

        var message = await GetErrorResponseMessage(response);
        if (showNotifications)
            _notificationService.Notify(_notificationFactory.Failure("Server Side Error", message));

        return true;
    }

    private async Task<string> GetErrorResponseMessage(HttpResponseMessage response)
    {
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

        return message;
    }

    private static ErrorResultDataContract? TryDeserializeErrorResult(string content)
    {
        try
        {
            return JsonConvert.DeserializeObject<ErrorResultDataContract>(content, JsonSettings.FigDefault);
        }
        catch
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

    /// <summary>
    /// True for the all-clients list endpoint that uses FigWebLoad compact JSON.
    /// </summary>
    private static bool IsClientsListUri(Uri? requestUri)
    {
        if (requestUri is null)
            return false;

        var path = requestUri.IsAbsoluteUri ? requestUri.AbsolutePath : requestUri.OriginalString;
        path = path.Split('?', '#')[0].TrimEnd('/');
        return path.Equals("/clients", StringComparison.OrdinalIgnoreCase)
               || path.Equals("clients", StringComparison.OrdinalIgnoreCase);
    }
}
