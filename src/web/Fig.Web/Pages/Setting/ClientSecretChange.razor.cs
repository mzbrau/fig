using Fig.Web.Facades;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace Fig.Web.Pages.Setting;

public partial class ClientSecretChange
{
    private DateTime _oldClientSecretExpiry = DateTime.Now.AddHours(1);
    private string _newClientSecret = string.Empty;
    private bool _secretIsInvalid = true;

    [Parameter] 
    public string ClientName { get; set; } = null!;
    
    [Inject] 
    private DialogService DialogService { get; set; } = null!;
    
    [Inject]
    private ISettingClientFacade SettingClientFacade { get; set; } = null!;
    
    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private INotificationFactory NotificationFactory { get; set; } = null!;
    
    [Inject] 
    public IJSRuntime JavascriptRuntime { get; set; } = null!;

    private string NewClientSecret
    {
        get => _newClientSecret;
        set
        {
            if (value != _newClientSecret)
            {
                _newClientSecret = value;
                _secretIsInvalid = !Guid.TryParse(_newClientSecret, out _);
            }
        }
    }

    private async Task GenerateNewSecret()
    {
        NewClientSecret = Guid.NewGuid().ToString("N");
        await InvokeAsync(StateHasChanged);
    }

    private async Task ChangeSecret()
    {
        try
        {
            var result = await SettingClientFacade.ChangeClientSecret(ClientName,
                NewClientSecret,
                _oldClientSecretExpiry);

            ShowNotification(NotificationFactory.Success("Secret Change",
                $"Client secret has been changed for {result.ClientName}. " +
                $"Clients with the previous secret will be accepted until {result.OldSecretExpiryUtc.ToLocalTime():F}"));
        }
        catch (Exception ex)
        {
            ShowNotification(NotificationFactory.Failure("Secret Change",
                $"Secret Change Failed for client {ClientName}: {ex.Message}"));

        }
        finally
        {
            DialogService.Close();
        }
    }
    
    private void ShowNotification(NotificationMessage message)
    {
        NotificationService.Notify(message);
    }

    private async Task CopySecretToClipboard()
    {
        await JavascriptRuntime.InvokeVoidAsync("navigator.clipboard.writeText", NewClientSecret);
        ShowNotification(NotificationFactory.Info("Success", "Secret Copied."));
    }
}