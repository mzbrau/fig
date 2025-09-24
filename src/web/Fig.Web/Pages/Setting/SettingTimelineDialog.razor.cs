using Fig.Common.Constants;
using Fig.Web.Models.Events;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages.Setting;

public partial class SettingTimelineDialog
{
    [Parameter] public string ClientName { get; set; } = string.Empty;
    [Parameter] public string? Instance { get; set; }
    [Parameter] public int TimelineDurationDays { get; set; } = 30;

    private bool _isLoading = true;
    private List<EventLogModel> _timelineEvents = [];
    private List<TimelineItem> _timelineItems = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadTimeline();
    }

    private async Task LoadTimeline()
    {
        _isLoading = true;
        
        try
        {
            _timelineEvents = await EventsFacade.GetClientTimeline(ClientName, Instance);
            _timelineItems = CreateTimelineItems(_timelineEvents);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading timeline: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private List<TimelineItem> CreateTimelineItems(List<EventLogModel> events)
    {
        var items = new List<TimelineItem>();
        
        // Group events by minute to consolidate multiple changes
        var eventGroups = events
            .GroupBy(e => new { 
                Date = e.Timestamp.Date, 
                Hour = e.Timestamp.Hour, 
                Minute = e.Timestamp.Minute 
            })
            .OrderByDescending(g => g.Key.Date)
            .ThenByDescending(g => g.Key.Hour)
            .ThenByDescending(g => g.Key.Minute);

        foreach (var group in eventGroups)
        {
            var groupEvents = group.OrderByDescending(e => e.Timestamp).ToList();
            var firstEvent = groupEvents.First();
            
            var item = new TimelineItem
            {
                DateTime = firstEvent.Timestamp
            };

            // Separate different event types
            var settingChanges = groupEvents.Where(e =>
                    e.EventType is EventMessage.SettingValueUpdated
                        or EventMessage.ExternallyManagedSettingUpdatedByUser)
                .ToList();
            var registrationEvents = groupEvents.Where(e => e.EventType is EventMessage.InitialRegistration).ToList();
            var clientDeletedEvents = groupEvents.Where(e => e.EventType is EventMessage.ClientDeleted).ToList();

            if (settingChanges.Count > 1)
            {
                // Multiple setting changes - group them
                item.Title = $"{settingChanges.Count} settings changed";
                item.AuthenticatedUser = settingChanges.First().AuthenticatedUser;
                item.Changes = settingChanges.Select(e => new ChangeDetail
                {
                    SettingName = e.SettingName ?? "Unknown",
                    NewValue = e.NewValue ?? "N/A",
                    OriginalValue = e.OriginalValue,
                    Message = e.Message
                }).ToList();
                item.PointStyle = "setting-change group";
            }
            else if (settingChanges.Count == 1)
            {
                // Single setting change
                var change = settingChanges.First();
                item.Title = $"{change.SettingName} changed";
                item.Details = change.Message ?? "Setting value was updated";
                item.AuthenticatedUser = change.AuthenticatedUser;
                item.Changes =
                [
                    new ChangeDetail
                    {
                        SettingName = change.SettingName ?? "Unknown",
                        NewValue = change.NewValue ?? "N/A",
                        OriginalValue = change.OriginalValue,
                        Message = change.Message
                    }
                ];
                item.PointStyle = "setting-change";
            }
            else if (registrationEvents.Any())
            {
                // Registration event
                var regEvent = registrationEvents.First();
                item.Title = regEvent.EventType;
                item.Details = "Client registered with Fig";
                item.AuthenticatedUser = regEvent.AuthenticatedUser;
                item.Changes = [];
                item.PointStyle = "registration";
            }
            else if (clientDeletedEvents.Any())
            {
                // Client deleted event
                var deleteEvent = clientDeletedEvents.First();
                item.Title = "Client deleted";
                item.Details = "Client was removed from Fig";
                item.AuthenticatedUser = deleteEvent.AuthenticatedUser;
                item.Changes = [];
                item.PointStyle = "client-deleted";
            }

            items.Add(item);
        }

        return items;
    }

    private string GetTimelineItemClass(TimelineItem item)
    {
        return item.PointStyle.Contains("group") ? "timeline-item-grouped" : "timeline-item-single";
    }

    private string TruncateValue(string? value, int maxLength = 60)
    {
        if (string.IsNullOrEmpty(value))
            return "N/A";
        
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }

    private string GetChangeTooltip(ChangeDetail change)
    {
        var tooltip = $"Setting: {change.SettingName}\n";
        if (!string.IsNullOrEmpty(change.OriginalValue))
            tooltip += $"Previous: {change.OriginalValue}\n";
        tooltip += $"New: {change.NewValue}";
        if (!string.IsNullOrEmpty(change.Message))
            tooltip += $"\nMessage: {change.Message}";
        return tooltip;
    }

    private class TimelineItem
    {
        public DateTime DateTime { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? AuthenticatedUser { get; set; }
        public List<ChangeDetail> Changes { get; set; } = [];
        public string PointStyle { get; set; } = string.Empty;
    }

    private class ChangeDetail
    {
        public string SettingName { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string? OriginalValue { get; set; }
        public string? Message { get; set; }
    }
}