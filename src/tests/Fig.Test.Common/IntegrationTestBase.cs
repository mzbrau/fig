using System.Text;
using Fig.Client;
using Fig.Common.NetStandard.Json;
using Fig.Common.WebHook;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Test.Common;

public abstract class IntegrationTestBase
{
    private WebApplicationFactory<Program> _app = null!;
    protected ApiClient ApiClient = null!;
    protected static string UserName => ApiClient.AdminUserName;

    [OneTimeSetUp]
    public async Task FixtureSetup()
    {
        _app = new WebApplicationFactory<Program>();
        ApiClient = new ApiClient(_app);
        await ApiClient.Authenticate();
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
        await DeleteAllWebHookClients();
    }

    [TearDown]
    public async Task TearDown()
    {
        await DeleteAllClients();
        await ResetConfiguration();
        await ResetUsers();
        await DeleteAllLookupTables();
        await DeleteAllWebHookClients();
    }

    protected async Task<AuthenticateResponseDataContract> Login(bool checkSuccess = true)
    {
        return await ApiClient.Login(checkSuccess);
    }

    protected async Task<AuthenticateResponseDataContract> Login(string username, string password)
    {
        return await ApiClient.Login(username, password);
    }

    protected async Task<List<SettingDataContract>> GetSettingsForClient(string clientName,
        string clientSecret, string? instance = null)
    {
        var requestUri = $"/clients/{Uri.EscapeDataString(clientName)}/settings";
        if (instance != null) 
            requestUri += $"?instance={Uri.EscapeDataString(instance)}";

        var result = await ApiClient.Get<IEnumerable<SettingDataContract>>(requestUri, false, clientSecret);

        return result is not null ? result.ToList() : Array.Empty<SettingDataContract>().ToList();
    }

    protected async Task<IEnumerable<SettingsClientDefinitionDataContract>> GetAllClients(bool authenticate = true)
    {
        var clients = await ApiClient.Get<IEnumerable<SettingsClientDefinitionDataContract>>("/clients", authenticate);

        return clients ?? Array.Empty<SettingsClientDefinitionDataContract>();
    }

    protected async Task SetSettings(string clientName, IEnumerable<SettingDataContract> settings,
        string? instance = null, bool authenticate = true)
    {
        var requestUri = $"/clients/{Uri.EscapeDataString(clientName)}/settings";
        if (instance != null) requestUri += $"?instance={Uri.EscapeDataString(instance)}";

        await ApiClient.Put<StatusCodeResult>(requestUri, settings, authenticate);
    }

    protected async Task<HttpResponseMessage> SetConfiguration(FigConfigurationDataContract configuration,
        string? token = null, bool validateSuccess = true)
    {
        const string requestUri = "/configuration";
        var result = await ApiClient.Put<HttpResponseMessage>(requestUri, configuration, authenticate: true, tokenOverride: token, validateSuccess);

        if (result is null)
            throw new ApplicationException($"Null result for put to uri {requestUri}");

        return result;
    }

    protected async Task DeleteClient(string clientName, string? instance = null, bool authenticate = true)
    {
        var requestUri = $"/clients/{Uri.EscapeDataString(clientName)}";
        if (instance != null) requestUri += $"?instance={Uri.EscapeDataString(instance)}";

        await ApiClient.Delete(requestUri);
    }

    protected async Task<T> RegisterSettings<T>(string? clientSecret = null, string? nameOverride = null) where T : SettingsBase
    {
        var settings = Activator.CreateInstance<T>();
        var dataContract = settings.CreateDataContract(true);

        if (nameOverride != null)
        {
            dataContract = new SettingsClientDefinitionDataContract(nameOverride, dataContract.Instance,
                dataContract.Settings, dataContract.PluginVerifications, dataContract.DynamicVerifications);
        }

        const string requestUri = "/clients";
        await ApiClient.Post(requestUri, dataContract, clientSecret);

        return settings;
    }

    protected async Task<HttpResponseMessage> TryRegisterSettings<T>(string? clientSecret = null) where T : SettingsBase
    {
        var settings = Activator.CreateInstance<T>();
        var dataContract = settings.CreateDataContract(true);
        var json = JsonConvert.SerializeObject(dataContract, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret ?? GetNewSecret());
        return await httpClient.PostAsync("/clients", data);
    }

    protected async Task<VerificationResultDataContract> RunVerification(string clientName, string verificationName,
        bool authenticate = true)
    {
        var uri = $"/clients/{Uri.EscapeDataString(clientName)}/verifications/{verificationName}";

        var result = await ApiClient.Put<VerificationResultDataContract>(uri, null, authenticate);

        if (result == null)
            throw new ApplicationException($"Expected non null result for put for URI {uri}");

        return result;
    }

    protected async Task DeleteAllClients()
    {
        var clients = await GetAllClients();
        foreach (var client in clients)
            await DeleteClient(client.Name, client.Instance);
    }
    
    protected async Task DeleteAllWebHookClients()
    {
        var clients = await GetAllWebHookClients();
        foreach (var client in clients.Where(a => a.Id is not null))
            await DeleteWebHookClient(client.Id!.Value);
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
                errorContract = JsonConvert.DeserializeObject<ErrorResultDataContract>(resultString, JsonSettings.FigDefault);
            else
                errorContract = new ErrorResultDataContract("Unknown", response.StatusCode.ToString(), resultString, null);
        }

