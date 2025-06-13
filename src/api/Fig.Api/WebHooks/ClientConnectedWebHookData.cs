using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.WebHooks;

public record ClientConnectedWebHookData(
    ClientRunSessionBusinessEntity Session,
    ClientStatusBusinessEntity Client);
