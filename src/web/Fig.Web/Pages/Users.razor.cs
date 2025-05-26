using Fig.Common.NetStandard.Data;
using Fig.Contracts.Authentication;
using Fig.Web.Facades;
using Fig.Web.Models.Authentication;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class Users
{
    private RadzenDataGrid<UserModel> _userGrid = default!;

    private PasswordWithRating _passwordWithRating = default!;
    
    public bool IsKeycloakEnabled { get; private set; }
    
    public List<UserModel> UserCollection => UsersFacade.UserCollection;

    private List<Role> Roles { get; } = Enum.GetValues(typeof(Role))
        .Cast<Role>()
        .ToList();
    
    private List<Classification> AllClassifications { get;  }= Enum.GetValues(typeof(Classification))
        .Cast<Classification>()
        .ToList();


    [Inject]
    private IUsersFacade UsersFacade { get; set; } = null!;

    [Inject]
    private DialogService DialogService { get; set; } = null!;

    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private INotificationFactory NotificationFactory { get; set; } = null!;
    
    [Inject]
    private IOptions<WebSettings> WebSettings { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        IsKeycloakEnabled = WebSettings.Value.UseKeycloak;
        await UsersFacade.LoadAllUsers(); // Users might still be listed in a read-only fashion
        await base.OnInitializedAsync();
    }

    private async Task EditRow(UserModel row)
    {
        row.Snapshot();
        await _userGrid.EditRow(row);
    }

    private async Task SaveRow(UserModel row)
    {
        var error = row.Validate(_passwordWithRating.PasswordStrength);
        if (error is not null)
        {
            await ShowCloseableFromOverlayDialog(error);
            return;
        }

        if (!await AddOrUpdateUser(row))
            return;

        await _userGrid.UpdateRow(row);
        _passwordWithRating.ResetPasswordInformation();
    }

    private async Task<bool> AddOrUpdateUser(UserModel user)
    {
        if (user.Id is null)
            try
            {
                await UsersFacade.AddUser(user);
                NotificationService.Notify(NotificationFactory.Success("Success",
                    $"User {user.Username} successfully created."));
            }
            catch (Exception e)
            {
                NotificationService.Notify(NotificationFactory.Failure("Add User Failed", e.Message));
                return false;
            }
        else
            try
            {
                await UsersFacade.SaveUser(user);
                NotificationService.Notify(NotificationFactory.Success("Success",
                    $"User {user.Username} successfully updated."));
            }
            catch (Exception e)
            {
                NotificationService.Notify(NotificationFactory.Failure("Update User Failed", e.Message));
                return false;
            }

        return true;
    }

    private async Task CancelEdit(UserModel row)
    {
        row.Revert();
        _passwordWithRating.ResetPasswordInformation();
        _userGrid.CancelEditRow(row);
        if (row.Id is null)
        {
            UserCollection.Remove(row);
            await _userGrid.Reload();
        }
    }

    private async Task DeleteRow(UserModel row)
    {
        if (!await GetDeleteConfirmation(row.Username))
            return;
        
        try
        {
            await UsersFacade.DeleteUser(row);
            UserCollection.Remove(row);
            await _userGrid.Reload();
            if (row.Id is not null)
                NotificationService.Notify(NotificationFactory.Success("Success",
                    $"User {row.Username} successfully removed."));
        }
        catch (Exception e)
        {
            NotificationService.Notify(NotificationFactory.Failure("Failed", $"Unable to delete user. {e.Message}"));
        }
    }

    private async Task AddUser()
    {
        if (IsKeycloakEnabled)
        {
            NotificationService.Notify(NotificationFactory.Warning("Read Only", "User management is handled by Keycloak."));
            return;
        }
        
        var rowToInsert = new UserModel
        {
            ClientFilter = ".*"
        };
        UserCollection.Add(rowToInsert);
        await _userGrid.Reload();
        await EditRow(rowToInsert);
    }
    
    private void OnValidPassword(UserModel user, string password)
    {
        user.Password = password;
    }
}