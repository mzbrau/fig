using Fig.Web.Facades;
using Fig.Web.Factories;
using Fig.Web.Models.WebHooks;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class WebHooks
{
    [Inject] private IWebHookFacade WebHookFacade { get; set; } = null!;

    private RadzenDataGrid<WebHookClientModel> _webHookClientsGrid = null!;
    private WebHookClientModel? _pendingNewClientToEdit;

    private List<WebHookClientModel> WebHookClients => WebHookFacade?.WebHookClients ?? new List<WebHookClientModel>();

    [Inject] private DialogService DialogService { get; set; } = null!;

    [Inject] private NotificationService NotificationService { get; set; } = null!;

    [Inject] private INotificationFactory NotificationFactory { get; set; } = null!;

    [Inject] private IWebHookTypeFactory WebHookTypeFactory { get; set; } = null!;

    [Inject] public IJSRuntime JavascriptRuntime { get; set; } = null!;

    private List<WebHookTypeEnumerable> WebHookTypes { get; } = new();
    private bool _isLoading = true;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _isLoading = true;
            StateHasChanged();

            // Ensure all dependencies are available
            if (WebHookFacade == null)
            {
                _errorMessage = "WebHook service is not available.";
                return;
            }

            if (WebHookTypeFactory == null)
            {
                _errorMessage = "WebHook type factory is not available.";
                return;
            }

            // Load webhook types safely
            var webHookTypes = WebHookTypeFactory.GetWebHookTypes();
            if (webHookTypes != null)
            {
                foreach (var item in webHookTypes)
                {
                    WebHookTypes.Add(item);
                }
            }

            // Load data safely
            await WebHookFacade.LoadAllClients();
            await WebHookFacade.LoadAllWebHooks();

            await base.OnInitializedAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load webhook data: {ex.Message}";
            NotificationService?.Notify(NotificationFactory?.Failure("Load Error", _errorMessage));
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _webHookClientsGrid != null)
        {
            try
            {
                await _webHookClientsGrid.Reload();
            }
            catch (Exception ex)
            {
                // Grid reload error - this is non-critical
                Console.WriteLine($"Grid reload error: {ex.Message}");
            }
        }

        // If we added a client when the grid wasn't rendered (empty state),
        // the grid will now be in the DOM. Enter edit mode for that row.
        if (_pendingNewClientToEdit is not null && _webHookClientsGrid is not null)
        {
            try
            {
                await _webHookClientsGrid.Reload();
                await _webHookClientsGrid.EditRow(_pendingNewClientToEdit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Deferred edit error: {ex.Message}");
            }
            finally
            {
                _pendingNewClientToEdit = null;
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task AddClient()
    {
        var newModel = new WebHookClientModel();
        WebHookClients.Add(newModel);

        // Always defer entering edit mode until after the grid renders. This covers
        // both states: when the grid is currently visible and when we're in the
        // empty-state view (no grid in the DOM). OnAfterRenderAsync will pick this up.
        _pendingNewClientToEdit = newModel;
        await InvokeAsync(StateHasChanged);
    }

    private async Task EditRow(WebHookClientModel row)
    {
        row.Snapshot();
        await _webHookClientsGrid.EditRow(row);
    }

    private async Task SaveRow(WebHookClientModel row)
    {
        var error = row.Validate(WebHookClients);
        if (error is not null)
        {
            await ShowCloseableFromOverlayDialog("Cannot Save Web Hook Client", error);
            return;
        }

        if (!await AddOrUpdateClient(row))
            return;

        if (row.UpdateSecret)
        {
            NotificationService.Notify(NotificationFactory.Info("Processing...",
                "Hashing secret, this takes a few seconds"));

            await Task.Delay(100); // Hack to ensure that the notification is shown before hashing begins.
            var result = await Task.Run(() => GetHashedSecretMessage(row));
            await ShowClientHashDialog(result);
            row.Save();
        }

        await _webHookClientsGrid.UpdateRow(row);
    }

    private (string message, string hash) GetHashedSecretMessage(WebHookClientModel client)
    {
        if (string.IsNullOrWhiteSpace(client.Secret))
        {
            return ("Error: Secret was not set.", string.Empty);
        }

        var hash = BCrypt.Net.BCrypt.EnhancedHashPassword(client.Secret);
        return ($"Use the following hashed secret in {client.Name} " +
                $"to validate requests to ensure they are from Fig.{Environment.NewLine}" +
                $"This will only be shown once.", hash);
    }

    private async Task CancelEdit(WebHookClientModel row)
    {
        row.Revert();
        _webHookClientsGrid.CancelEditRow(row);
        if (row.Id is null)
        {
            WebHookClients.Remove(row);
            await _webHookClientsGrid.Reload();
        }
    }

    private async Task<bool> AddOrUpdateClient(WebHookClientModel client)
    {
        if (client.Id is null)
        {
            try
            {
                await WebHookFacade.AddClient(client);
                NotificationService.Notify(NotificationFactory.Success("Client Created",
                    $"Webhook client '{client.Name}' has been created successfully."));
            }
            catch (Exception e)
            {
                NotificationService.Notify(NotificationFactory.Failure("Creation Failed",
                    $"Failed to create webhook client: {e.Message}"));
                return false;
            }
        }

        else
        {
            try
            {
                await WebHookFacade.SaveClient(client);
                NotificationService.Notify(NotificationFactory.Success("Client Updated",
                    $"Webhook client '{client.Name}' has been updated successfully."));
            }
            catch (Exception e)
            {
                NotificationService.Notify(NotificationFactory.Failure("Update Failed",
                    $"Failed to update webhook client: {e.Message}"));
                return false;
            }
        }

        return true;
    }

    private async Task DeleteRow(WebHookClientModel row)
    {
        if (!await GetDeleteConfirmation(row.Name ?? "this webhook client"))
            return;

        try
        {
            await WebHookFacade.DeleteClient(row);
            await _webHookClientsGrid.Reload();
            if (row.Id is not null)
                NotificationService.Notify(NotificationFactory.Success("Client Deleted",
                    $"Webhook client '{row.Name}' has been removed successfully."));
        }
        catch (Exception e)
        {
            NotificationService.Notify(NotificationFactory.Failure("Delete Failed",
                $"Failed to delete webhook client: {e.Message}"));
        }
    }

    private void AddWebHook()
    {
        WebHookFacade?.WebHooks?.Add(new WebHookModel() { IsInEditMode = true });
    }

    private async Task SaveWebHook(WebHookModel webHook)
    {
        var error = webHook.Validate();
        if (!string.IsNullOrEmpty(error))
        {
            NotificationService.Notify(NotificationFactory.Failure("Validation Error", error));
            return;
        }

        try
        {
            await WebHookFacade.SaveWebHook(webHook);
            NotificationService.Notify(NotificationFactory.Success("Webhook Saved",
                "Webhook definition has been saved successfully."));
            webHook.IsInEditMode = false;
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationFactory.Failure("Save Failed",
                $"Failed to save webhook: {ex.Message}"));
        }
    }

    private async Task DeleteWebHook(WebHookModel webHook)
    {
        if (!await GetDeleteConfirmation("this webhook definition"))
            return;

        try
        {
            await WebHookFacade.DeleteWebHook(webHook);
            NotificationService.Notify(NotificationFactory.Success("Webhook Deleted",
                "Webhook definition has been removed successfully."));
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationFactory.Failure("Delete Failed",
                $"Failed to delete webhook: {ex.Message}"));
        }
    }

    private async Task TestClient(WebHookClientModel client)
    {
        // Prevent testing until the client exists on the server
        if (client?.Id is null)
        {
            NotificationService.Notify(NotificationFactory.Info("Not Ready",
                "Save this webhook client before running a test."));
            return;
        }

        try
        {
            client.TestPending = true;
            try
            {
                var result = await WebHookFacade.TestClient(client);

                if (result is not null)
                    await ShowTestResultDialog(result);
                else
                    NotificationService.Notify(NotificationFactory.Info("No Results",
                        "The server did not return any test results."));
            }
            catch (Exception ex)
            {
                // Surface a friendly message instead of breaking the UI
                NotificationService.Notify(NotificationFactory.Failure("Test Failed",
                    $"Could not complete test: {ex.Message}"));
            }
        }
        finally
        {
            client.TestPending = false;
        }
    }
}