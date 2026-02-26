using System.Net;
using System.Text;
using Fig.Api;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Test.Common;

public class ApiClient
{
    private readonly WebApplicationFactory<ApiSettings> _app;
    private string? _bearerToken;
    public const string AdminUserName = "admin";

    internal ApiClient(WebApplicationFactory<ApiSettings> app)
    {
        _app = app;
    }
    
    public async Task Authenticate()
    {
        var responseObject = await Login(AdminUserName, "admin");

        _bearerToken = $"Bearer {responseObject.Token}";

        Assert.That(responseObject.Token, Is.Not.Null, "A bearer token should be set after authentication");
    }

    public async Task<AuthenticateResponseDataContract> Login(bool checkSuccess = true)
    {
        return await Login(AdminUserName, "admin", checkSuccess);
    }
    
    public async Task<AuthenticateResponseDataContract> Login(string username, string password,
        bool checkSuccess = true)
    {
        var auth = new AuthenticateRequestDataContract(username, password);

        var json = JsonConvert.SerializeObject(auth, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        var response = await httpClient.PostAsync("/users/authenticate", data);

        if (checkSuccess)
        {
            var error = await GetErrorResult(response);
            Assert.That(response.IsSuccessStatusCode, Is.True, $"Authentication should succeed. {error}");
        }

        var responseString = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<AuthenticateResponseDataContract>(responseString, JsonSettings.FigDefault)!;
    }

    public async Task<T?> Get<T>(string uri, bool authenticate = true, string? secret = null, string? tokenOverride = null)
    {
        using var httpClient = GetHttpClient();
        
        if (secret is not null)
            httpClient.DefaultRequestHeaders.Add("clientSecret", secret);
        
        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", tokenOverride ?? _bearerToken);
        
        var result = await httpClient.GetStringAsync(uri);

        Assert.That(result, Is.Not.Null, $"Non null result expected for uri {uri}.");
        
        return !string.IsNullOrEmpty(result) ? JsonConvert.DeserializeObject<T>(result, JsonSettings.FigDefault) : default;
    }

    public async Task GetAndVerify(string uri, HttpStatusCode expected, bool authenticate = true)
    {
        using var httpClient = GetHttpClient();
        
        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", _bearerToken);

        var result = await httpClient.GetAsync(uri);
        
        Assert.That(result.StatusCode, Is.EqualTo(expected));
    }

    public async Task PutAndVerify(string uri, object? data, HttpStatusCode expected, bool authenticate = true)
    {
        using var httpClient = GetHttpClient();
        
        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", _bearerToken);

        StringContent? content = null;
        if (data is not null)
        {
            var json = JsonConvert.SerializeObject(data, JsonSettings.FigDefault);
            content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        
        var result = await httpClient.PutAsync(uri, content);

        Assert.That(result.StatusCode, Is.EqualTo(expected));
    }

    public async Task PostAndVerify(string uri, object data, HttpStatusCode expected, bool authenticate = true)
    {
        var json = JsonConvert.SerializeObject(data, JsonSettings.FigDefault);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        
        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", _bearerToken);
        
        var result = await httpClient.PostAsync(uri, content);

        Assert.That(result.StatusCode, Is.EqualTo(expected));
    }

    public async Task<T?> Put<T>(string uri, object? data, bool authenticate = true, string? tokenOverride = null, bool validateSuccess = true) where T : class
    {
        StringContent? content = null;
        if (data is not null)
        {
            var json = JsonConvert.SerializeObject(data, JsonSettings.FigDefault);
            content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", tokenOverride ?? _bearerToken);

        var result = await httpClient.PutAsync(uri, content);

        if (validateSuccess)
        {
            var error = await GetErrorResult(result);
            Assert.That(result.IsSuccessStatusCode, Is.True, $"Put to uri {uri} should succeed. {error}");
        }

        if (typeof(T) == typeof(HttpResponseMessage))
            return result as T;
        
        var response = await result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(response, JsonSettings.FigDefault);
    }

    public async Task<HttpResponseMessage> Post(string uri, object data, string? clientSecret = null, bool authenticate = false, bool validateSuccess = true)
    {
        var json = JsonConvert.SerializeObject(data, JsonSettings.FigDefault);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret ?? GetNewSecret());
        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", _bearerToken);
        
        var result = await httpClient.PostAsync(uri, content);

        if (validateSuccess)
        {
            var error = await GetErrorResult(result);
            Assert.That(result.IsSuccessStatusCode, Is.True, $"Post to uri {uri} should succeed. {error}");
        }

        return result;
    }

    public async Task<ErrorResultDataContract?> Delete(string uri, bool authenticate = true, bool validateSuccess = true, string? tokenOverride = null)
    {
        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", tokenOverride ?? _bearerToken);

        var result = await httpClient.DeleteAsync(uri);
        var error = await GetErrorResult(result);
        if (validateSuccess)
            Assert.That(result.IsSuccessStatusCode, Is.True, $"Delete at uri {uri} should succeed. {error}");

        return error;
    }

    public async Task<HttpResponseMessage> GetRaw(string uri, string? tokenOverride = null)
    {
        using var httpClient = GetHttpClient();
        
        if (tokenOverride is not null)
            httpClient.DefaultRequestHeaders.Add("Authorization", tokenOverride);
        else if (_bearerToken is not null)
            httpClient.DefaultRequestHeaders.Add("Authorization", _bearerToken);
        
        return await httpClient.GetAsync(uri);
    }

    public async Task<HttpResponseMessage> DeleteRaw(string uri, string? tokenOverride = null)
    {
        using var httpClient = GetHttpClient();
        
        if (tokenOverride is not null)
            httpClient.DefaultRequestHeaders.Add("Authorization", tokenOverride);
        else if (_bearerToken is not null)
            httpClient.DefaultRequestHeaders.Add("Authorization", _bearerToken);
        
        return await httpClient.DeleteAsync(uri);
    }

    private async Task<ErrorResultDataContract?> GetErrorResult(HttpResponseMessage response)
    {
        ErrorResultDataContract? errorContract = null;
        if (!response.IsSuccessStatusCode)
        {
            var resultString = await response.Content.ReadAsStringAsync();

            if (resultString.Contains("Reference"))
                errorContract = JsonConvert.DeserializeObject<ErrorResultDataContract>(resultString, JsonSettings.FigDefault);
            else
                errorContract = new ErrorResultDataContract("Unknown", response.StatusCode.ToString(), resultString, null);
        }

        return errorContract;
    }
    
    private string GetNewSecret()
    {
        return Guid.NewGuid().ToString();
    }
    
    private HttpClient GetHttpClient()
    {
        var client = _app.CreateClient();
        // Set a longer timeout for integration tests to prevent PipeWriter cancellation
        // during async operations like scheduled changes processing
        client.Timeout = TimeSpan.FromMinutes(2);
        return client;
    }
}