using Fig.Web.Models.Api;

namespace Fig.Web.Facades;

public interface IApiStatusFacade
{
    List<ApiStatusModel> ApiStatusModels { get; }
    
    Task Refresh();
}