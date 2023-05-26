using Fig.Web.Models.WebHooks;

namespace Fig.Web.Services;

public interface IWebHookAnalysisService
{
    Task<MatchingClientsModel> PerformAnalysis(WebHookModel webHook);
}