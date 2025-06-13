using Fig.Datalayer.BusinessEntities;
using Fig.WebHooks.Contracts;

namespace Fig.Api.WebHooks;

public record MinRunSessionsWebHookData(
    ClientStatusBusinessEntity Client,
    RunSessionsEvent RunSessionsEvent,
    int? SessionCount = null); // Optional override for the session count
