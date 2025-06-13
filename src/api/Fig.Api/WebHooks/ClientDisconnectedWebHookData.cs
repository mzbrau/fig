using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.WebHooks;

public record ClientDisconnectedWebHookData(
    ClientRunSessionBusinessEntity Session,
    ClientStatusBusinessEntity Client);
