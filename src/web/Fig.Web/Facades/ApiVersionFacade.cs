using Fig.Common.Timer;
using Fig.Contracts.Status;
using Fig.Web.Events;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class ApiVersionFacade : IApiVersionFacade
{
    private readonly IHttpService _httpService;
    private readonly IEventDistributor _eventDistributor;
    private readonly IPeriodicTimer _timer;
    private DateTime _lastSettingUpdateOnServer = DateTime.MinValue;
    private DateTime _lastSettingUpdateInWebApp = DateTime.MinValue;

    public ApiVersionFacade(IHttpService httpService,
        ITimerFactory timerFactory,
        IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        _eventDistributor = eventDistributor;

        _eventDistributor.Subscribe(EventConstants.SettingsLoaded, () =>
        {
            _lastSettingUpdateInWebApp = DateTime.UtcNow;
            IsConnectedChanged?.Invoke(this, EventArgs.Empty);
        });

        _timer = timerFactory.Create(TimeSpan.FromSeconds(10));

        ApiAddress = httpService.BaseAddress;
        
        Task.Run(async () => await Start());
    }

    public event EventHandler? IsConnectedChanged;
    public bool IsConnected { get; private set; }
    
    public DateTime? LastConnected { get; private set; }
    
    public string ApiAddress { get; }
    
    public string? ApiVersion { get; private set; }

    public bool AreSettingsStale => _lastSettingUpdateOnServer > _lastSettingUpdateInWebApp;

    private async Task Start()
    {
        await PingApi();
        
        var source = new CancellationTokenSource();
        while (await _timer.WaitForNextTickAsync(source.Token) && !source.Token.IsCancellationRequested)
            await PingApi();
    }

    private async Task PingApi()
    {
        try
        {
            var result = await _httpService.Get<ApiVersionDataContract>("apiversion", false);

            if (result == null)
                throw new Exception("No Connection to API");
            
            IsConnected = true;
            ApiVersion = result.ApiVersion;
            LastConnected = DateTime.UtcNow;
            _lastSettingUpdateOnServer = result.LastSettingChange;
            IsConnectedChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            IsConnected = false;
            IsConnectedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}