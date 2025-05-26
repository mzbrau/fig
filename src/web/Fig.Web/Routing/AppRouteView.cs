using System.Net;
using Fig.Contracts.Authentication;
using System.Net;
using Fig.Contracts.Authentication;
using Fig.Web.Attributes;
using Fig.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Options;

namespace Fig.Web.Routing;

public class AppRouteView : RouteView
{
    [Inject]
    public NavigationManager? NavigationManager { get; set; }

    [Inject]
    public IAccountService? AccountService { get; set; }
    
    [Inject]
    public IOptions<WebSettings>? WebSettings { get; set; }

    protected override void Render(RenderTreeBuilder builder)
    {
        var isKeycloakEnabled = WebSettings?.Value.UseKeycloak == true;

        if (isKeycloakEnabled)
        {
            // When Keycloak is enabled, AuthorizeRouteView in App.razor handles auth.
            // Fig-specific attribute checks might still be needed if they represent
            // application-level permissions beyond simple authentication/role.
            // However, the primary redirection for login is handled by OIDC components.

            // AdministratorAttribute check might still be relevant, but should use
            // claims from AuthenticationStateProvider if needed here, or be handled by AuthorizeView roles.
            // For now, letting AuthorizeRouteView in App.razor handle this.
            base.Render(builder);
            return;
        }

        // Original Fig authentication logic
        var authorize = Attribute.GetCustomAttribute(RouteData.PageType, typeof(AuthorizeAttribute)) != null;
        if (authorize && AccountService?.AuthenticatedUser == null && NavigationManager != null)
        {
            var returnUrl = WebUtility.UrlEncode(new Uri(NavigationManager.Uri).PathAndQuery);
            NavigationManager.NavigateTo($"account/login?returnUrl={returnUrl}");
        }
        else if (Attribute.GetCustomAttribute(RouteData.PageType, typeof(ManageAttribute)) == null && 
                 AccountService?.AuthenticatedUser?.PasswordChangeRequired == true && NavigationManager != null)
        {
            NavigationManager.NavigateTo("account/Manage");
        }
        else if (Attribute.GetCustomAttribute(RouteData.PageType, typeof(AdministratorAttribute)) != null &&
                 AccountService?.AuthenticatedUser?.Role != Role.Administrator)
        {
            // TODO: Consider redirecting to an "Access Denied" page or showing a message.
            // For now, rendering nothing for unauthorized access to admin pages.
            // Or, rely on page-level checks if this component shouldn't block rendering.
            // NavigationManager.NavigateTo("access-denied"); // Example
        }
        else
        {
            base.Render(builder);
        }
    }
}