using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Fig.Client;
using Fig.Contracts;
using Fig.Contracts.Authentication;
using Fig.Contracts.Common;
using Fig.Contracts.Configuration;
using Fig.Contracts.EventHistory;
using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Fig.Contracts.Status;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public abstract class IntegrationTestBase
{
    protected const string UserName = "admin";
    private WebApplicationFactory<Program> _app = null!;
    protected string? BearerToken;

    [OneTimeSetUp]
    public async Task FixtureSetup()
    {
        _app = new WebApplicationFactory<Program>();

        await Authenticate();
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        _app.Dispose();
    }

    [SetUp]
    public async Task Setup()
    {
        await DeleteAllClients();
        await ResetConfiguration();
        await ResetUsers();
        await DeleteAllLookupTables();
    }

    [TearDown]
    public async Task TearDown()
    {
        await DeleteAllClients();
        await ResetConfiguration();
        await ResetUsers();
        await DeleteAllLookupTables();
    }

    protected async Task<List<SettingDataContract>> GetSettingsForClient(string clientName,
        string clientSecret, string? instance = null)
    {
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret);
        var requestUri = $"/clients/{HttpUtility.UrlEncode(clientName)}/settings";
        if (instance != null) requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";

        var result = await httpClient.GetStringAsync(requestUri);

        if (!string.IsNullOrEmpty(result))
            return JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(result)!.ToList();

        return Array.Empty<SettingDataContract>().ToList();
    }

    protected async Task<IEnumerable<SettingsClientDefinitionDataContract>> GetAllClients(bool authenticate = true)
    {
        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.GetStringAsync("/clients");

        Assert.That(result, Is.Not.Null, "Get all clients should succeed.");

        return JsonConvert.DeserializeObject<IEnumerable<SettingsClientDefinitionDataContract>>(result)!;
    }

    protected async Task SetSettings(string clientName, IEnumerable<SettingDataContract> settings,
        string? instance = null, bool authenticate = true)
    {
        var json = JsonConvert.SerializeObject(settings);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var requestUri = $"/clients/{HttpUtility.UrlEncode(clientName)}/settings";
        if (instance != null) requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";

        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.PutAsync(requestUri, data);

        var error = await GetErrorResult(result);
        Assert.That(result.IsSuccessStatusCode, Is.True, $"Set of settings should succeed. {error}");
    }

    protected async Task<HttpResponseMessage> SetConfiguration(FigConfigurationDataContract configuration,
        string? token = null)
    {
        var json = JsonConvert.SerializeObject(configuration);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var requestUri = "/configuration";
        using var httpClient = GetHttpClient();

        httpClient.DefaultRequestHeaders.Add("Authorization", token ?? BearerToken);

        var result = await httpClient.PutAsync(requestUri, data);

        var error = await GetErrorResult(result);
        if (token == null)
            Assert.That(result.IsSuccessStatusCode, Is.True, $"Set of configuration should succeed. {error}");

        return result;
    }

    protected async Task<FigConfigurationDataContract> GetConfiguration(string? token = null)
    {
        using var httpClient = GetHttpClient();

        httpClient.DefaultRequestHeaders.Add("Authorization", token ?? BearerToken);

        var result = await httpClient.GetStringAsync("/configuration");

        Assert.That(result, Is.Not.Null, "Get of configuration should succeed.");

        return JsonConvert.DeserializeObject<FigConfigurationDataContract>(result)!;
    }

    protected async Task DeleteClient(string clientName, string? instance = null, bool authenticate = true)
    {
        var requestUri = $"/clients/{HttpUtility.UrlEncode(clientName)}";
        if (instance != null) requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";

        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.DeleteAsync(requestUri);
        var error = await GetErrorResult(result);
        Assert.That(result.IsSuccessStatusCode, Is.True, $"Delete of clients should succeed. {error}");
    }

    protected async Task<T> RegisterSettings<T>(string? clientSecret = null) where T : SettingsBase
    {
        var settings = Activator.CreateInstance<T>();
        var dataContract = settings.CreateDataContract();
        var json = JsonConvert.SerializeObject(dataContract);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret ?? GetNewSecret());
        var result = await httpClient.PostAsync("/clients", data);

        var error = await GetErrorResult(result);
        Assert.That(result.IsSuccessStatusCode, Is.True,
            $"Registration of settings should succeed. {error}");

        return settings;
    }

    protected async Task<HttpResponseMessage> TryRegisterSettings<T>(string? clientSecret = null) where T : SettingsBase
    {
        var settings = Activator.CreateInstance<T>();
        var dataContract = settings.CreateDataContract();
        var json = JsonConvert.SerializeObject(dataContract);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret ?? GetNewSecret());
        return await httpClient.PostAsync("/clients", data);
    }

    protected async Task<VerificationResultDataContract> RunVerification(string clientName, string verificationName,
        bool authenticate = true)
    {
        var uri = $"/clients/{HttpUtility.UrlEncode(clientName)}/verifications/{verificationName}";

        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var response = await httpClient.PutAsync(uri, null);

        var error = await GetErrorResult(response);
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Verification should not throw an error result. {error}");

        var result = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<VerificationResultDataContract>(result)!;
    }

    protected async Task DeleteAllClients()
    {
        var clients = await GetAllClients();
        foreach (var client in clients)
            await DeleteClient(client.Name, client.Instance);
    }

    protected async Task Authenticate()
    {
        var responseObject = await Login(UserName, "admin");

        BearerToken = responseObject.Token;

        Assert.That(BearerToken, Is.Not.Null, "A bearer token should be set after authentication");
    }

    protected async Task<AuthenticateResponseDataContract> Login(string username, string password,
        bool checkSuccess = true)
    {
        var auth = new AuthenticateRequestDataContract
        {
            Username = username,
            Password = password
        };

        var json = JsonConvert.SerializeObject(auth);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        var response = await httpClient.PostAsync("/users/authenticate", data);

        if (checkSuccess)
        {
            var error = await GetErrorResult(response);
            Assert.That(response.IsSuccessStatusCode, Is.True, $"Authentication should succeed. {error}");
        }

        var responseString = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<AuthenticateResponseDataContract>(responseString)!;
    }

    protected async Task<SettingsClientDefinitionDataContract> GetClient(SettingsBase settings)
    {
        var clients = await GetAllClients();
        var client = clients.First(a => a.Name == settings.ClientName);
        return client;
    }

    protected HttpClient GetHttpClient()
    {
        return _app.CreateClient();
    }

    protected async Task<ErrorResultDataContract?> GetErrorResult(HttpResponseMessage response)
    {
        ErrorResultDataContract? errorContract = null;
        if (!response.IsSuccessStatusCode)
        {
            var resultString = await response.Content.ReadAsStringAsync();

            if (resultString.Contains("Reference"))
                errorContract = JsonConvert.DeserializeObject<ErrorResultDataContract>(resultString);
            else
                errorContract = new ErrorResultDataContract("Unknown", response.StatusCode.ToString(), resultString, null);
        }

        return errorContract;
    }

    protected async Task<EventLogCollectionDataContract> GetEvents(DateTime startTime, DateTime endTime)
    {
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var uri = "/events" +
                  $"?startTime={HttpUtility.UrlEncode(startTime.ToString("o"))}" +
                  $"&endTime={HttpUtility.UrlEncode(endTime.ToString("o"))}";
        var result = await httpClient.GetStringAsync(uri);

        Assert.That(result, Is.Not.Null, "Get events should succeed.");

        return JsonConvert.DeserializeObject<EventLogCollectionDataContract>(result)!;
    }

    protected async Task<Guid> CreateUser(RegisterUserRequestDataContract user)
    {
        var json = JsonConvert.SerializeObject(user);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        var uri = "/users/register";
        var result = await httpClient.PostAsync(uri, data);

        Assert.That((int) result.StatusCode, Is.EqualTo(StatusCodes.Status200OK), "Create user should succeed");
        var id = await result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<Guid>(id);
    }

    protected async Task<UserDataContract> GetUser(Guid id)
    {
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var uri = $"/users/{id}";
        var result = await httpClient.GetStringAsync(uri);

        Assert.That(result, Is.Not.Null, "Get user should succeed.");

        return JsonConvert.DeserializeObject<UserDataContract>(result)!;
    }

    protected async Task DeleteUser(Guid id)
    {
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        var uri = $"/users/{id}";
        var result = await httpClient.DeleteAsync(uri);

        Assert.That((int) result.StatusCode, Is.EqualTo(StatusCodes.Status200OK), "Delete user should succeed");
    }

    protected async Task UpdateUser(Guid id, UpdateUserRequestDataContract user)
    {
        var json = JsonConvert.SerializeObject(user);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        var uri = $"/users/{id}";
        var result = await httpClient.PutAsync(uri, data);

        Assert.That((int) result.StatusCode, Is.EqualTo(StatusCodes.Status200OK), "Update user should succeed");
    }

    protected async Task<IEnumerable<UserDataContract>> GetUsers()
    {
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.GetStringAsync("/users");

        Assert.That(result, Is.Not.Null, "Get users should succeed.");

        return JsonConvert.DeserializeObject<IEnumerable<UserDataContract>>(result)!;
    }

    protected async Task ResetUsers()
    {
        var users = await GetUsers();

        foreach (var user in users.Where(a => a.Username != UserName))
            await DeleteUser(user.Id);
    }

    protected async Task<StatusResponseDataContract> GetStatus(string clientName, string? clientSecret,
        StatusRequestDataContract status)
    {
        var json = JsonConvert.SerializeObject(status);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret);
        var response = await httpClient.PutAsync($"statuses/{clientName}", data);

        var error = await GetErrorResult(response);
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Getting status should succeed. {error}");

        var result = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<StatusResponseDataContract>(result)!;
    }

    protected async Task<FigDataExportDataContract> ExportData(bool decryptSecrets)
    {
        using var httpClient = GetHttpClient();

        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.GetStringAsync($"/data?decryptSecrets={decryptSecrets}");

        Assert.That(result, Is.Not.Null, "Export should succeed.");

        return JsonConvert.DeserializeObject<FigDataExportDataContract>(result)!;
    }

    protected async Task ImportData(FigDataExportDataContract export)
    {
        using var httpClient = GetHttpClient();

        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var json = JsonConvert.SerializeObject(export);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var result = await httpClient.PutAsync("data", data);

        Assert.That(result.IsSuccessStatusCode, Is.True, "Import should succeed.");
    }

    protected string GetNewSecret()
    {
        return Guid.NewGuid().ToString();
    }

    protected string GetConfigImportPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var path = Path.Combine(appData, "Fig", "ConfigImport");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }

    protected FigConfigurationDataContract CreateConfiguration(
        bool allowNewRegistrations = true,
        bool allowUpdatedRegistrations = true,
        bool allowFileImports = true,
        bool allowOfflineSettings = true,
        bool allowDynamicVerifications = true)
    {
        return new FigConfigurationDataContract
        {
            AllowNewRegistrations = allowNewRegistrations,
            AllowUpdatedRegistrations = allowUpdatedRegistrations,
            AllowFileImports = allowFileImports,
            AllowOfflineSettings = allowOfflineSettings,
            AllowDynamicVerifications = allowDynamicVerifications
        };
    }

    protected RegisterUserRequestDataContract NewUser(
        string username = "testUser",
        string firstName = "Test",
        string lastName = "user",
        Role role = Role.User,
        string password = "this is a complex password!")
    {
        return new RegisterUserRequestDataContract(username, firstName, lastName, role, password);
    }

    protected async Task ResetConfiguration()
    {
        await SetConfiguration(CreateConfiguration());
    }

    protected async Task AddLookupTable(LookupTableDataContract dataContract)
    {
        var json = JsonConvert.SerializeObject(dataContract);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        var result = await httpClient.PostAsync("/lookuptables", data);

        var error = await GetErrorResult(result);
        Assert.That(result.IsSuccessStatusCode, Is.True, $"Post of lookup table should succeed. {error}");
    }

    protected async Task UpdateLookupTable(LookupTableDataContract dataContract)
    {
        var json = JsonConvert.SerializeObject(dataContract);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        var result = await httpClient.PutAsync($"/lookuptables/{dataContract.Id}", data);

        var error = await GetErrorResult(result);
        Assert.That(result.IsSuccessStatusCode, Is.True, $"Put of lookup table should succeed. {error}");
    }

    protected async Task<IEnumerable<LookupTableDataContract>> GetAllLookupTables()
    {
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        var requestUri = "/lookuptables";

        var result = await httpClient.GetStringAsync(requestUri);

        if (!string.IsNullOrEmpty(result))
            return JsonConvert.DeserializeObject<IEnumerable<LookupTableDataContract>>(result)!.ToList();

        return Array.Empty<LookupTableDataContract>().ToList();
    }

    protected async Task DeleteLookupTable(Guid? id)
    {
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        var uri = $"/lookuptables/{id}";
        var result = await httpClient.DeleteAsync(uri);

        var error = await GetErrorResult(result);
        Assert.That(result.IsSuccessStatusCode, Is.True, $"Delete of lookup table should succeed. {error}");
    }

    protected async Task DeleteAllLookupTables()
    {
        var items = await GetAllLookupTables();
        foreach (var item in items)
            await DeleteLookupTable(item.Id);
    }

    protected StatusRequestDataContract CreateStatusRequest(double uptime, DateTime lastUpdate, double pollInterval,
        bool liveReload)
    {
        return new StatusRequestDataContract(Guid.NewGuid(),
            uptime,
            lastUpdate,
            pollInterval,
            liveReload,
            "v1",
            "v1",
            true,
            liveReload,
            "user1",
            0);
    }
}