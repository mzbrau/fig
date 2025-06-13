using Fig.Contracts.Health;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.WebHooks;

public record HealthStatusChangedWebHookData(
    ClientRunSessionBusinessEntity Session,
    ClientStatusBusinessEntity Client,
    HealthDataContract HealthDetails);
