using System.Net;
using System.Reflection;
using System.Text;
using Fig.Api;
using Fig.Api.Secrets;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Configuration;
using Fig.Client.ExtensionMethods;
using Fig.Client.Testing.Integration;
using Fig.Client.Workers;
using Fig.Client.Abstractions.Data;
using Fig.Client.Abstractions.LookupTable;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.Authentication;
using Fig.Contracts.CheckPoint;
using Fig.Contracts.Configuration;
using Fig.Contracts.CustomActions;
using Fig.Contracts.EventHistory;
using Fig.Contracts.Health;
using Fig.Contracts.ImportExport;
using Fig.Contracts.LookupTable;
using Fig.Contracts.Scheduling;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.Status;
using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;
using Fig.Test.Common.TestSettings;
using Fig.WebHooks.TestClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NHibernate;
using NUnit.Framework;
using Environment = System.Environment;

namespace Fig.Test.Common;

public abstract class IntegrationTestBase
{
    protected const string WebHookSecret = "d21b0b4b-b978-4048-85be-eb73e057f6fb";
    private string _originalServerSecret = string.Empty;

    private WebApplicationFactory<ApiSettings> _app = null!;
    private WebApplicationFactory<FigWebHookAuthMiddleware> _webHookTestApp = null!;
    protected ApiClient ApiClient = null!;
    protected HttpClient WebHookClient = null!;

    protected readonly ApiSettings Settings = new()
        { DbConnectionString = "Server=localhost;Database=Fig;Trusted_Connection=true;TrustServerCertificate=true;" };

    protected readonly ConfigReloader<ApiSettings> ConfigReloader = new();
    protected readonly Mock<ISecretStore> SecretStoreMock = new();
    protected static string UserName => ApiClient.AdminUserName;
    protected readonly List<WebApplication> ConfigProviderApps = new();
    protected readonly List<IConfigurationRoot> ConfigRoots = new();

    [OneTimeSetUp]
    public async Task FixtureSetup()
    {
        Settings.DbConnectionString = "Data Source=fig.db;Version=3;New=True";
        Settings.Secret = "50b93c880cdf4041954da041386d54f9";
        Settings.TokenLifeMinutes = 60;
        Settings.SchedulingCheckIntervalMs = 547;
        Settings.TimeMachineCheckIntervalMs = 1002;
        Settings.ImportFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Fig",
            "ConfigImport");
        var reloadableSource = new ReloadableConfigurationSource<ApiSettings>
        {
            ConfigReloader = ConfigReloader,
            SettingsType = typeof(ApiSettings),
            InitialConfiguration = Settings,
            SectionNameOverride = "ApiSettings"
        };

