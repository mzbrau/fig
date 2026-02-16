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
        // Wait for authentication to be initialized before proceeding
        if (!AccountService?.IsInitialized == true)
        {
            // Don't render anything while authentication is being validated
            return;
        }

        var authorize = Attribute.GetCustomAttribute(RouteData.PageType, typeof(AuthorizeAttribute)) != null;
        var isManagePage = Attribute.GetCustomAttribute(RouteData.PageType, typeof(ManageAttribute)) != null;
        var isAdministratorPage = Attribute.GetCustomAttribute(RouteData.PageType, typeof(AdministratorAttribute)) != null;
        
        // Check if user authentication is required
        if (authorize && AccountService?.AuthenticatedUser == null && NavigationManager != null)
        {
            var returnUrl = WebUtility.UrlEncode(new Uri(NavigationManager.Uri).PathAndQuery);
            if (AccountService?.AuthenticationMode == WebAuthMode.Keycloak)
                NavigationManager.NavigateTo($"authentication/login?returnUrl={returnUrl}");
            else
                NavigationManager.NavigateTo($"account/login?returnUrl={returnUrl}");

            return;
        }
        
        // Check for password change requirement (except on manage page)
        if (!isManagePage && AccountService?.AuthenticatedUser?.PasswordChangeRequired == true && NavigationManager != null)
        {
            NavigationManager.NavigateTo("account/Manage");
            return;
        }
        
        // Check administrator role requirement
        if (isAdministratorPage && AccountService?.AuthenticatedUser?.Role != Role.Administrator)
        {
            // Redirect to unauthorized or home page
            NavigationManager?.NavigateTo("/");
            return;
        }
        
        // Render the page if all checks pass
        base.Render(builder);
    }
}