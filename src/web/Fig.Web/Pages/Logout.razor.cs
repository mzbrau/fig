namespace Fig.Web.Pages;

public partial class Logout
{
    protected override async Task OnInitializedAsync()
    {
        await AccountService.Logout();
    }
}