        return errorContract;
    }

    protected async Task<EventLogCollectionDataContract> GetEvents(DateTime startTime, DateTime endTime)
    {
        var uri = "/events" +
                  $"?startTime={Uri.EscapeDataString(startTime.ToString("o"))}" +
                  $"&endTime={Uri.EscapeDataString(endTime.ToString("o"))}";
        var result = await ApiClient.Get<EventLogCollectionDataContract>(uri);
        
        if (result == null)
            throw new ApplicationException($"Expected non null result for get for URI {uri}");

        return result;
    }

    protected async Task<Guid> CreateUser(RegisterUserRequestDataContract user)
    {
        const string uri = "/users/register";
        var response = await ApiClient.Post(uri, user, authenticate: true);

        var id = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<Guid>(id);
    }

    protected async Task<UserDataContract> GetUser(Guid id)
    {
        var uri = $"/users/{id}";
        var result = await ApiClient.Get<UserDataContract>(uri);
        
        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }

    protected async Task DeleteUser(Guid id)
    {
        var uri = $"/users/{id}";
        await ApiClient.Delete(uri);
    }

    protected async Task UpdateUser(Guid id, UpdateUserRequestDataContract user)
    {
        var uri = $"/users/{id}";
        await ApiClient.Put<UpdateUserRequestDataContract>(uri, user);
    }

    protected async Task<IEnumerable<UserDataContract>> GetUsers()
    {
        const string uri = "/users";
        var result = await ApiClient.Get<IEnumerable<UserDataContract>>(uri);
        
        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
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
        var uri = $"/data?decryptSecrets={decryptSecrets}";
        var result = await ApiClient.Get<FigDataExportDataContract>(uri);
        
        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }

    protected async Task ImportData(FigDataExportDataContract export)
    {
        const string uri = "data";
        await ApiClient.Put<ImportResultDataContract>(uri, export);
    }
    
    protected async Task<FigValueOnlyDataExportDataContract> ExportValueOnlyData()
    {
        const string uri = "/valueonlydata";
        var result = await ApiClient.Get<FigValueOnlyDataExportDataContract>(uri);
        
        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }

    protected async Task ImportValueOnlyData(FigValueOnlyDataExportDataContract export)
    {
        const string uri = "valueonlydata";
        await ApiClient.Put<ImportResultDataContract>(uri, export);
    }

    protected async Task<List<DeferredImportClientDataContract>> GetDeferredImports()
    {
        const string uri = "/deferredimport";
        var result = await ApiClient.Get<List<DeferredImportClientDataContract>>(uri);
        
        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
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
        const string uri = "/lookuptables";
        await ApiClient.Post(uri, dataContract, authenticate: true);
    }

    protected async Task UpdateLookupTable(LookupTableDataContract dataContract)
    {
        var uri = $"/lookuptables/{dataContract.Id}";
        await ApiClient.Put<LookupTableDataContract>(uri, dataContract);
    }

    protected async Task<IEnumerable<LookupTableDataContract>> GetAllLookupTables()
    {
        var requestUri = "/lookuptables";
        var result = await ApiClient.Get<IEnumerable<LookupTableDataContract>>(requestUri);
        
        return result ?? Array.Empty<LookupTableDataContract>();
    }

    protected async Task DeleteLookupTable(Guid? id)
    {
        var uri = $"/lookuptables/{id}";
        await ApiClient.Delete(uri);
    }

    protected async Task DeleteAllLookupTables()
    {
        var items = await GetAllLookupTables();
        foreach (var item in items)
            await DeleteLookupTable(item.Id);
    }

    protected StatusRequestDataContract CreateStatusRequest(double uptime, DateTime lastUpdate, double pollInterval,
        bool liveReload, bool hasConfigurationError = false, List<string>? configurationErrors = null, Guid? runSessionId = null)

    {
        return new StatusRequestDataContract(runSessionId ?? Guid.NewGuid(),
            uptime,
            lastUpdate,
            pollInterval,
            liveReload,
            "v1",
            "v1",
            true,
            liveReload,
            "user1",
            0,
            hasConfigurationError,
            configurationErrors ?? Array.Empty<string>().ToList());
    }

    protected async Task<IEnumerable<SettingValueDataContract>> GetHistory(string client, string secret, string settingName, bool authenticate = true, string? instance = null)
    {
        var uri = $"/clients/{Uri.EscapeDataString(client)}/settings/{Uri.EscapeDataString(settingName)}/history";
        if (instance != null) 
            uri += $"?instance={Uri.EscapeDataString(instance)}";

        var result = await ApiClient.Get<IEnumerable<SettingValueDataContract>>(uri);
        
        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }
    
    protected async Task<List<WebHookClientDataContract>> GetAllWebHookClients()
    {
        const string uri = "/webhookclient";
        var result = await ApiClient.Get<List<WebHookClientDataContract>>(uri);
        
        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }
    
    protected async Task DeleteWebHookClient(Guid clientId)
    {
        var requestUri = $"/webhookclient/{Uri.EscapeDataString(clientId.ToString())}";
        await ApiClient.Delete(requestUri);
    }
    
    private async Task ResetUsers()
    {
        var users = await GetUsers();

        foreach (var user in users.Where(a => a.Username != "admin"))
            await DeleteUser(user.Id);
    }
}