using System.Reflection;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Common.Diag;
using Fig.Common.IpAddress;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ApiStatus;

public class ApiStatusMonitor : BackgroundService
{
    private const int CheckTimeSeconds = 30;
    private readonly IApiStatusRepository _apiStatusRepository;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly IDiagnostics _diagnostics;
    private readonly ILogger<ApiStatusMonitor> _logger;
    private readonly Guid _runtimeId = Guid.NewGuid();
    private readonly DateTime _startTimeUtc = DateTime.UtcNow;
    private readonly ITimer _timer;

    public ApiStatusMonitor(ITimerFactory timerFactory,
        IApiStatusRepository apiStatusRepository,
        IIpAddressResolver ipAddressResolver,
        IDiagnostics diagnostics,
        ILogger<ApiStatusMonitor> logger)
    {
        _apiStatusRepository = apiStatusRepository;
        _ipAddressResolver = ipAddressResolver;
        _diagnostics = diagnostics;
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
            UpdateCurrentApiStatus(allActive);
        }
        catch (Exception e)
        {
            _logger.LogError($"Error updating api status: {e.Message}");
        }
    }

    private void InactivateOfflineApis(IList<ApiStatusBusinessEntity> apis)
    {
        foreach (var instance in apis.Where(IsNotActive))
        {
            instance.IsActive = false;
            _apiStatusRepository.AddOrUpdate(instance);
        }
    }

    private void UpdateCurrentApiStatus(IList<ApiStatusBusinessEntity> apis)
    {
        var thisApi = apis.FirstOrDefault(a => a.RuntimeId == _runtimeId) ?? CreateApiStatus();
        thisApi.LastSeen = DateTime.UtcNow;
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
            LastSeen = DateTime.UtcNow,
            MemoryUsageBytes = _diagnostics.GetMemoryUsageBytes()
        };
    }

    private string GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly();

        if (assembly == null)
            return "Unknown";

        var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        return version;
    }
}