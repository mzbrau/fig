using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Web.Models.Authentication;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Radzen;

namespace Fig.Web.Pages;

public partial class ManageAccount
{
    [Inject]
    IAccountService AccountService { get; set; } = null!;
    
    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private INotificationFactory NotificationFactory { get; set; } = null!;

    [Inject]
    private IOptions<WebSettings> WebSettings { get; set; } = null!;
    
    private bool _showPasswordRow;
    private string _password = string.Empty;
    private bool _passwordValid;

    private bool IsKeycloakMode => AccountService.AuthenticationMode == WebAuthMode.Keycloak;

    private string? AccountManagementUrl => WebSettings.Value.Authentication.Keycloak.AccountManagementUrl;
    
    private List<Classification> AllClassifications { get; } = Enum.GetValues(typeof(Classification))
        .Cast<Classification>()
        .ToList();

    protected override async Task OnInitializedAsync()
    {
        if (AccountService.AuthenticatedUser?.PasswordChangeRequired == true)
        {
            _showPasswordRow = true;
        }

        await base.OnInitializedAsync();
    }

    private void Submit(AuthenticatedUserModel user)
    {
        if (IsKeycloakMode)
        {
            NotificationService.Notify(NotificationFactory.Info("Managed by Keycloak",
                "Profile updates are managed by Keycloak in this mode."));
            return;
        }

        var request = new UpdateUserRequestDataContract
        {
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AllowedClassifications = user.AllowedClassifications
        };

        if (_passwordValid)
            request.Password = _password;

        if (user.Id != null)
        {
            AccountService.Update(user.Id.Value, request);
            NotificationService.Notify(NotificationFactory.Success("User Updated", "Details updated successfully."));
        }
            
    }

    private void OnValidPassword(string password)
    {
        _passwordValid = true;
        _password = password;
        StateHasChanged();
    }

    private void ShowPasswordRow()
    {
        _showPasswordRow = true;
    }

    private void OnInvalidPassword()
    {
        _passwordValid = false;
        StateHasChanged();
    }
}