using System.Text.RegularExpressions;
using Fig.Contracts.WebHook;
using Fig.Web.Models.WebHooks;

namespace Fig.Web.Factories;

public class WebHookTypeFactory : EnumFriendlyNameBase<WebHookType>, IWebHookTypeFactory
{
    public IEnumerable<WebHookTypeEnumerable> GetWebHookTypes()
    {
        foreach (var item in Enum.GetValues(typeof(WebHookType)))
        {
            yield return new WebHookTypeEnumerable((WebHookType)item, GetFriendlyString((WebHookType)item));
        }
    }
}