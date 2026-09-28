using Fig.Web.ExtensionMethods;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages;

public partial class Logout
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var sessionExpiredValue = NavigationManager.QueryString("sessionExpired");
        var showSessionExpiredNotice =
            string.Equals(sessionExpiredValue, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sessionExpiredValue, "true", StringComparison.OrdinalIgnoreCase);

        await AccountService.Logout(showSessionExpiredNotice);
    }
}
