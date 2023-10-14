using System.Text;
using Fig.Api;
using Fig.Api.Secrets;
using Fig.Client;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.Authentication;
using Fig.Contracts.Configuration;
using Fig.Contracts.EventHistory;
using Fig.Contracts.ImportExport;
using Fig.Contracts.LookupTable;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Fig.Contracts.Status;
using Fig.Contracts.WebHook;
using Fig.WebHooks.TestClient;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Test.Common;

public abstract class IntegrationTestBase
{
    protected const string WebHookSecret = "d21b0b4b-b978-4048-85be-eb73e057f6fb";
    private string _originalServerSecret = string.Empty;
    
    private WebApplicationFactory<Program> _app = null!;
    private WebApplicationFactory<Fig.WebHooks.TestClient.FigWebHookAuthMiddleware> _webHookTestApp = null!;
    protected ApiClient ApiClient = null!;
    protected HttpClient WebHookClient = null!;
    protected ApiSettings Settings = null!;
    protected Mock<ISecretStore> secretStoreMock = new();
    protected static string UserName => ApiClient.AdminUserName;

    [OneTimeSetUp]
    public async Task FixtureSetup()
    {
        _webHookTestApp = new WebApplicationFactory<FigWebHookAuthMiddleware>();
        WebHookClient = _webHookTestApp.CreateClient();
        _app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IHttpClientFactory>(new CustomHttpClientFactory(WebHookClient));
                services.Configure<ApiSettings>(opts =>
                {
                    Settings = opts;
                    Settings.DbConnectionString = "Data Source=fig.db;Version=3;New=True";
                    Settings.Secret = "50b93c880cdf4041954da041386d54f9";
                    Settings.TokenLifeMinutes = 60;
                });
                services.AddScoped<ISecretStore>(a => secretStoreMock.Object);
            });
        });
        ApiClient = new ApiClient(_app);
        await ApiClient.Authenticate();
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        _app.Dispose();
        if (File.Exists("fig.db"))
            File.Delete("fig.db");
    }

    [SetUp]
    public async Task Setup()
    {
        Console.WriteLine($"Secret: {Settings.Secret}");
        await ApiClient.Authenticate();
        _originalServerSecret = Settings.Secret;
        await DeleteAllClients();
        await ResetConfiguration();
        await ResetUsers();
        await DeleteAllLookupTables();
        await DeleteAllWebHooks();
        await DeleteAllWebHookClients();
        secretStoreMock.Reset();
    }

    [TearDown]
    public async Task TearDown()
    {
        await DeleteAllClients();
        await ResetConfiguration();
        await ResetUsers();
        await DeleteAllLookupTables();
        await DeleteAllWebHooks();
        await DeleteAllWebHookClients();
        Settings.Secret = _originalServerSecret;
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

    protected async Task<IEnumerable<SettingsClientDefinitionDataContract>> GetAllClients(bool authenticate = true, string? tokenOverride = null)
    {
        var clients = await ApiClient.Get<IEnumerable<SettingsClientDefinitionDataContract>>("/clients", authenticate, tokenOverride: tokenOverride);

        return clients ?? Array.Empty<SettingsClientDefinitionDataContract>();
    }

    protected async Task<HttpResponseMessage> SetSettings(string clientName, IEnumerable<SettingDataContract> settings,
        string? instance = null, bool authenticate = true, string message = "", 
        string? tokenOverride = null, bool validateSuccess = true)
    {
        var contract = new SettingValueUpdatesDataContract(settings, message);
        var requestUri = $"/clients/{Uri.EscapeDataString(clientName)}/settings";
        if (instance != null) requestUri += $"?instance={Uri.EscapeDataString(instance)}";

        return await ApiClient.Put<HttpResponseMessage>(requestUri, contract, authenticate, tokenOverride, validateSuccess);
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

    protected async Task<T> RegisterSettings<T>(string? clientSecret = null,
        string? nameOverride = null,
        List<SettingDataContract>? settingOverrides = null) where T : SettingsBase
    {
        var settings = Activator.CreateInstance<T>();
        var dataContract = settings.CreateDataContract(true);

        if (nameOverride != null)
        {
            dataContract = new SettingsClientDefinitionDataContract(nameOverride, dataContract.Description, dataContract.Instance,
                dataContract.Settings, dataContract.Verifications, dataContract.ClientSettingOverrides);
        }

        if (settingOverrides is not null)
        {
            dataContract.ClientSettingOverrides = settingOverrides;
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
    
    protected async Task<HttpResponseMessage> RunVerification(string clientName, string verificationName,
        string? tokenOverride)
    {
        var uri = $"/clients/{Uri.EscapeDataString(clientName)}/verifications/{verificationName}";

        return await ApiClient.Put<HttpResponseMessage>(uri, null, tokenOverride: tokenOverride, validateSuccess: false);
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
    
    protected async Task DeleteAllWebHooks()
    {
        var webHooks = await GetAllWebHooks();
        foreach (var webHook in webHooks.Where(a => a.Id is not null))
            await DeleteWebHook(webHook.Id!.Value);
    }

    protected async Task<SettingsClientDefinitionDataContract> GetClient(SettingsBase settings)
    {
        return await GetClient(settings.ClientName);
    }
    
    protected async Task<SettingsClientDefinitionDataContract> GetClient(string clientName)
    {
        var clients = await GetAllClients();
        var client = clients.First(a => a.Name == clientName);
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

    protected async Task<EventLogCollectionDataContract> GetEvents(DateTime startTime, DateTime endTime, string? tokenOverride = null)
    {
        var uri = "/events" +
                  $"?startTime={Uri.EscapeDataString(startTime.ToString("o"))}" +
                  $"&endTime={Uri.EscapeDataString(endTime.ToString("o"))}";
        var result = await ApiClient.Get<EventLogCollectionDataContract>(uri, tokenOverride: tokenOverride);
        
        if (result == null)
            throw new ApplicationException($"Expected non null result for get for URI {uri}");

        return result;
    }
    
    protected async Task<long> GetEventCount()
    {
        var uri = "/events/count";
        var result = await ApiClient.Get<EventLogCountDataContract>(uri);
        return result.EventLogCount;
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

    protected async Task<ClientSecretChangeResponseDataContract> ChangeClientSecret(string clientName, string secret, DateTime expiry)
    {
        var request = new ClientSecretChangeRequestDataContract(secret, expiry);
        
        var uri = $"clients/{Uri.EscapeDataString(clientName)}/secret";
        var result = await ApiClient.Put<ClientSecretChangeResponseDataContract>(uri, request);
        
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

    protected async Task<FigDataExportDataContract> ExportData(bool excludeSecrets, string? tokenOverride = null)
    {
        var uri = $"/data?excludeSecrets={excludeSecrets}";
        var result = await ApiClient.Get<FigDataExportDataContract>(uri, tokenOverride: tokenOverride);
        
        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }

    protected async Task<ImportResultDataContract> ImportData(FigDataExportDataContract export)
    {
        const string uri = "data";
        return await ApiClient.Put<ImportResultDataContract>(uri, export);
    }
    
    protected async Task<HttpResponseMessage> ImportData(FigDataExportDataContract export, string tokenOverride, bool validateSuccess)
    {
        const string uri = "data";
        return await ApiClient.Put<HttpResponseMessage>(uri, export, tokenOverride: tokenOverride, validateSuccess: validateSuccess);
    }

    protected async Task<FigValueOnlyDataExportDataContract> ExportValueOnlyData(bool excludeSecrets, string? tokenOverride = null)
    {
        var uri = $"/valueonlydata?excludeSecrets={excludeSecrets}";
        var result = await ApiClient.Get<FigValueOnlyDataExportDataContract>(uri, tokenOverride: tokenOverride);
        
        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }

    protected async Task ImportValueOnlyData(FigValueOnlyDataExportDataContract export)
    {
        const string uri = "valueonlydata";
        await ApiClient.Put<ImportResultDataContract>(uri, export);
    }
    
    protected async Task<HttpResponseMessage> ImportValueOnlyData(FigValueOnlyDataExportDataContract export, string tokenOverride)
    {
        const string uri = "valueonlydata";
        return await ApiClient.Put<HttpResponseMessage>(uri, export, tokenOverride: tokenOverride, validateSuccess: false);
    }

    protected async Task<List<DeferredImportClientDataContract>> GetDeferredImports(string? tokenOverride = null)
    {
        const string uri = "/deferredimport";
        var result = await ApiClient.Get<List<DeferredImportClientDataContract>>(uri, tokenOverride: tokenOverride);
        
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
        bool allowClientOverrides = true,
        string clientOverrideRegex = ".*",
        long delayBeforeMemoryLeakMeasurementsMs = 5000,
        long intervalBetweenMemoryLeakChecksMs = 5000,
        int minimumDataPointsForMemoryLeakCheck = 40,
        string webApplicationBaseAddress = "http://localhost",
        bool useAzureKeyVault = false,
        string? azureKeyVaultName = null)
    {
        return new FigConfigurationDataContract
        {
            AllowNewRegistrations = allowNewRegistrations,
            AllowUpdatedRegistrations = allowUpdatedRegistrations,
            AllowFileImports = allowFileImports,
            AllowOfflineSettings = allowOfflineSettings,
            AllowClientOverrides = allowClientOverrides,
            ClientOverridesRegex = clientOverrideRegex,
            DelayBeforeMemoryLeakMeasurementsMs = delayBeforeMemoryLeakMeasurementsMs,
            IntervalBetweenMemoryLeakChecksMs = intervalBetweenMemoryLeakChecksMs,
            MinimumDataPointsForMemoryLeakCheck = minimumDataPointsForMemoryLeakCheck,
            WebApplicationBaseAddress = webApplicationBaseAddress,
            UseAzureKeyVault = useAzureKeyVault,
            AzureKeyVaultName = azureKeyVaultName
        };
    }

    protected RegisterUserRequestDataContract NewUser(
        string username = "testUser",
        string firstName = "Test",
        string lastName = "user",
        Role role = Role.User,
        string password = "this is a complex password!",
        string clientFilter = ".*")
    {
        return new RegisterUserRequestDataContract(username, firstName, lastName, role, password, clientFilter);
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
        bool liveReload, bool hasConfigurationError = false, List<string>? configurationErrors = null, Guid? runSessionId = null, long memoryUsageBytes = 0, string appVersion = "v1", string figVersion = "v1")

    {
        return new StatusRequestDataContract(runSessionId ?? Guid.NewGuid(),
            uptime,
            lastUpdate,
            pollInterval,
            liveReload,
            figVersion,
            appVersion,
            true,
            liveReload,
            "user1",
            memoryUsageBytes,
            hasConfigurationError,
            configurationErrors ?? Array.Empty<string>().ToList());
    }

    protected async Task<IEnumerable<SettingValueDataContract>> GetHistory(string client, string settingName, string? tokenOverride = null, string? instance = null)
    {
        var uri = $"/clients/{Uri.EscapeDataString(client)}/settings/{Uri.EscapeDataString(settingName)}/history";
        if (instance != null) 
            uri += $"?instance={Uri.EscapeDataString(instance)}";

        var result = await ApiClient.Get<IEnumerable<SettingValueDataContract>>(uri, tokenOverride: tokenOverride);
        
        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }
    
    protected async Task<IEnumerable<ClientStatusDataContract>> GetAllStatuses(string? tokenOverride = null)
    {
        const string uri = "/statuses";
        var result = await ApiClient.Get<IEnumerable<ClientStatusDataContract>>(uri, tokenOverride: tokenOverride);

        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }
    
    protected async Task WaitForCondition(Func<Task<bool>> condition, TimeSpan timeout, Func<string>? message = null)
    {
        var expiry = DateTime.UtcNow + timeout;

        var conditionMet = false;

        while (!conditionMet && DateTime.UtcNow < expiry)
        {
            await Task.Delay(100);
            conditionMet = await condition();
        }

        if (!conditionMet)
        {
            var extraMessage = message != null ? message() : string.Empty;
            Assert.Fail($"Timed out ({timeout}) before condition was met. {extraMessage}");
        }
    }
    
    protected async Task<List<WebHookClientDataContract>> GetAllWebHookClients()
    {
        const string uri = "/webhookclient";
        var result = await ApiClient.Get<List<WebHookClientDataContract>>(uri);
        
        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }
    
    protected async Task<WebHookClientDataContract> CreateWebHookClient(WebHookClientDataContract client)
    {
        const string uri = "/webhookclient";
        var response = await ApiClient.Post(uri, client, authenticate: true);

        var result = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<WebHookClientDataContract>(result);
    }
    
    protected async Task<ErrorResultDataContract?> DeleteWebHookClient(Guid clientId, bool validateSuccess = true)
    {
        var requestUri = $"/webhookclient/{Uri.EscapeDataString(clientId.ToString())}";
        return await ApiClient.Delete(requestUri, validateSuccess: validateSuccess);
    }
    
    protected async Task<List<WebHookDataContract>> GetAllWebHooks()
    {
        const string uri = "/webhooks";
        var result = await ApiClient.Get<List<WebHookDataContract>>(uri);
        
        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }
    
    protected async Task<WebHookDataContract> CreateWebHook(WebHookDataContract webHook)
    {
        const string uri = "/webhooks";
        var response = await ApiClient.Post(uri, webHook, authenticate: true);

        var result = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<WebHookDataContract>(result);
    }
    
    protected async Task DeleteWebHook(Guid webHookId)
    {
        var requestUri = $"/webhooks/{Uri.EscapeDataString(webHookId.ToString())}";
        await ApiClient.Delete(requestUri);
    }
    
    protected async Task<WebHookClientDataContract> CreateTestWebHookClient(string secret)
    {
        var clientContract = new WebHookClientDataContract(null, "MyTest", WebHookClient.BaseAddress, secret);
        return await CreateWebHookClient(clientContract);
    }
    
    protected async Task<IEnumerable<object>> GetWebHookMessages(DateTime from)
    {
        if (!WebHookClient.DefaultRequestHeaders.Contains("Authorization"))
            WebHookClient.DefaultRequestHeaders.Add("Authorization", $"Secret {WebHookSecret}");

        var result = await WebHookClient.GetStringAsync($"{WebHookClient.BaseAddress}?fromTimeUtc={Uri.EscapeDataString(from.ToString("o"))}");

        Assert.That(result, Is.Not.Null);

        return JsonConvert.DeserializeObject<IEnumerable<object>>(result, JsonSettings.FigDefault);
    }

    protected void AssertJsonEquivalence<T>(T actual, T expected)
    {
        var actualJson = JsonConvert.SerializeObject(actual, JsonSettings.FigDefault);
        var expectedJson = JsonConvert.SerializeObject(expected, JsonSettings.FigDefault);

        Assert.That(actualJson, Is.EqualTo(expectedJson));
    }

    private async Task ResetUsers()
    {
        var users = await GetUsers();

        foreach (var user in users.Where(a => a.Username != "admin"))
            await DeleteUser(user.Id);
    }
}