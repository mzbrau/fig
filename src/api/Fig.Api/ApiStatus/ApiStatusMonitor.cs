using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Api.SettingVerification;
using Fig.Common;
using Fig.Common.NetStandard.Diag;
using Fig.Common.NetStandard.IpAddress;
using Fig.Common.Timer;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Options;

namespace Fig.Api.ApiStatus;

public class ApiStatusMonitor : BackgroundService
{
    private const int CheckTimeSeconds = 30;
    private readonly IOptions<ApiSettings> _apiSettings;
    private readonly IDiagnostics _diagnostics;
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly ILogger<ApiStatusMonitor> _logger;
    private readonly IVerificationFactory _verificationFactory;
    private readonly IVersionHelper _versionHelper;
    private readonly Guid _runtimeId = Guid.NewGuid();
    private readonly DateTime _startTimeUtc = DateTime.UtcNow;
    private readonly IPeriodicTimer _timer;

    public ApiStatusMonitor(ITimerFactory timerFactory,
        IServiceProvider serviceProvider,
        IIpAddressResolver ipAddressResolver,
        IDiagnostics diagnostics,
        IDiagnosticsService diagnosticsService,
        IOptions<ApiSettings> apiSettings,
        ILogger<ApiStatusMonitor> logger,
        IVerificationFactory verificationFactory,
        IVersionHelper versionHelper)
    {
        _serviceProvider = serviceProvider;
        _ipAddressResolver = ipAddressResolver;
        _diagnostics = diagnostics;
        _diagnosticsService = diagnosticsService;
        _apiSettings = apiSettings;
        _logger = logger;
        _verificationFactory = verificationFactory;
        _versionHelper = versionHelper;
        _timer = timerFactory.Create(TimeSpan.FromSeconds(CheckTimeSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await UpdateStatus();

        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            await UpdateStatus();
    }

    private async Task UpdateStatus()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var apiStatusRepository = scope.ServiceProvider.GetRequiredService<IApiStatusRepository>();
            var allActive = await apiStatusRepository.GetAllActive();
            InactivateOfflineApis(allActive, apiStatusRepository);
            var wasValid = ValidateSecrets(allActive);
            UpdateCurrentApiStatus(allActive, wasValid, apiStatusRepository);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating api status");
        }
    }

    private bool ValidateSecrets(IList<ApiStatusBusinessEntity> apis)
    {
        foreach (var api in apis.Where(a => !string.IsNullOrEmpty(a.SecretHash)))
        {
            var apiSettings = _apiSettings;
            var isValid = BCrypt.Net.BCrypt.EnhancedVerify(apiSettings.Value.GetDecryptedSecret(), api.SecretHash);
            if (!isValid)
            {
                _logger.LogWarning("API on host {Hostname} has a different client secret from this API. " +
                                   "All server secrets should be the same", api.Hostname);
                return false;
            }
        }

        return true;
    }

    private void InactivateOfflineApis(IList<ApiStatusBusinessEntity> apis, IApiStatusRepository apiStatusRepository)
    {
        foreach (var instance in apis.Where(IsNotActive))
        {
            instance.IsActive = false;
            apiStatusRepository.AddOrUpdate(instance);
        }
    }

    private void UpdateCurrentApiStatus(IList<ApiStatusBusinessEntity> apis, bool wasSecretValid, IApiStatusRepository apiStatusRepository)
    {
        var thisApi = apis.FirstOrDefault(a => a.RuntimeId == _runtimeId) ?? CreateApiStatus();
        thisApi.LastSeen = DateTime.UtcNow;
        thisApi.MemoryUsageBytes = _diagnostics.GetMemoryUsageBytes();
        thisApi.TotalRequests = _diagnosticsService.TotalRequests;
        thisApi.RequestsPerMinute = _diagnosticsService.RequestsPerMinute;
        thisApi.ConfigurationErrorDetected = !wasSecretValid;
        apiStatusRepository.AddOrUpdate(thisApi);
    }

    private bool IsNotActive(ApiStatusBusinessEntity apiStatus)
    {
        return apiStatus.LastSeen < DateTime.UtcNow - TimeSpan.FromSeconds(3 * CheckTimeSeconds);
    }

    private ApiStatusBusinessEntity CreateApiStatus()
    {
        var verifiers = _verificationFactory.GetAvailableVerifiers().ToList();

        return new ApiStatusBusinessEntity
        {
            RuntimeId = _runtimeId,
            IpAddress = _ipAddressResolver.Resolve(),
            Hostname = Environment.MachineName,
            Version = _versionHelper.GetVersion(),
            IsActive = true,
            StartTimeUtc = _startTimeUtc,
            RunningUser = _diagnostics.GetRunningUser(),
            SecretHash = BCrypt.Net.BCrypt.EnhancedHashPassword(_apiSettings.Value.GetDecryptedSecret()),
            NumberOfVerifiers = verifiers.Count,
            Verifiers = string.Join(", ", verifiers)
        };
    }
}