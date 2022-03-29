using Fig.Contracts.Authentication;
using Fig.Web.Facades;
using Fig.Web.Models.Authentication;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Namotion.Reflection;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class Users
{
    public List<UserModel> UserCollection => UsersFacade.UserCollection;
    
    private RadzenDataGrid<UserModel> userGrid;

    private string CrackableMessage { get; set; } = string.Empty;
    private int PasswordStrength { get; set; } = -1;

    private List<Role> Roles { get; } = Enum.GetValues(typeof(Role))
        .Cast<Role>()
        .ToList();


    [Inject]
    private IUsersFacade UsersFacade { get; set; } = null!;
    
    [Inject]
    private DialogService DialogService { get; set; } = null!;
    
    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private INotificationFactory NotificationFactory { get; set; } = null!;
    
    protected override async Task OnInitializedAsync()
    {
        await UsersFacade.LoadAllUsers();
        await base.OnInitializedAsync();
    }
    
    private async Task EditRow(UserModel row)
    {
        row.Snapshot();
        await userGrid.EditRow(row);
    }

    private async Task SaveRow(UserModel row)
    {
        var error = row.Validate(PasswordStrength);
        if (error != null)
        {
            await ShowCloseableFromOverlayDialog(error);
            return;
        }

        if (!await AddOrUpdateUser(row))
            return;
        
        await userGrid.UpdateRow(row);
        ResetPasswordInformation();
    }
    
    private async Task<bool> AddOrUpdateUser(UserModel user)
    {
        if (user.Id == null)
        {
            try
            {
                await UsersFacade.AddUser(user);
                NotificationService.Notify(NotificationFactory.Success("Success", $"User {user.Username} successfully created."));
            }
            catch (Exception e)
            {
                NotificationService.Notify(NotificationFactory.Failure("Add User Failed", e.Message));
                return false;
            }
        }
        else
        {
            try
            {
                await UsersFacade.SaveUser(user);
                NotificationService.Notify(NotificationFactory.Success("Success", $"User {user.Username} successfully updated."));
            }
            catch (Exception e)
            {
                NotificationService.Notify(NotificationFactory.Failure("Update User Failed", e.Message));
                return false;
            }
        }

        return true;
    }

    private async Task CancelEdit(UserModel row)
    {
        row.Revert();
        ResetPasswordInformation();
        userGrid.CancelEditRow(row);
        if (row.Id == null)
        {
            UserCollection.Remove(row);
            await userGrid.Reload();
        }
    }

    private async Task DeleteRow(UserModel row)
    {
        try
        {
            await UsersFacade.DeleteUser(row);
            UserCollection.Remove(row);
            await userGrid.Reload();
            NotificationService.Notify(NotificationFactory.Success("Success", $"User {row.Username} successfully removed."));
        }
        catch (Exception e)
        {
            NotificationService.Notify(NotificationFactory.Failure("Failed", $"Unable to delete user. {e.Message}"));
        }
    }

    private async Task AddUser()
    {
        var rowToInsert = new UserModel();
        UserCollection.Add(rowToInsert);
        await userGrid.Reload();
        await EditRow(rowToInsert);
    }

    private bool ValidatePassword(string password)
    {
        var result = Zxcvbn.Core.EvaluatePassword(password);
        PasswordStrength = result.Score;
        CrackableMessage = $"Crackable in {result.CrackTimeDisplay.OfflineSlowHashing1e4PerSecond}";

        return result.Score >= 3;
    }

    private void PasswordChanged(UserModel user, string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            ResetPasswordInformation();
            return;
        }

        if (ValidatePassword(password))
            user.Password = password;
    }

    private void ResetPasswordInformation()
    {
        PasswordStrength = -1;
        CrackableMessage = string.Empty;
    }
}