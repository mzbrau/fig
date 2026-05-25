using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class SettingChangeRepository : RepositoryBase<SettingChangeBusinessEntity>, ISettingChangeRepository
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private const long SlowQueryWarningMs = 1000;
    private readonly ILogger<SettingChangeRepository> _logger;

    public SettingChangeRepository(ISession session, ILogger<SettingChangeRepository> logger) 
        : base(session)
    {
        _logger = logger;
    }
    
    public async Task<SettingChangeBusinessEntity?> GetLastChange()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var watch = Stopwatch.StartNew();
        try
        {
            var result = await Session.QueryOver<SettingChangeBusinessEntity>()
                .OrderBy(x => x.LastChange).Desc
                .Take(1)
                .SingleOrDefaultAsync();
            if (watch.ElapsedMilliseconds >= SlowQueryWarningMs)
            {
                _logger.LogWarning("Slow last setting change query completed in {ElapsedMs} ms", watch.ElapsedMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to query last setting change after {ElapsedMs} ms. LockContentionDetected={LockContentionDetected}",
                watch.ElapsedMilliseconds,
                ex.IsLockContention());
            throw;
        }
    }

    public async Task RegisterChange()
    {
        try
        {
            if (!await Semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
                throw new TimeoutException("Failed to acquire lock for registering setting change");

            var item = await GetLastChangeForThisServer();
            if (item is null)
            {
                item = new SettingChangeBusinessEntity
                {
                    LastChange = DateTime.UtcNow,
                    ServerName = Environment.MachineName
                };
                await Save(item);
            }
            else
            {
                item.LastChange = DateTime.UtcNow;
                await Update(item);
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private async Task<SettingChangeBusinessEntity?> GetLastChangeForThisServer()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        return await Session.QueryOver<SettingChangeBusinessEntity>()
            .Where(x => x.ServerName == Environment.MachineName)
            .OrderBy(x => x.LastChange).Desc
            .Take(1)
            .SingleOrDefaultAsync();
    }
}