using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Fig.Common.NetStandard.Constants;
using Fig.Common.NetStandard.Json;
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

    public async Task Put(string uri, object value)
    {
        var request = CreateRequest(HttpMethod.Put, uri, value);
        await SendRequest(request);
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
        if (value != null)
            request.Content = new StringContent(JsonConvert.SerializeObject(value, JsonSettings.FigDefault), Encoding.UTF8, "application/json");

        return request;
    }

    private async Task SendRequest(HttpRequestMessage request)
    {
        await AddJwtHeader(request);

        // send request
        using var response = await _httpClient.SendAsync(request);

        // auto logout on 401 response
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _navigationManager.NavigateTo("account/logout");
            return;
        }

        await ThrowErrorResponse(response);
    }

    private async Task<T?> SendRequest<T>(HttpRequestMessage request, bool showNotifications = true)
    {
        await AddJwtHeader(request);

        try
        {
            using var response = await _httpClient.SendAsync(request);

            Console.WriteLine($"Request ({request.Method}) to {request.RequestUri} got response {response.StatusCode}");

            // auto logout on 401 response
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _navigationManager.NavigateTo("account/logout");
                return default;
            }

            await ThrowErrorResponse(response);

            var stringContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response json was: {stringContent}");
            return JsonConvert.DeserializeObject<T>(stringContent, JsonSettings.FigDefault);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error when making request {ex.Message}");
            if (showNotifications)
                _notificationService.Notify(_notificationFactory.Failure("Request Failed", "Could not contact the API"));
            return default;
        }
    }

    private async Task AddJwtHeader(HttpRequestMessage request)
    {
        // add jwt auth header if user is logged in and request is to the api url
        var user = await _localStorageService.GetItem<AuthenticatedUserModel>("user");
        var isApiUrl = !request.RequestUri?.IsAbsoluteUri == true;
        if (user != null && isApiUrl)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
    }

    private async Task ThrowErrorResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                Dictionary<string, string>? error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
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
                _notificationService.Notify(_notificationFactory.Failure("Failed Processing Server Side Error", e.Message));
                throw;
            }
        }
    }
}