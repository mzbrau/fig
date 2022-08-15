using Fig.Common;
using Fig.Web.ExtensionMethods;
using Fig.Web.Models.Authentication;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages;

public partial class Login
{
    private bool _loading;
    private readonly LoginModel _loginModel = new();
    private string _webVersion = "Unknown";

    [Inject] 
    private IAccountService AccountService { get; set; } = null!;

    [Inject] 
    private IVersionHelper VersionHelper { get; set; } = null!;
    
    [Inject] 
    private NavigationManager NavigationManager { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _webVersion = $"v{VersionHelper.GetVersion()}";
        await base.OnInitializedAsync();
    }
    
    private async Task OnLogin()
    {
        if (!_loginModel.IsValid())
            return;
        
        _loading = true;
        try
        {
            await AccountService.Login(_loginModel);
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