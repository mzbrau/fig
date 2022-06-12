using System.Reflection;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Common.IpAddress;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ApiStatus;

public class ApiStatusMonitor : IApiStatusMonitor
{
    private const int CheckTimeSeconds = 60;
    private readonly DateTime _startTimeUtc = DateTime.UtcNow;
    private readonly Guid _runtimeId = Guid.NewGuid();
    private readonly IEncryptionService _encryptionService;
    private readonly IApiStatusRepository _apiStatusRepository;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly ITimer _timer;

    public ApiStatusMonitor(ITimerFactory timerFactory,
        IEncryptionService encryptionService,
        IApiStatusRepository apiStatusRepository,
        IIpAddressResolver ipAddressResolver)
    {
        _encryptionService = encryptionService;
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
        MigrateIfRequired(allActive);
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
        thisApi.Update(_encryptionService.GetAllThumbprintsInStore(), _startTimeUtc);
        _apiStatusRepository.AddOrUpdate(thisApi);
    }

    private void MigrateIfRequired(IList<ApiStatusBusinessEntity> apis)
    {
        var certificateStatus = _encryptionService.GetCertificateStatus();
        if (certificateStatus.RequiresMigration && apis.Where(a => a.IsActive).All(a => a.CertificatesInStore.Contains(certificateStatus.NewestThumbprint)))
        {
            _encryptionService.MigrateToNewCertificate();
        }
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
            IsActive = true
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