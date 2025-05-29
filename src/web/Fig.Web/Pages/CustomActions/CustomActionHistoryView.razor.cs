using System;
using System.Threading.Tasks;
using Fig.Web.Facades;
using Fig.Web.Models.CustomActions;
using Fig.Web.Utils; // For Modal
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages.CustomActions
{
    public partial class CustomActionHistoryView
    {
        private CustomActionHistoryModel? _history;
        private bool _isLoading = true;
        private Modal? ExecutionDetailsModal { get; set; }
        private CustomActionExecutionStatusModel? _selectedExecutionDetails;
        
        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PageSize = 10; // Or make configurable

        [Parameter]
        public Guid CustomActionId { get; set; }

        [Parameter]
        public string CustomActionName { get; set; } = "Custom Action"; // Default name

        protected override async Task OnInitializedAsync()
        {
            await LoadHistoryPage(1);
        }

        private async Task LoadHistoryPage(int pageNumber)
        {
            _isLoading = true;
            _currentPage = pageNumber;
            StateHasChanged(); // Update UI to show loading state

            var offset = (pageNumber - 1) * PageSize;
            var historyContract = await CustomActionsFacade.GetExecutionHistory(CustomActionId, PageSize, offset, CancellationToken.None);
            if (historyContract != null)
            {
                _history = new CustomActionHistoryModel(historyContract);
                // Assuming the API doesn't directly provide total count for pagination in this contract.
                // A more robust pagination would require total count from API.
                // For now, if less than PageSize items are returned, assume it's the last page.
                // Or, if an actual total count is available (e.g. as another property on historyContract or a separate API call), use that.
                // Placeholder:
                if (_history.Executions.Count < PageSize && pageNumber == 1 && _history.Executions.Count == 0)
                {
                    _totalPages = 1;
                }
                else if (_history.Executions.Count < PageSize) // Simple inference, might not be accurate if total is a multiple of PageSize
                {
                     _totalPages = pageNumber;
                }
                else // More data might exist, allow user to try next page
                {
                    _totalPages = pageNumber + 1; // At least one more page might exist.
                }
            }
            else
            {
                _history = null;
                _totalPages = 1;
            }
            _isLoading = false;
            StateHasChanged();
        }
        
        private string GetStatusBadgeClass(string status) => status switch
        {
            "Completed" => "badge bg-success",
            "Failed" => "badge bg-danger",
            "Executing" => "badge bg-info text-dark",
            "Pending" => "badge bg-warning text-dark",
            _ => "badge bg-secondary"
        };

        private string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        private async Task ViewExecutionDetails(Guid executionId)
        {
            var statusContract = await CustomActionsFacade.GetExecutionStatus(executionId, CancellationToken.None);
            if (statusContract != null)
            {
                _selectedExecutionDetails = new CustomActionExecutionStatusModel(statusContract);
                if (ExecutionDetailsModal != null)
                {
                    await ExecutionDetailsModal.Show();
                }
            }
            else
            {
                // Handle error: execution details not found
                await JsRuntime.InvokeVoidAsync("alert", $"Details for execution ID {executionId} could not be retrieved.");
            }
        }
    }
}
