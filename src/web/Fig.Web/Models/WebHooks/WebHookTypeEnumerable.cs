using Fig.Contracts.WebHook;

namespace Fig.Web.Models.WebHooks;

public class WebHookTypeEnumerable
{
    public WebHookTypeEnumerable(WebHookType enumValue, string enumName)
    {
        EnumValue = enumValue;
        EnumName = enumName;
    }

    public WebHookType EnumValue { get; }
        
    public string EnumName { get; }
}