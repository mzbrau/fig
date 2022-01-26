using Fig.Contracts.Authentication;
using Fig.Web.ExtensionMethods;

namespace Fig.Web.Pages;

public partial class Login
{
    private readonly AuthenticateRequestDataContract _loginRequest = new();
    private bool _loading;

    private async void OnValidSubmit()
    {
        // reset alerts on submit
        //AlertService.Clear();

        _loading = true;
        try
        {
            await AccountService.Login(_loginRequest);
            var returnUrl = NavigationManager.QueryString("returnUrl") ?? "";
            NavigationManager.NavigateTo(returnUrl);
        }
        catch (Exception ex)
        {
            //AlertService.Error(ex.Message);
            _loading = false;
            StateHasChanged();
        }
    }
}