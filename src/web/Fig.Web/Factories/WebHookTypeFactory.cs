using System.Text.RegularExpressions;
using Fig.Common.NetStandard.WebHook;
using Fig.Web.Models.WebHooks;

namespace Fig.Web.Factories;

public class WebHookTypeFactory : IWebHookTypeFactory
{
    private readonly Regex _camelCaseAddSpaces = new Regex("(\\B[A-Z])", RegexOptions.Compiled);
    
    public IEnumerable<WebHookTypeEnumerable> GetWebHookTypes()
    {
        
        foreach (var item in Enum.GetValues(typeof(WebHookType)))
        {
            yield return new WebHookTypeEnumerable
            {
                EnumName = GetFriendlyString((WebHookType)item), 
                EnumValue = (WebHookType)item
            };
        }
    }

    private string GetFriendlyString(WebHookType webHookType)
    {
        return _camelCaseAddSpaces.Replace(webHookType.ToString(), " $1");
    }
}