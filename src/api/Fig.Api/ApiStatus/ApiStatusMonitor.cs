using System.Reflection;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Common.IpAddress;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ApiStatus;

public class ApiStatusMonitor : IApiStatusMonitor
{
    private const int CheckTimeSeconds = 30;
    private readonly IApiStatusRepository _apiStatusRepository;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly Guid _runtimeId = Guid.NewGuid();
    private readonly DateTime _startTimeUtc = DateTime.UtcNow;
    private readonly ITimer _timer;

    public ApiStatusMonitor(ITimerFactory timerFactory,
        IApiStatusRepository apiStatusRepository,
        IIpAddressResolver ipAddressResolver)
    {
        _apiStatusRepository = apiStatusRepository;
        _ipAddressResolver = ipAddressResolver;
        _timer = timerFactory.Create(TimeSpan.FromSeconds(CheckTimeSeconds));
        _timer.Elapsed += OnTimerElapsed;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    private void OnTimerElapsed(object? sender, EventArgs e)
    {
        var allActive = _apiStatusRepository.GetAllActive();
        InactivateOfflineApis(allActive);
        UpdateCurrentApiStatus(allActive);
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
        _apiStatusRepository.AddOrUpdate(thisApi);
    }

    private bool IsNotActive(ApiStatusBusinessEntity apiStatus)
    {
        return apiStatus.LastSeen < DateTime.UtcNow - TimeSpan.FromSeconds(5 * CheckTimeSeconds);
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
            UptimeSeconds = (DateTime.UtcNow - _startTimeUtc).TotalSeconds,
            LastSeen = DateTime.UtcNow
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