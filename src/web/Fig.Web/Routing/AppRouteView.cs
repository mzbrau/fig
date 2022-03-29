using System.Net;
using Fig.Contracts.Authentication;
using Fig.Web.Attributes;
using Fig.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Fig.Web.Routing;

public class AppRouteView : RouteView
{
    [Inject]
    public NavigationManager? NavigationManager { get; set; }

    [Inject]
    public IAccountService? AccountService { get; set; }

    protected override void Render(RenderTreeBuilder builder)
    {
        var authorize = Attribute.GetCustomAttribute(RouteData.PageType, typeof(AuthorizeAttribute)) != null;
        if (authorize && AccountService?.AuthenticatedUser == null && NavigationManager != null)
        {
            var returnUrl = WebUtility.UrlEncode(new Uri(NavigationManager.Uri).PathAndQuery);
            NavigationManager.NavigateTo($"account/login?returnUrl={returnUrl}");
        }
        else if (Attribute.GetCustomAttribute(RouteData.PageType, typeof(AdministratorAttribute)) != null &&
                 AccountService?.AuthenticatedUser?.Role != Role.Administrator)
        {
            // Don't do anything (maybe page not found is better?)
            // TODO: Page not found
        }
        else
        {
            base.Render(builder);
        }
    }
}