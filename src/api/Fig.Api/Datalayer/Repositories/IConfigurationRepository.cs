using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IConfigurationRepository
{
    FigConfigurationBusinessEntity GetConfiguration(bool upgradeLock = false);

    void UpdateConfiguration(FigConfigurationBusinessEntity configuration);
}