using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class SettingChangeRepository : RepositoryBase<SettingChangeBusinessEntity>, ISettingChangeRepository
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public SettingChangeRepository(ISession session) 
        : base(session)
    {
    }
    
    public async Task<SettingChangeBusinessEntity?> GetLastChange()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        return await Session.QueryOver<SettingChangeBusinessEntity>()
            .OrderBy(x => x.LastChange).Desc
            .Take(1)
            .SingleOrDefaultAsync();
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