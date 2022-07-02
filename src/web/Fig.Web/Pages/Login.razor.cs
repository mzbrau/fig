using Fig.Contracts.Authentication;
using Fig.Web.ExtensionMethods;

namespace Fig.Web.Pages;

public partial class Login
{
    private readonly AuthenticateRequestDataContract _loginRequest = new();
    private bool _loading;

    private async void OnValidSubmit()
    {
        _loading = true;
        try
        {
            await AccountService.Login(_loginRequest);
            var returnUrl = NavigationManager.QueryString("returnUrl") ?? "";
            NavigationManager.NavigateTo(returnUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while trying to log in {ex}");
            _loading = false;
            StateHasChanged();
        }
    }
}