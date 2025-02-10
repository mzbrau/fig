using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IConfigurationRepository
{
    Task<FigConfigurationBusinessEntity> GetConfiguration(bool upgradeLock = false);

    Task UpdateConfiguration(FigConfigurationBusinessEntity configuration);
}