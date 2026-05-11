using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Web.Models.Authentication;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
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
    
    private bool _showPasswordRow;
    private string _password = string.Empty;
    private bool _passwordValid;
    
    private List<Classification> AllClassifications { get; } = Enum.GetValues(typeof(Classification))
        .Cast<Classification>()
        .ToList();

    private bool IsForcedPasswordChange => AccountService.AuthenticatedUser?.PasswordChangeRequired == true;

    protected override async Task OnInitializedAsync()
    {
        if (AccountService.AuthenticatedUser?.PasswordChangeRequired == true)
        {
            _showPasswordRow = true;
        }

        await base.OnInitializedAsync();
    }

    private async Task Submit(AuthenticatedUserModel user)
    {
        if (AccountService.AuthenticatedUser?.PasswordChangeRequired == true && !_passwordValid)
        {
            NotificationService.Notify(NotificationFactory.Failure(
                "Password Change Required",
                "Enter a password rated Good or better before saving."));
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
            try
            {
                var completedForcedPasswordChange = IsForcedPasswordChange && _passwordValid;
                await AccountService.Update(user.Id.Value, request);
                if (!completedForcedPasswordChange)
                {
                    var summary = _passwordValid ? "Password Updated" : "Account Updated";
                    var message = _passwordValid ? "Password updated successfully." : "Details updated successfully.";
                    NotificationService.Notify(NotificationFactory.Success(summary, message));
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationFactory.Failure("User Update Failed", ex.Message));
            }
        }
            
    }

    private void OnValidPassword(string password)
    {
        _passwordValid = true;
        _password = password;
        StateHasChanged();
    }

    private Task OnPasswordChanged(string password)
    {
        _password = password;
        return Task.CompletedTask;
    }

    private void ShowPasswordRow()
    {
        _showPasswordRow = true;
    }

    private void OnInvalidPassword(string password)
    {
        _passwordValid = false;
        _password = password;
        StateHasChanged();
    }
}
