using Fig.Datalayer.BusinessEntities;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class ConfigurationRepository : RepositoryBase<FigConfigurationBusinessEntity>, IConfigurationRepository
{
    public ConfigurationRepository(ISession session) 
        : base(session)
    {
    }

    public async Task<FigConfigurationBusinessEntity> GetConfiguration(bool upgradeLock = false)
    {
        var configuration = (await GetAll(upgradeLock)).FirstOrDefault();

        return configuration ?? await CreateNew();
    }

    public async Task UpdateConfiguration(FigConfigurationBusinessEntity configuration)
    {
        await Update(configuration);
    }

    private async Task<FigConfigurationBusinessEntity> CreateNew()
    {
        var configuration = new FigConfigurationBusinessEntity();
        await Save(configuration);
        return configuration;
    }
}