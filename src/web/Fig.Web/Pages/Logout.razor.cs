namespace Fig.Web.Pages;

public partial class Logout
{
    protected override async void OnInitialized()
    {
        await AccountService.Logout();
    }
}