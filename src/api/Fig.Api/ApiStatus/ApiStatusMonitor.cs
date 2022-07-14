using System.Reflection;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Common.Diag;
using Fig.Common.IpAddress;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Options;

namespace Fig.Api.ApiStatus;

public class ApiStatusMonitor : BackgroundService
{
    private const int CheckTimeSeconds = 30;
    private const string UnknownVersion = "Unknown";
    private readonly ApiSettings _apiSettings;
    private readonly IApiStatusRepository _apiStatusRepository;
    private readonly IDiagnostics _diagnostics;
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly ILogger<ApiStatusMonitor> _logger;
    private readonly Guid _runtimeId = Guid.NewGuid();
    private readonly DateTime _startTimeUtc = DateTime.UtcNow;
    private readonly ITimer _timer;

    public ApiStatusMonitor(ITimerFactory timerFactory,
        IApiStatusRepository apiStatusRepository,
        IIpAddressResolver ipAddressResolver,
        IDiagnostics diagnostics,
        IDiagnosticsService diagnosticsService,
        IOptions<ApiSettings> apiSettings,
        ILogger<ApiStatusMonitor> logger)
    {
        _apiStatusRepository = apiStatusRepository;
        _ipAddressResolver = ipAddressResolver;
        _diagnostics = diagnostics;
        _diagnosticsService = diagnosticsService;
        _apiSettings = apiSettings.Value;
        _logger = logger;
        _timer = timerFactory.Create(TimeSpan.FromSeconds(CheckTimeSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        UpdateStatus();

        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            UpdateStatus();
    }

    private void UpdateStatus()
    {
        try
        {
            var allActive = _apiStatusRepository.GetAllActive();
            InactivateOfflineApis(allActive);
            var wasValid = ValidateSecrets(allActive);
            UpdateCurrentApiStatus(allActive, wasValid);
            
        }
        catch (Exception e)
        {
            _logger.LogError($"Error updating api status: {e.Message}");
        }
    }

    private bool ValidateSecrets(IList<ApiStatusBusinessEntity> apis)
    {
        foreach (var api in apis.Where(a => !string.IsNullOrEmpty(a.SecretHash)))
        {
            var isValid = BCrypt.Net.BCrypt.EnhancedVerify(_apiSettings.Secret, api.SecretHash);
            if (!isValid)
            {
                _logger.LogWarning($"API on host {api.Hostname} has a different client secret from this API. " +
                                   "All server secrets should be the same.");
                return false;
            }
        }

        return true;
    }

    private void InactivateOfflineApis(IList<ApiStatusBusinessEntity> apis)
    {
        foreach (var instance in apis.Where(IsNotActive))
        {
            instance.IsActive = false;
            _apiStatusRepository.AddOrUpdate(instance);
        }
    }

    private void UpdateCurrentApiStatus(IList<ApiStatusBusinessEntity> apis, bool wasSecretValid)
    {
        var thisApi = apis.FirstOrDefault(a => a.RuntimeId == _runtimeId) ?? CreateApiStatus();
        thisApi.LastSeen = DateTime.UtcNow;
        thisApi.MemoryUsageBytes = _diagnostics.GetMemoryUsageBytes();
        thisApi.TotalRequests = _diagnosticsService.TotalRequests;
        thisApi.RequestsPerMinute = _diagnosticsService.RequestsPerMinute;
        thisApi.ConfigurationErrorDetected = !wasSecretValid;
        _apiStatusRepository.AddOrUpdate(thisApi);
    }

    private bool IsNotActive(ApiStatusBusinessEntity apiStatus)
    {
        return apiStatus.LastSeen < DateTime.UtcNow - TimeSpan.FromSeconds(3 * CheckTimeSeconds);
    }

    private ApiStatusBusinessEntity CreateApiStatus()
    {
        return new ApiStatusBusinessEntity
        {
            RuntimeId = _runtimeId,
            IpAddress = _ipAddressResolver.Resolve(),
            Hostname = Environment.MachineName,
            Version = GetVersion(),
            IsActive = true,
            StartTimeUtc = _startTimeUtc,
            RunningUser = _diagnostics.GetRunningUser(),
            SecretHash = BCrypt.Net.BCrypt.EnhancedHashPassword(_apiSettings.Secret)
        };
    }

    private string GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly();

        if (assembly == null)
            return UnknownVersion;

        var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        return version ?? UnknownVersion;
    }
}