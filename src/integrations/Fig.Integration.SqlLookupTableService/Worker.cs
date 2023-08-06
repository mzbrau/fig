using Fig.Common.Timer;

namespace Fig.Integration.SqlLookupTableService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ISettings _settings;
    private readonly ISqlQueryManager _sqlQueryManager;
    private readonly IFigFacade _figFacade;
    private readonly IPeriodicTimer _timer;

    public Worker(ILogger<Worker> logger,
        ITimerFactory timerFactory,
        ISettings settings,
        ISqlQueryManager sqlQueryManager,
        IFigFacade figFacade)
    {
        _logger = logger;
        _settings = settings;
        _sqlQueryManager = sqlQueryManager;
        _figFacade = figFacade;
        _timer = timerFactory.Create(TimeSpan.FromSeconds(_settings.RefreshIntervalSeconds));
    }

    public override void Dispose()
    {
        _timer?.Dispose();
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
        if (!_settings.AreValid(_logger) || _settings.Configuration is null)
            return;

        
        _logger.LogInformation("Evaluating {Count} configured lookup tables", _settings.Configuration!.Count);
        await _figFacade.Login();
        await _figFacade.GetExistingLookups();
        foreach (var lookup in _settings.Configuration!.Where(a => a.SqlExpression is not null))
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