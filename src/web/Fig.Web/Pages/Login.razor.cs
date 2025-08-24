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
    private string? _errorMessage;

    [Inject] 
    private IAccountService AccountService { get; set; } = null!;

    [Inject] 
    private IVersionHelper VersionHelper { get; set; } = null!;
    
    [Inject] 
    private NavigationManager NavigationManager { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _webVersion = $"v{VersionHelper.GetVersion()}";
        
        // If user is already authenticated and account service is initialized, redirect to the return URL or home
        if (AccountService.IsInitialized && AccountService.AuthenticatedUser != null)
        {
            var returnUrl = NavigationManager.QueryString("returnUrl") ?? "";
            NavigationManager.NavigateTo(returnUrl);
            return;
        }
        
        await base.OnInitializedAsync();
    }
    
    private async Task OnLogin()
    {
        if (!_loginModel.IsValid())
            return;
        
        _loading = true;
        _errorMessage = null; // Clear any previous error message
        StateHasChanged();
        
        try
        {
            await AccountService.Login(_loginModel);
            var returnUrl = NavigationManager.QueryString("returnUrl") ?? "";
            NavigationManager.NavigateTo(returnUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while trying to log in {ex}");
            _errorMessage = "Invalid username or password. Please try again.";
            _loading = false;
            StateHasChanged();
        }
    }
}