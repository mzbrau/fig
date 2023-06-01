using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

public interface IWebHookClientTestingService
{
    Task<WebHookClientTestResultsDataContract> PerformTest(WebHookClientBusinessEntity client);
}