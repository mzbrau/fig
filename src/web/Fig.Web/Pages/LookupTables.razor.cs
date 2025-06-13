using Fig.Contracts.Authentication;
using Fig.Web.Facades;
using Fig.Web.Models.LookupTables;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages
{
    public partial class LookupTables : ComponentBase
    {
        [Inject] private ILookupTablesFacade LookupTablesFacade { get; set; } = null!;

        [Inject] private NotificationService NotificationService { get; set; } = null!;

        [Inject] private INotificationFactory NotificationFactory { get; set; } = null!;

        [Inject] private DialogService DialogService { get; set; } = null!;

        [Inject] private IAccountService AccountService { get; set; } = null!;

        private bool IsReadOnly => AccountService.AuthenticatedUser?.Role == Role.ReadOnly;

        private List<Models.LookupTables.LookupTables> Items => LookupTablesFacade.Items;

        private Models.LookupTables.LookupTables? SelectedItem { get; set; }

        private RadzenDataGrid<LookupTablesItemModel> _itemGrid = default!;

        private bool _isDeleteInProgress;

        protected override async Task OnInitializedAsync()
        {
            await LookupTablesFacade.LoadAll();
            await base.OnInitializedAsync();
        }

        private async Task CreateNew()
        {
            var newItem = LookupTablesFacade.CreateNew();
            if (_itemGrid is not null)
                await _itemGrid.Reload();
            SelectedItem = newItem;
            SelectedItem.StartEditing();
        }

        private async Task OnDelete()
        {
            if (SelectedItem == null)
                return;

            var name = SelectedItem.Name;
            if (!await GetDeleteConfirmation(name))
                return;

            _isDeleteInProgress = true;
            await LookupTablesFacade.Delete(SelectedItem);
            await _itemGrid.Reload();
            _isDeleteInProgress = false;
            NotificationService.Notify(NotificationFactory.Success("Success", $"{name} Deleted Successfully"));
            SelectedItem = Items.FirstOrDefault();
        }

        private async Task Save()
        {
            if (SelectedItem != null)
            {
                SelectedItem.Save();
                var success = await LookupTablesFacade.Save(SelectedItem);

                if (success)
                {
                    await _itemGrid.Reload();
                    NotificationService.Notify(NotificationFactory.Success("Success",
                        $"{SelectedItem.Name} Saved Successfully. Page refresh is required to apply changes on settings page."));
                }
                // Error notification is already handled by HttpService, so we don't need to show another one
            }
        }

        private async Task Cancel()
        {
            if (SelectedItem != null)
            {
                // If this is a new table that hasn't been saved yet, remove it from the list
                if (SelectedItem.Id == null)
                {
                    await LookupTablesFacade.Delete(SelectedItem);
                    if (_itemGrid is not null)
                        await _itemGrid.Reload();
                    SelectedItem = Items.FirstOrDefault();
                }
                else
                {
                    // For existing tables, just cancel the edit
                    SelectedItem.CancelEdit();
                }

                StateHasChanged();
            }
        }
    }
}