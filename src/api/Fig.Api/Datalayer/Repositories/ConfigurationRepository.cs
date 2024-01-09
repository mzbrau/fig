using Fig.Datalayer.BusinessEntities;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class ConfigurationRepository : RepositoryBase<FigConfigurationBusinessEntity>, IConfigurationRepository
{
    public ConfigurationRepository(ISession session) 
        : base(session)
    {
    }

    public FigConfigurationBusinessEntity GetConfiguration()
    {
        var configuration = GetAll().FirstOrDefault();

        if (configuration is not null)
        {
            return configuration;
        }

        return CreateNew();
    }

    public void UpdateConfiguration(FigConfigurationBusinessEntity configuration)
    {
        Update(configuration);
    }

    private FigConfigurationBusinessEntity CreateNew()
    {
        var configuration = new FigConfigurationBusinessEntity();
        Save(configuration);
        return configuration;
    }
}