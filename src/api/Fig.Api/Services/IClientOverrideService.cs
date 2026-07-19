using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

public interface IClientOverrideService
{
    Task<SettingClientBusinessEntity> CreateClientOverride(
        string clientName,
        string instance,
        UserDataContract? authenticatedUser);
}

