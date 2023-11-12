using Fig.Common.Timer;
using Microsoft.Extensions.Options;

namespace Fig.Integration.SqlLookupTableService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IOptionsMonitor<Settings> _settings;
    private readonly ISqlQueryManager _sqlQueryManager;
    private readonly IFigFacade _figFacade;
    private readonly IPeriodicTimer _timer;

    public Worker(ILogger<Worker> logger,
        ITimerFactory timerFactory,
        IOptionsMonitor<Settings> settings,
        ISqlQueryManager sqlQueryManager,
        IFigFacade figFacade)
    {
        _logger = logger;
        _settings = settings;
        _sqlQueryManager = sqlQueryManager;
        _figFacade = figFacade;
        _timer = timerFactory.Create(TimeSpan.FromSeconds(_settings.CurrentValue.RefreshIntervalSeconds));
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EvaluateLookupTables();
        
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            await EvaluateLookupTables();
    }

    private async Task EvaluateLookupTables()
    {
        _settings.CurrentValue.Validate(_logger);
        if (_settings.CurrentValue.HasConfigurationError || _settings.CurrentValue.Configuration is null)
            return;
        
        _logger.LogInformation("Evaluating {Count} configured lookup tables", _settings.CurrentValue.Configuration!.Count);
        await _figFacade.Login();
        await _figFacade.GetExistingLookups();
        foreach (var lookup in _settings.CurrentValue.Configuration!.Where(a => a.SqlExpression is not null))
        {
            _logger.LogInformation("Evaluating \'{LookupName}\'", lookup.Name);
            try
            {
                var result = await _sqlQueryManager.ExecuteQuery(lookup.SqlExpression!);
                await _figFacade.UpdateLookup(lookup, result);
                _logger.LogInformation("\'{LookupName}\' completed successfully", lookup.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while evaluating {LookupName}", lookup.Name);
            }
        }
    }
}