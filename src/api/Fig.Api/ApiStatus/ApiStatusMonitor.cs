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
    private const int CheckTimeSeconds = 5; // TODO: Configurable for integration tests...
    private readonly IApiStatusRepository _apiStatusRepository;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly Guid _runtimeId = Guid.NewGuid();
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly DateTime _startTimeUtc = DateTime.UtcNow;
    private readonly ITimer _timer;

    public ApiStatusMonitor(ITimerFactory timerFactory,
        IApiStatusRepository apiStatusRepository,
        IIpAddressResolver ipAddressResolver,
        IServiceScopeFactory serviceScopeFactory)
    {
        _apiStatusRepository = apiStatusRepository;
        _ipAddressResolver = ipAddressResolver;
        _serviceScopeFactory = serviceScopeFactory;
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

        using var scope = _serviceScopeFactory.CreateScope();
        var encryptionService = scope.ServiceProvider.GetService<IEncryptionService>();
        if (encryptionService is null)
            throw new InvalidOperationException("Unable to find Encryption service");

        InactivateOfflineApis(allActive);
        UpdateCurrentApiStatus(allActive, encryptionService);
        MigrateIfRequired(allActive, encryptionService, scope);
    }

    private void InactivateOfflineApis(IList<ApiStatusBusinessEntity> apis)
    {
        foreach (var instance in apis.Where(IsNotActive))
        {
            instance.IsActive = false;
            _apiStatusRepository.AddOrUpdate(instance);
        }
    }

    private void UpdateCurrentApiStatus(IList<ApiStatusBusinessEntity> apis, IEncryptionService encryptionService)
    {
        var thisApi = apis.FirstOrDefault(a => a.RuntimeId == _runtimeId) ?? CreateApiStatus();
        var certificateThumbprints = encryptionService.GetAllCertificatesInStore().Select(a => a.Thumbprint).ToList();
        thisApi.Update(certificateThumbprints, _startTimeUtc);
        _apiStatusRepository.AddOrUpdate(thisApi);
    }

    private void MigrateIfRequired(IList<ApiStatusBusinessEntity> apis, IEncryptionService encryptionService,
        IServiceScope serviceScope)
    {
        var certificateStatus = encryptionService.GetCertificateStatus();
        if (certificateStatus.RequiresMigration && apis.Where(a => a.IsActive)
                .All(a => a.CertificatesInStore.Contains(certificateStatus.NewestThumbprint)))
            MigrateToCertificate(certificateStatus.NewestThumbprint, serviceScope);
    }

    private void MigrateToCertificate(string thumbprint, IServiceScope scope)
    {
        var settingsService = scope.ServiceProvider.GetService<ISettingsService>();
        if (settingsService is null)
            throw new InvalidOperationException("Unable to find Settings service");

        settingsService.MigrateToCertificate(thumbprint);
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