        _webHookTestApp = new WebApplicationFactory<FigWebHookAuthMiddleware>();
        WebHookClient = _webHookTestApp.CreateClient();
        // Set a longer timeout for webhook client to prevent PipeWriter cancellation
        WebHookClient.Timeout = TimeSpan.FromMinutes(2);
        _app = new WebApplicationFactory<ApiSettings>().WithWebHostBuilder(builder =>
        {
            IConfigurationRoot? configuration = null;
            builder.ConfigureAppConfiguration((services, config) =>
            {
                config.Sources.Clear();
                config.Add(reloadableSource);
                configuration = config.Build();
            });
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IHttpClientFactory>(new CustomHttpClientFactory(WebHookClient));
                services.AddScoped<ISecretStore>(a => SecretStoreMock.Object);
                if (configuration is not null)
                    services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));
            });
        });

        ApiClient = new ApiClient(_app);
        await ApiClient.Authenticate();
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        // Explicitly dispose the NHibernate SessionFactory to close all database connections
        var sessionFactory = _app.Services.GetService<ISessionFactory>();
        sessionFactory?.Close();
        sessionFactory?.Dispose();
        
        _app.Dispose();
        _webHookTestApp.Dispose();
        
        // Give SQLite a moment to release file locks
        Thread.Sleep(100);
        
        if (File.Exists("fig.db"))
        {
            try
            {
                File.Delete("fig.db");
            }
            catch (IOException ex)
            {
                // Log the error but don't fail the test - the file will be cleaned up by the next run
                Console.WriteLine($"Warning: Could not delete fig.db: {ex.Message}");
            }
        }
    }

    [SetUp]
    public virtual async Task Setup()
    {
        await ApiClient.Authenticate();
        _originalServerSecret = Settings.Secret;
        Settings.DisableTransactionMiddleware = false;
        ConfigReloader.Reload(Settings);
        await DeleteAllClients();
        await ResetConfiguration();
        await ResetUsers();
        await DeleteAllLookupTables();
        await DeleteAllWebHooks();
        await DeleteAllWebHookClients();
        await DeleteAllScheduledChanges();
        await DeleteAllCheckPointTriggers();
        await DeleteAllDeferredImports();
        await ClearClientRegistrationHistory();
        SecretStoreMock.Reset();
        RegisteredProviders.Clear();
        SecretStoreMock.Setup(a => a.GetSecrets(It.IsAny<List<string>>()))
            .ReturnsAsync([]);
    }

    [TearDown]
    public async Task TearDown()
    {
        foreach (var configRoot in ConfigRoots)
        {
            (configRoot as IDisposable)?.Dispose();
        }

        ConfigRoots.Clear();

        foreach (var configProvider in ConfigProviderApps)
        {
            await configProvider.StopAsync();
            await configProvider.DisposeAsync();
        }

        ConfigProviderApps.Clear();

        await DeleteAllClients();
        await ResetConfiguration();
        await ResetUsers();
        await DeleteAllLookupTables();
        await DeleteAllWebHooks();
        await DeleteAllWebHookClients();
        await DeleteAllScheduledChanges();
        await DeleteAllCheckPointTriggers();
        await DeleteAllDeferredImports();
        await ClearClientRegistrationHistory();

        Settings.Secret = _originalServerSecret;
        ConfigReloader.Reload(Settings);
        RegisteredProviders.Clear();
    }

    protected async Task<AuthenticateResponseDataContract> Login(bool checkSuccess = true)
    {
        return await ApiClient.Login(checkSuccess);
    }

    protected void DisableTimeMachineWorker()
    {
        Settings.TimeMachineCheckIntervalMs = 0;
        ConfigReloader.Reload(Settings);
    }
    
    protected void DisableTransactionMiddleware()
    {
        Settings.DisableTransactionMiddleware = true;
        ConfigReloader.Reload(Settings);
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

    protected async Task<IEnumerable<SettingsClientDefinitionDataContract>> GetAllClients(bool authenticate = true,
        string? tokenOverride = null)
    {
        var clients =
            await ApiClient.Get<IEnumerable<SettingsClientDefinitionDataContract>>("/clients", authenticate,
                tokenOverride: tokenOverride);

        return clients ?? Array.Empty<SettingsClientDefinitionDataContract>();
    }

    protected async Task<HttpResponseMessage> SetSettings(string clientName, IEnumerable<SettingDataContract> settings,
        string? instance = null, bool authenticate = true, string message = "",
        string? tokenOverride = null, bool validateSuccess = true, DateTime? applyAt = null, DateTime? revertAt = null)
    {
        ScheduleDataContract? schedule = null;
        if (applyAt.HasValue || revertAt.HasValue)
        {
            schedule = new ScheduleDataContract(applyAt, revertAt);
        }

        var contract = new SettingValueUpdatesDataContract(settings, message, schedule);
        var requestUri = $"/clients/{Uri.EscapeDataString(clientName)}/settings";
        if (instance != null) requestUri += $"?instance={Uri.EscapeDataString(instance)}";

        return await ApiClient.Put<HttpResponseMessage>(requestUri, contract, authenticate, tokenOverride,
            validateSuccess) ?? throw new InvalidOperationException("API call returned null");
    }

    protected async Task<HttpResponseMessage> SetConfiguration(FigConfigurationDataContract configuration,
        string? token = null, bool validateSuccess = true)
    {
        const string requestUri = "/configuration";
        var result = await ApiClient.Put<HttpResponseMessage>(requestUri, configuration, authenticate: true,
            tokenOverride: token, validateSuccess);

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
        List<SettingDataContract>? settingOverrides = null,
        string? instance = null) where T : TestSettingsBase
    {
        var settings = Activator.CreateInstance<T>();
        var dataContract = settings.CreateDataContract(nameOverride ?? settings.ClientName, instance: instance);

        if (settingOverrides is not null)
        {
            dataContract.ClientSettingOverrides = settingOverrides;
        }

        const string requestUri = "/clients";
        await ApiClient.Post(requestUri, dataContract, clientSecret);

        return settings;
    }

    protected (IOptionsMonitor<T> options, IConfigurationRoot config) InitializeConfigurationProvider<T>(
        string clientSecret, string? instanceOverride = null, bool addLookupProviders = false) where T : TestSettingsBase
    {
        var builder = WebApplication.CreateBuilder();
        var settings = Activator.CreateInstance<T>();

        var loggerFactory = LoggerFactory.Create(b => { b.AddConsole(); });

        var configuration = new ConfigurationBuilder()
            .AddFig<T>(o =>
            {
                o.ClientName = settings.ClientName;
                o.HttpClient = GetHttpClient();
                o.ClientSecretOverride = clientSecret;
                o.LoggerFactory = loggerFactory;
                o.InstanceOverride = instanceOverride;
                // Use a shorter delay for tests when lookup providers are used
                if (addLookupProviders)
                {
                    o.LookupTableRegistrationDelay = TimeSpan.FromMilliseconds(100);
                }
            }).Build();

        builder.Services.Configure<T>(configuration);

        if (addLookupProviders)
        {
            builder.Services.AddSingleton<IHostedService, FigLookupWorker<T>>();
            builder.Services.AddSingleton<ILookupProvider, TestLookupProvider>();
            builder.Services.AddSingleton<ILookupProvider, TestCategoryLookupProvider>();
            builder.Services.AddSingleton<IKeyedLookupProvider, TestKeyedLookupProvider>();
        }

        var app = builder.Build();

        var options = app.Services.GetRequiredService<IOptionsMonitor<T>>();
        ConfigProviderApps.Add(app);
        ConfigRoots.Add(configuration);

        if (addLookupProviders)
        {
            // Start the app's hosted services (including FigLookupWorker)
            // StartAsync starts the app but doesn't block - the app continues running
            app.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            
            // Manually trigger lookup table registration after a short delay
            // This is a workaround for the hosted service not completing before app shutdown in tests
            var clientName = settings.ClientName;
            Task.Run(async () =>
            {
                await Task.Delay(100); // Match the configured LookupTableRegistrationDelay
                
                var scope = app.Services.CreateScope();
                try
                {
                    var lookupProviders = scope.ServiceProvider.GetServices<ILookupProvider>();
                    var keyedLookupProviders = scope.ServiceProvider.GetServices<IKeyedLookupProvider>();
                    var figOptions = scope.ServiceProvider.GetRequiredService<IOptions<FigOptions>>();
                    
                    // Get valid lookup table names from the settings
                    var validLookupTableNames = GetValidLookupTableNames<T>();
                    
                    // Register regular lookup providers
                    foreach (var provider in lookupProviders)
                    {
                        if (validLookupTableNames.Contains(provider.LookupName))
                        {
                            var items = await provider.GetItems();
                            await RegisterLookupTableDirect(clientName, provider.LookupName, items);
                        }
                    }
                    
                    // Register keyed lookup providers
                    foreach (var provider in keyedLookupProviders)
                    {
                        if (validLookupTableNames.Contains(provider.LookupName))
                        {
                            var items = await provider.GetItems();
                            var flattenedItems = new Dictionary<string, string?>();
                            foreach (var kvp in items)
                            {
                                foreach (var innerKvp in kvp.Value)
                                {
                                    flattenedItems[$"[{kvp.Key}]{innerKvp.Key}"] = innerKvp.Value;
                                }
                            }
                            await RegisterLookupTableDirect(clientName, provider.LookupName, flattenedItems);
                        }
                    }
                }
                finally
                {
                    scope.Dispose();
                }
            });
        }
        
        return (options, configuration);
    }
    
    private HashSet<string> GetValidLookupTableNames<T>() where T : TestSettingsBase
    {
        var lookupTableNames = new HashSet<string>();
        var settingsType = typeof(T);
        
        var properties = settingsType.GetProperties();
        foreach (var property in properties)
        {
            var settingAttribute = property.GetCustomAttribute<Client.Abstractions.Attributes.SettingAttribute>();
            var lookupTableAttribute = property.GetCustomAttribute<Client.Abstractions.Attributes.LookupTableAttribute>();
            
            if (settingAttribute != null && lookupTableAttribute != null)
            {
                lookupTableNames.Add(lookupTableAttribute.LookupTableKey);
            }
        }
        
        return lookupTableNames;
    }
    
    private async Task RegisterLookupTableDirect(string clientName, string lookupName, Dictionary<string, string?> items)
    {
        // Use the API client to register the lookup table with client name prefix
        var fullLookupName = $"{clientName}:{lookupName}";
        var lookupTable = new LookupTableDataContract(null, fullLookupName, items, true);
        await AddLookupTable(lookupTable);
    }

    protected async Task<HttpResponseMessage> TryRegisterSettings<T>(string? clientSecret = null)
        where T : TestSettingsBase
    {
        var settings = Activator.CreateInstance<T>();
        var dataContract = settings.CreateDataContract(settings.ClientName);
        var json = JsonConvert.SerializeObject(dataContract, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret ?? GetNewSecret());
        return await httpClient.PostAsync("/clients", data);
    }

    protected async Task DeleteAllClients()
    {
        var clients = (await GetAllClients()).ToList();
        foreach (var client in clients)
        {
            await DeleteClient(client.Name, client.Instance);
        }
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

    protected async Task DeleteAllScheduledChanges()
    {
        var changes = await GetScheduledChanges();
        foreach (var change in changes.Changes)
            await DeleteScheduledChange(change.Id, true);
    }

    protected async Task DeleteAllCheckPointTriggers()
    {
        var requestUri = "/timemachine";
        await ApiClient.Delete(requestUri);
    }

    protected async Task WaitForNoRecentCheckpoints()
    {
        // Wait until there are no checkpoints created in the last 2 seconds.
        // This ensures any cleanup-triggered checkpoints have completed even on
        // slower CI machines. To avoid transient races, require two consecutive
        // no-checkpoint observations spaced briefly apart.
        await WaitForCondition(
            async () =>
            {
                var recentStart = DateTime.UtcNow.AddSeconds(-2);
                var checkpoints = await GetCheckpoints(recentStart, DateTime.UtcNow);
                if (checkpoints.CheckPoints.Any()) return false;

                // verify stability
                await Task.Delay(200);
                checkpoints = await GetCheckpoints(recentStart, DateTime.UtcNow);
                return !checkpoints.CheckPoints.Any();
            },
            // allow more time on slower machines
            TimeSpan.FromSeconds(10));
    }

    protected async Task<SettingsClientDefinitionDataContract> GetClient(TestSettingsBase settings)
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
                errorContract =
                    JsonConvert.DeserializeObject<ErrorResultDataContract>(resultString, JsonSettings.FigDefault);
            else
                errorContract =
                    new ErrorResultDataContract("Unknown", response.StatusCode.ToString(), resultString, null);
        }

        return errorContract;
    }

    protected async Task<EventLogCollectionDataContract> GetEvents(DateTime startTime, DateTime endTime,
        string? tokenOverride = null)
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
        return result?.EventLogCount ?? 0;
    }

    protected async Task<CheckPointCollectionDataContract> GetCheckpoints(DateTime startTime, DateTime endTime,
        string? tokenOverride = null)
    {
        var uri = "/timemachine" +
                  $"?startTime={Uri.EscapeDataString(startTime.ToString("o"))}" +
                  $"&endTime={Uri.EscapeDataString(endTime.ToString("o"))}";
        var result = await ApiClient.Get<CheckPointCollectionDataContract>(uri, tokenOverride: tokenOverride);

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

    protected async Task<ClientSecretChangeResponseDataContract> ChangeClientSecret(string clientName, string secret,
        DateTime expiry)
    {
        var request = new ClientSecretChangeRequestDataContract(secret, expiry);

        var uri = $"clients/{Uri.EscapeDataString(clientName)}/secret";
        var result = await ApiClient.Put<ClientSecretChangeResponseDataContract>(uri, request);

        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }

    protected async Task<StatusResponseDataContract> GetStatus(string clientName, string? clientSecret,
        StatusRequestDataContract status, string? instance = null)
    {
        var json = JsonConvert.SerializeObject(status);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        if (!string.IsNullOrEmpty(clientSecret))
            httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret);

        var uri = $"statuses/{Uri.EscapeDataString(clientName)}";
        if (instance != null)
            uri += $"?instance={Uri.EscapeDataString(instance)}";
        
        var response = await httpClient.PutAsync(uri, data);

        var error = await GetErrorResult(response);
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Getting status should succeed. {error}");

        var result = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<StatusResponseDataContract>(result)!;
    }

    protected async Task SetLiveReload(bool liveReload, Guid runSessionId)
    {
        var uri = $"statuses/{runSessionId}/liveReload?liveReload={liveReload}";
        await ApiClient.PutAndVerify(uri, null, HttpStatusCode.OK);
    }

    protected async Task RequestRestart(Guid runSessionId)
    {
        var uri = $"statuses/{runSessionId}/restart";
        await ApiClient.PutAndVerify(uri, null, HttpStatusCode.OK);
    }

    protected async Task<FigDataExportDataContract> ExportData(string? tokenOverride = null)
    {
        var uri = $"/data";
        var result = await ApiClient.Get<FigDataExportDataContract>(uri, tokenOverride: tokenOverride);

        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }

    protected async Task<FigDataExportDataContract> ExportData(bool includeLastChanged, string? tokenOverride = null)
    {
        var uri = "/data";
        if (includeLastChanged)
            uri += "?includeLastChanged=true";

        var result = await ApiClient.Get<FigDataExportDataContract>(uri, tokenOverride: tokenOverride);

        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }

    protected async Task<IEnumerable<SettingValueDataContract>> GetLastChangedForAllSettings(
        string clientName, string? instance = null, string? tokenOverride = null)
    {
        var uri = $"/clients/{Uri.EscapeDataString(clientName)}/settings/lastchanged";
        if (instance != null)
            uri += $"?instance={Uri.EscapeDataString(instance)}";

        var result = await ApiClient.Get<IEnumerable<SettingValueDataContract>>(uri, tokenOverride: tokenOverride);

        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }

    protected async Task<IList<ClientSettingsLastChangedDataContract>> GetLastChangedForAllClientsSettings(
        string? tokenOverride = null)
    {
        const string uri = "/clients/settings/lastchanged";

        var result = await ApiClient.Get<IList<ClientSettingsLastChangedDataContract>>(uri, tokenOverride: tokenOverride);

        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }

    protected async Task<ImportResultDataContract> ImportData(FigDataExportDataContract export)
    {
        const string uri = "data";
        return await ApiClient.Put<ImportResultDataContract>(uri, export) ??
               throw new InvalidOperationException("API call returned null");
    }

    protected async Task<HttpResponseMessage> ImportData(FigDataExportDataContract export, string tokenOverride,
        bool validateSuccess)
    {
        const string uri = "data";
        return await ApiClient.Put<HttpResponseMessage>(uri, export, tokenOverride: tokenOverride,
            validateSuccess: validateSuccess) ?? throw new InvalidOperationException("API call returned null");
    }

    protected async Task<FigValueOnlyDataExportDataContract> ExportValueOnlyData(string? tokenOverride = null, bool excludeEnvironmentSpecific = false)
    {
        var queryString = excludeEnvironmentSpecific ? "?excludeEnvironmentSpecific=true" : "";
        var uri = $"/valueonlydata{queryString}";
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

    protected async Task<HttpResponseMessage> ImportValueOnlyData(FigValueOnlyDataExportDataContract export,
        string tokenOverride)
    {
        const string uri = "valueonlydata";
        return await ApiClient.Put<HttpResponseMessage>(uri, export, tokenOverride: tokenOverride,
            validateSuccess: false) ?? throw new InvalidOperationException("API call returned null");
    }

    protected async Task<List<DeferredImportClientDataContract>> GetDeferredImports(string? tokenOverride = null)
    {
        const string uri = "/deferredimport";
        var result = await ApiClient.Get<List<DeferredImportClientDataContract>>(uri, tokenOverride: tokenOverride);

        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }

    protected async Task DeleteAllDeferredImports()
    {
        const string uri = "/data/deferredimports";
        await ApiClient.Delete(uri);
    }

    protected async Task ClearClientRegistrationHistory()
    {
        const string uri = "/clientregistrationhistory";
        await ApiClient.Delete(uri);
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
        bool allowClientOverrides = false,
        string clientOverrideRegex = ".*",
        string webApplicationBaseAddress = "http://localhost",
        bool useAzureKeyVault = false,
        string? azureKeyVaultName = null,
        double? pollIntervalOverrideMs = null,
        bool allowDisplayScripts = false,
        bool enableTimeMachine = true,
        int? timeMachineCleanupDays = 90,
        int? eventLogsCleanupDays = null,
        int? apiStatusCleanupDays = 90,
        int? settingHistoryCleanupDays = null)
    {
        return new FigConfigurationDataContract
        {
            AllowNewRegistrations = allowNewRegistrations,
            AllowUpdatedRegistrations = allowUpdatedRegistrations,
            AllowFileImports = allowFileImports,
            AllowOfflineSettings = allowOfflineSettings,
            AllowClientOverrides = allowClientOverrides,
            ClientOverridesRegex = clientOverrideRegex,
            WebApplicationBaseAddress = webApplicationBaseAddress,
            UseAzureKeyVault = useAzureKeyVault,
            AzureKeyVaultName = azureKeyVaultName,
            PollIntervalOverride = pollIntervalOverrideMs,
            AllowDisplayScripts = allowDisplayScripts,
            EnableTimeMachine = enableTimeMachine,
            TimeMachineCleanupDays = timeMachineCleanupDays,
            EventLogsCleanupDays = eventLogsCleanupDays,
            ApiStatusCleanupDays = apiStatusCleanupDays,
            SettingHistoryCleanupDays = settingHistoryCleanupDays
        };
    }

    protected RegisterUserRequestDataContract NewUser(
        string username = "testUser",
        string firstName = "Test",
        string lastName = "user",
        Role role = Role.User,
        string password = "this is a complex password!",
        string clientFilter = ".*",
        List<Classification>? allowedClassifications = null)
    {
        return new RegisterUserRequestDataContract(username, firstName, lastName, role, password, clientFilter,
            allowedClassifications ?? Enum.GetValues<Classification>().ToList());
    }

    protected async Task ResetConfiguration()
    {
        await SetConfiguration(CreateConfiguration(enableTimeMachine: false));
    }

    protected async Task EnableTimeMachine()
    {
        await SetConfiguration(CreateConfiguration(enableTimeMachine: true));
    }

    protected async Task<HttpResponseMessage> AddLookupTable(LookupTableDataContract dataContract, bool validateSuccess = true)
    {
        const string uri = "/lookuptables";
        return await ApiClient.Post(uri, dataContract, authenticate: true, validateSuccess: validateSuccess);
    }

    protected async Task UpdateLookupTable(LookupTableDataContract dataContract)
    {
        var uri = $"/lookuptables/{dataContract.Id}";
        await ApiClient.Put<LookupTableDataContract>(uri, dataContract);
    }

    protected async Task<List<LookupTableDataContract>> GetAllLookupTables()
    {
        var requestUri = "/lookuptables";
        var result = await ApiClient.Get<IEnumerable<LookupTableDataContract>>(requestUri);

        return result?.ToList() ?? [];
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

    protected StatusRequestDataContract CreateStatusRequest(DateTime startTime, DateTime lastUpdate,
        double pollInterval,
        bool liveReload, bool hasConfigurationError = false, List<string>? configurationErrors = null,
        Guid? runSessionId = null, long memoryUsageBytes = 0, string appVersion = "v1",
        string figVersion = "v1", HealthDataContract? health = null)

    {
        return new StatusRequestDataContract(runSessionId ?? Guid.NewGuid(),
            startTime,
            lastUpdate,
            pollInterval,
            figVersion,
            appVersion,
            true,
            liveReload,
            "user1",
            memoryUsageBytes,
            health);
    }

    protected async Task<IEnumerable<SettingValueDataContract>> GetHistory(string client, string settingName,
        string? tokenOverride = null, string? instance = null)
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
        return JsonConvert.DeserializeObject<WebHookClientDataContract>(result) ??
               throw new InvalidOperationException("Deserialization returned null");
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
        return JsonConvert.DeserializeObject<WebHookDataContract>(result) ??
               throw new InvalidOperationException("Deserialization returned null");
    }

    protected async Task DeleteWebHook(Guid webHookId)
    {
        var requestUri = $"/webhooks/{Uri.EscapeDataString(webHookId.ToString())}";
        await ApiClient.Delete(requestUri);
    }

    protected async Task<WebHookClientDataContract> CreateTestWebHookClient(string secret)
    {
        var clientContract = new WebHookClientDataContract(null, "MyTest",
            WebHookClient.BaseAddress ?? throw new InvalidOperationException("WebHookClient.BaseAddress is null"),
            secret);
        return await CreateWebHookClient(clientContract);
    }

    protected async Task<IEnumerable<object>> GetWebHookMessages(DateTime from)
    {
        if (!WebHookClient.DefaultRequestHeaders.Contains("Authorization"))
            WebHookClient.DefaultRequestHeaders.Add("Authorization", $"Secret {WebHookSecret}");

        var result =
            await WebHookClient.GetStringAsync(
                $"{WebHookClient.BaseAddress}?fromTimeUtc={Uri.EscapeDataString(from.ToString("o"))}");

        Assert.That(result, Is.Not.Null);

        return JsonConvert.DeserializeObject<IEnumerable<object>>(result, JsonSettings.FigDefault) ??
               throw new InvalidOperationException("Deserialization returned null");
    }

    protected void AssertJsonEquivalence<T>(T actual, T expected)
    {
        var actualJson = JsonConvert.SerializeObject(actual, JsonSettings.FigDefault);
        var expectedJson = JsonConvert.SerializeObject(expected, JsonSettings.FigDefault);

        Assert.That(actualJson, Is.EqualTo(expectedJson));
    }

    protected DateTime FiveHundredMillisecondsAgo()
    {
        return DateTime.UtcNow - TimeSpan.FromMilliseconds(500);
    }

    protected async Task<T> RegisterClientAndWaitForCheckpoint<T>(string? secret = null) where T : TestSettingsBase
    {
        var theSecret = secret ?? GetNewSecret();
        var setupStartTime = DateTime.UtcNow;
        var settings = await RegisterSettings<T>(theSecret);

        await WaitForCondition(
            async () => (await GetCheckpoints(setupStartTime, DateTime.UtcNow)).CheckPoints.Count() == 1,
            TimeSpan.FromSeconds(10));

        return settings;
    }

    protected async Task<FigDataExportDataContract?> GetCheckPointData(Guid dataId)
    {
        var uri = $"/timemachine/data?dataId={Uri.EscapeDataString(dataId.ToString())}";
        return await ApiClient.Get<FigDataExportDataContract>(uri);
    }

    protected async Task ApplyCheckPoint(CheckPointDataContract checkPoint)
    {
        var uri = $"/timemachine/{Uri.EscapeDataString(checkPoint.Id.ToString())}";
        await ApiClient.Put<HttpResponseMessage>(uri, null);
    }
    
    /// <summary>
    /// Backdates a checkpoint to a specific timestamp by directly updating the database.
    /// This is useful for testing cleanup functionality that depends on old data.
    /// </summary>
    protected async Task BackdateCheckpoint(Guid checkpointId, DateTime newTimestamp)
    {
        using var scope = GetServiceScope();
        var session = scope.ServiceProvider.GetRequiredService<NHibernate.ISession>();
        using var transaction = session.BeginTransaction();
        
        try
        {
            // Use HQL to update the checkpoint timestamp
            await session.CreateQuery(
                    "update CheckPointBusinessEntity set Timestamp = :newTimestamp where Id = :checkpointId")
                .SetParameter("newTimestamp", newTimestamp)
                .SetParameter("checkpointId", checkpointId)
                .ExecuteUpdateAsync();
            
            await session.FlushAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    /// <summary>
    /// Backdates event logs to a specific timestamp by directly updating the database.
    /// This is useful for testing cleanup functionality that depends on old data.
    /// </summary>
    protected async Task BackdateEventLogs(DateTime startTime, DateTime endTime, DateTime newTimestamp)
    {
        using var scope = GetServiceScope();
        var session = scope.ServiceProvider.GetRequiredService<NHibernate.ISession>();
        using var transaction = session.BeginTransaction();
        
        try
        {
            // Use HQL to update event log timestamps
            await session.CreateQuery(
                    "update EventLogBusinessEntity set Timestamp = :newTimestamp where Timestamp >= :startTime and Timestamp <= :endTime")
                .SetParameter("newTimestamp", newTimestamp)
                .SetParameter("startTime", startTime)
                .SetParameter("endTime", endTime)
                .ExecuteUpdateAsync();
            
            await session.FlushAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    /// <summary>
    /// Directly inserts old, inactive API status records into the database.
    /// This approach is necessary because API status is managed by a background timer
    /// and cannot be forced to update with old timestamps.
    /// Returns the RuntimeIds of the inserted records for verification.
    /// </summary>
    protected async Task<List<Guid>> InsertOldApiStatus(DateTime oldTimestamp, int count = 3)
    {
        var runtimeIds = new List<Guid>();
        using var scope = GetServiceScope();
        var session = scope.ServiceProvider.GetRequiredService<NHibernate.ISession>();
        using var transaction = session.BeginTransaction();
        
        try
        {
            for (int i = 0; i < count; i++)
            {
                var runtimeId = Guid.NewGuid();
                runtimeIds.Add(runtimeId);
                var status = new ApiStatusBusinessEntity
                {
                    RuntimeId = runtimeId,
                    StartTimeUtc = oldTimestamp,
                    LastSeen = oldTimestamp,
                    IpAddress = $"192.168.1.{100 + i}",
                    Hostname = $"test-host-{i}",
                    Version = "1.0.0",
                    MemoryUsageBytes = 1024 * 1024 * 100, // 100 MB
                    RunningUser = "test-user",
                    TotalRequests = 1000,
                    RequestsPerMinute = 10.0,
                    IsActive = false, // Must be false for cleanup to remove them
                    SecretHash = BCrypt.Net.BCrypt.EnhancedHashPassword("test-secret"),
                    ConfigurationErrorDetected = false
                };
                
                await session.SaveAsync(status);
            }
            
            await session.FlushAsync();
            await transaction.CommitAsync();
            return runtimeIds;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    /// <summary>
    /// Counts old, inactive API status records in the database.
    /// </summary>
    protected async Task<int> CountOldInactiveApiStatuses(DateTime cutoffDate)
    {
        using var scope = GetServiceScope();
        var session = scope.ServiceProvider.GetRequiredService<NHibernate.ISession>();
        var count = await session.CreateQuery(
                "select count(*) from ApiStatusBusinessEntity where LastSeen < :cutoffDate and IsActive = false")
            .SetParameter("cutoffDate", cutoffDate)
            .UniqueResultAsync<long>();
        return (int)count;
    }
    
    /// <summary>
    /// Checks if specific API status records exist by their RuntimeIds.
    /// </summary>
    protected async Task<bool> ApiStatusRecordsExist(List<Guid> runtimeIds)
    {
        if (runtimeIds.Count == 0)
            return false;
            
        using var scope = GetServiceScope();
        var session = scope.ServiceProvider.GetRequiredService<NHibernate.ISession>();
        var count = await session.CreateQuery(
                "select count(*) from ApiStatusBusinessEntity where RuntimeId in (:runtimeIds)")
            .SetParameterList("runtimeIds", runtimeIds)
            .UniqueResultAsync<long>();
        return count > 0;
    }
    
    /// <summary>
    /// Deletes old, inactive API status records directly from the database.
    /// This is useful for test cleanup to ensure a clean state before testing.
    /// </summary>
    protected async Task DeleteOldInactiveApiStatuses(DateTime cutoffDate)
    {
        using var scope = GetServiceScope();
        var session = scope.ServiceProvider.GetRequiredService<NHibernate.ISession>();
        using var transaction = session.BeginTransaction();
        
        try
        {
            await session.CreateQuery(
                    "delete from ApiStatusBusinessEntity where LastSeen < :cutoffDate and IsActive = false")
                .SetParameter("cutoffDate", cutoffDate)
                .ExecuteUpdateAsync();
            
            await session.FlushAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    /// <summary>
    /// Gets all API status records from the /apistatus endpoint.
    /// Note: This returns Fig API instance statuses, not client statuses.
    /// </summary>
    protected async Task<List<ApiStatusDataContract>> GetAllApiStatuses()
    {
        const string uri = "/apistatus";
        var result = await ApiClient.Get<List<ApiStatusDataContract>>(uri);
        
        if (result == null)
            throw new ApplicationException($"Expected non null result for get for URI {uri}");

        return result;
    }
    
    /// <summary>
    /// Backdates setting history records to a specific timestamp by directly updating the database.
    /// This is useful for testing cleanup functionality that depends on old data.
    /// </summary>
    protected async Task BackdateSettingHistory(DateTime startTime, DateTime endTime, DateTime newTimestamp)
    {
        using var scope = GetServiceScope();
        var session = scope.ServiceProvider.GetRequiredService<NHibernate.ISession>();
        using var transaction = session.BeginTransaction();
        
        try
        {
            // Use HQL to update setting history ChangedAt timestamps
            await session.CreateQuery(
                    "update SettingValueBusinessEntity set ChangedAt = :newTimestamp where ChangedAt >= :startTime and ChangedAt <= :endTime")
                .SetParameter("newTimestamp", newTimestamp)
                .SetParameter("startTime", startTime)
                .SetParameter("endTime", endTime)
                .ExecuteUpdateAsync();
            
            await session.FlushAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    protected async Task<SchedulingChangesDataContract> GetScheduledChanges(string? tokenOverride = null)
    {
        var requestUri = "/scheduling";
        return await ApiClient.Get<SchedulingChangesDataContract>(requestUri, tokenOverride: tokenOverride) ??
               throw new InvalidOperationException("API call returned null");
    }

    protected async Task<ErrorResultDataContract?> RescheduleChange(Guid changeId, DateTime newExecuteTime,
        bool validateSuccess = true, string? tokenOverride = null)
    {
        var requestUri = $"/scheduling/{changeId}";
        var contract = new RescheduleDeferredChangeDataContract { NewExecuteAtUtc = newExecuteTime };
        return await ApiClient.Put<ErrorResultDataContract>(requestUri, contract, validateSuccess: validateSuccess,
            tokenOverride: tokenOverride);
    }

    protected async Task<ErrorResultDataContract?> DeleteScheduledChange(Guid changeId, bool validateSuccess,
        string? tokenOverride = null)
    {
        var requestUri = $"/scheduling/{changeId}";
        return await ApiClient.Delete(requestUri, validateSuccess: validateSuccess, tokenOverride: tokenOverride);
    }

    protected async Task<CustomActionExecutionHistoryDataContract?> GetExecutionHistory(string clientName,
        string customActionName, DateTime startTime, DateTime endTime)
    {
        var uri = $"customactions/history/{Uri.EscapeDataString(clientName)}/{Uri.EscapeDataString(customActionName)}";
        uri += $"?startTime={startTime:yyyy-MM-ddTHH:mm:ss.fffZ}&endTime={endTime:yyyy-MM-ddTHH:mm:ss.fffZ}";

        var result = await ApiClient.Get<CustomActionExecutionHistoryDataContract>(uri);
        return result;
    }

    protected async Task<HttpResponseMessage> RegisterCustomActions(string clientName, string secret,
        IEnumerable<CustomActionDefinitionDataContract> customActions, bool validateSuccess = true)
    {
        var request = new CustomActionRegistrationRequestDataContract(clientName, customActions.ToList());
        return await ApiClient.Post("customactions/register", request, secret, validateSuccess: validateSuccess);
    }

    protected async Task<CustomActionExecutionResponseDataContract?> ExecuteAction(string clientName,
        CustomActionDefinitionDataContract action, Guid? runSessionId = null, bool validateSuccess = true)
    {
        var request = new CustomActionExecutionRequestDataContract(action.Name, runSessionId ?? Guid.NewGuid());
        var uri = $"customactions/execute/{Uri.EscapeDataString(clientName)}";
        return await ApiClient.Put<CustomActionExecutionResponseDataContract>(uri, request, authenticate: true,
            validateSuccess: validateSuccess);
    }

    protected async Task<CustomActionExecutionStatusDataContract?> GetExecutionStatus(Guid executionId)
    {
        var uri = $"customactions/status/{executionId}";
        return await ApiClient.Get<CustomActionExecutionStatusDataContract>(uri, authenticate: true);
    }

    protected async Task<IEnumerable<CustomActionPollResponseDataContract>> PollForExecutionRequests(string clientName,
        Guid runSession, string clientSecret)
    {
        var uri = $"customactions/poll/{Uri.EscapeDataString(clientName)}?runSessionId={runSession}";
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret);
        var response = await httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result =
            JsonConvert.DeserializeObject<IEnumerable<CustomActionPollResponseDataContract>>(content,
                JsonSettings.FigDefault);
        return result ?? [];
    }

    protected async Task SubmitActionResult(string clientName, string secret,
        CustomActionExecutionResultsDataContract result)
    {
        var uri = $"customactions/results/{Uri.EscapeDataString(clientName)}";
        await ApiClient.Post(uri, result, secret);
    }

    private async Task ResetUsers()
    {
        var users = await GetUsers();

        foreach (var user in users.Where(a => a.Username != "admin"))
            await DeleteUser(user.Id);
    }
    
    /// <summary>
    /// Gets a service scope for accessing services from the DI container.
    /// This is useful for integration tests that need to directly access services.
    /// </summary>
    protected IServiceScope GetServiceScope()
    {
        return _app.Services.CreateScope();
    }
}