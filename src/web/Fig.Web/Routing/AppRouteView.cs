using System.Net;
using Fig.Contracts.Authentication;
using Fig.Web.Attributes;
using Fig.Web.Facades;
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

    [Inject]
    public IConfigurationFacade? ConfigurationFacade { get; set; }

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
        var isDashboardOnlyPage = Attribute.GetCustomAttribute(RouteData.PageType, typeof(DashboardOnlyAttribute)) != null;
        
        // Check if user authentication is required
        if (authorize && AccountService?.AuthenticatedUser == null && NavigationManager != null)
        {
            var returnUrl = WebUtility.UrlEncode(new Uri(NavigationManager.Uri).PathAndQuery);
            // Route through account/login so Keycloak mode can apply post-logout prompt handling.
            NavigationManager.NavigateTo($"account/login?returnUrl={returnUrl}");
            return;
        }
        
        // Check for password change requirement (except on manage page)
        if (!isManagePage && AccountService?.AuthenticatedUser?.PasswordChangeRequired == true && NavigationManager != null)
        {
            NavigationManager.NavigateTo("/account/manage");
            return;
        }
        
        // Check administrator role requirement
        if (isAdministratorPage && AccountService?.AuthenticatedUser?.Role != Role.Administrator)
        {
            // Redirect to unauthorized or home page
            NavigationManager?.NavigateTo("/");
            return;
        }

        var dashboardsDisabled = ConfigurationFacade?.WebFeaturesLoaded == true &&
                                 !ConfigurationFacade.AllowDisplayScripts;
        var isDashboardPath = NavigationManager is not null && IsDashboardPath(NavigationManager.Uri);

        if (dashboardsDisabled && isDashboardPath && NavigationManager is not null)
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        // Dashboard role may only visit dashboards-related pages (and account/login/manage).
        // When dashboards are disabled, they may stay on home (/) for the disabled message.
        if (AccountService?.AuthenticatedUser?.Role == Role.Dashboard &&
            NavigationManager is not null &&
            !isDashboardOnlyPage &&
            !IsDashboardAllowedPath(NavigationManager.Uri, dashboardsEnabled: !dashboardsDisabled))
        {
            NavigationManager.NavigateTo(dashboardsDisabled ? "/" : "/dashboards");
            return;
        }
        
        // Render the page if all checks pass
        base.Render(builder);
    }

    private static bool IsDashboardPath(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var absolute))
            return false;

        var path = absolute.AbsolutePath.TrimEnd('/');
        return path.StartsWith("/dashboards", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDashboardAllowedPath(string uri, bool dashboardsEnabled)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var absolute))
            return false;

        var path = absolute.AbsolutePath.TrimEnd('/');
        if (string.IsNullOrEmpty(path))
            path = "/";

        if (path.StartsWith("/account", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!dashboardsEnabled)
            return path is "/" or "";

        // Index — Dashboard role should land on /dashboards when enabled
        if (path is "/" or "")
            return false;

        if (path.StartsWith("/dashboards", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}