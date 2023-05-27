using Fig.Web.Models.WebHooks;

namespace Fig.Web.Factories;

public interface IWebHookTypeFactory
{
    IEnumerable<WebHookTypeEnumerable> GetWebHookTypes();
}