using System.Data;
using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class SettingChangeRepository : RepositoryBase<SettingChangeBusinessEntity>, ISettingChangeRepository
{
    private static readonly object _lock = new object();

    public SettingChangeRepository(ISession session) 
        : base(session)
    {
    }
    
    public async Task<SettingChangeBusinessEntity?> GetLastChange()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var result = await Session.QueryOver<SettingChangeBusinessEntity>()
            .Lock().Upgrade
            .SingleOrDefaultAsync();
        return result;
    }

    public async Task RegisterChange()
    {
        using var transaction = Session.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var item = await GetLastChange();
            if (item is null)
            {
                item = new SettingChangeBusinessEntity
                {
                    LastChange = DateTime.UtcNow
                };
                await Save(item);
            }
            else
            {
                item.LastChange = DateTime.UtcNow;
                await Update(item);
            }

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}