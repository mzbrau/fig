using System.Collections.Specialized;
using System.Web;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.ExtensionMethods;

public static class NavigationManagerExtensionMethods
{
    public static NameValueCollection QueryString(this NavigationManager navigationManager)
    {
        return HttpUtility.ParseQueryString(new Uri(navigationManager.Uri).Query);
    }
}