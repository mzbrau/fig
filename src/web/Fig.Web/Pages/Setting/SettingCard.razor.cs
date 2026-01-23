using Fig.Common.Events;
using Fig.Contracts.Authentication;
using Fig.Web.Events;
using Fig.Web.Models.Setting;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages.Setting;

public partial class SettingCard : IAsyncDisposable
{
    private const int IndentationPixelMultiplier = 10;
    private ElementReference _compactCategoryLine;
    private ElementReference _categoryLine;
    private bool _isGroupManagedSettingVisible;
    private bool _showExpandIcon;
    private readonly Action _refreshViewCallback;

    public SettingCard()
    {
        _refreshViewCallback = StateHasChanged;
    }

    [Parameter]
    public ISetting Setting { get; set; } = null!;
    
    [Inject]
    private TooltipService TooltipService { get; set; } = null!;
    
    [Inject]
    private IAccountService AccountService { get; set; } = null!;
    
    [Inject]
    private IEventDistributor EventDistributor { get; set; } = null!;
    
    [Inject]
    private DialogService DialogService { get; set; } = null!;
    
    private bool IsReadOnlyUser => AccountService.AuthenticatedUser?.Role == Role.ReadOnly;
    
    private bool IsAdmin => AccountService.AuthenticatedUser?.Role == Role.Administrator;
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        // Subscribe to refresh events to recalculate widths when view changes
        EventDistributor.Subscribe(EventConstants.RefreshView, _refreshViewCallback);
    }

    public ValueTask DisposeAsync()
    {
        EventDistributor.Unsubscribe(EventConstants.RefreshView, _refreshViewCallback);
        return ValueTask.CompletedTask;
    }

    private (double nameWidth, double valueWidth) CalculateOptimalWidths()
    {
        if (Setting?.Parent?.Settings == null) return (40, 50);
        
        var compactSettings = Setting.Parent.Settings.Where(s => s.IsCompactView && !s.Hidden).ToList();
        if (!compactSettings.Any()) return (40, 50);
        
        var maxNameLength = compactSettings.Max(s => GetDisplayTextLength(s.DisplayName));
        var maxValueLength = compactSettings.Max(s => GetDisplayTextLength(s.GetStringValue(100)));
        
        // Calculate relative widths based on content lengths
        var totalLength = maxNameLength + maxValueLength;
        if (totalLength == 0) return (40, 50);
        
        // Calculate percentage distribution with constraints
        var namePercentage = Math.Min(Math.Max((double)maxNameLength / totalLength * 100, 25), 65);
        var valuePercentage = Math.Min(Math.Max((double)maxValueLength / totalLength * 100, 30), 70);
        
        // Ensure total doesn't exceed 95% (leave room for spacing and badges)
        var total = namePercentage + valuePercentage;
        if (total > 95)
        {
            var scale = 95.0 / total;
            namePercentage *= scale;
            valuePercentage *= scale;
        }
        
        return (namePercentage, valuePercentage);
    }
    
    private int GetDisplayTextLength(string text)
    {
        if (string.IsNullOrEmpty(text)) 
            return 0;
        
        // For very long text, consider character density differently
        return Math.Min(text.Length, 100);
    }
    
    private string GetCompactViewStyle()
    {
        var (nameWidth, valueWidth) = CalculateOptimalWidths();
        return $"--name-width: {nameWidth:F1}%; --value-width: {valueWidth:F1}%;";
    }

    private void ShowExpandIcon()
    {
        _showExpandIcon = true;
        StateHasChanged();
    }

    private void HideExpandIcon()
    {
        _showExpandIcon = false;
        StateHasChanged();
    }

    private void ShowTooltip(ElementReference elementReference, string tooltipText, TooltipPosition position = TooltipPosition.Bottom, bool multiLine = false)
    {
        if (string.IsNullOrWhiteSpace(tooltipText))
            return;

        var style = "background-color: black";
        var options = new TooltipOptions
        {
            Position = position,
            Style = style,
            Duration = multiLine ? 20000 : 6000,
        };

        if (multiLine)
        {
            RenderFragment<TooltipService> content = (tooltipService) => builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "style", "white-space:pre");
                builder.AddContent(2, tooltipText);
                builder.CloseElement();
            };
            TooltipService.Open(elementReference, content, options);
        }
        else
        {
            TooltipService.Open(elementReference, tooltipText, options);
        }
    }
    
    private void HandleTooltipRequest((ElementReference element, string tooltip, bool multiLine) request)
    {
        ShowTooltip(request.element, request.tooltip, TooltipPosition.Bottom, request.multiLine);
    }

    private void ToggleSettingCompactView(MouseEventArgs mouseEventArgs)
    {
        Setting.ToggleCompactView(mouseEventArgs.CtrlKey);
        EventDistributor.Publish(EventConstants.RefreshView);
    }

    private async Task UnlockSetting(ISetting setting)
    {
        if (!setting.IsReadOnly)
            return;

        var unlock = await GetUnlockConfirmation(setting);
        
        if (unlock)
            setting.Unlock();
    }
    
    private async Task<bool> GetUnlockConfirmation(ISetting setting)
    {
        var result = await DialogService.OpenAsync("Confirm Unlock", ds =>
        {
            RenderFragment fragment = builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "style", "padding: 0 1rem 1rem 1rem;");
                
                builder.OpenElement(2, "p");
                builder.AddAttribute(3, "class", "mb-3");
                builder.AddContent(4, $"{setting.DisplayName} is externally managed, changes made in the Fig UI might be overridden.");
                builder.CloseElement();
                
                builder.OpenElement(5, "p");
                builder.AddAttribute(6, "class", "mb-3");
                builder.AddAttribute(7, "style", "color: var(--rz-text-secondary-color);");
                builder.AddContent(8, "Consider any changes made here temporary.");
                builder.CloseElement();
                
                builder.OpenElement(9, "p");
                builder.AddAttribute(10, "class", "mb-4");
                builder.AddContent(11, "Do you want to proceed?");
                builder.CloseElement();
                
                builder.OpenElement(12, "div");
                builder.AddAttribute(13, "style", "display: flex; justify-content: flex-end; gap: 0.5rem; padding-top: 0.5rem;");
                
                builder.OpenComponent<RadzenButton>(14);
                builder.AddAttribute(15, "Text", "No");
                builder.AddAttribute(16, "Click", EventCallback.Factory.Create<MouseEventArgs>(this, () => ds.Close(false)));
                builder.AddAttribute(17, "ButtonStyle", ButtonStyle.Light);
                builder.AddAttribute(18, "Style", "min-width: 80px;");
                builder.CloseComponent();
                
                builder.OpenComponent<RadzenButton>(19);
                builder.AddAttribute(20, "Text", "Yes");
                builder.AddAttribute(21, "Click", EventCallback.Factory.Create<MouseEventArgs>(this, () => ds.Close(true)));
                builder.AddAttribute(22, "ButtonStyle", ButtonStyle.Primary);
                builder.AddAttribute(23, "Style", "min-width: 80px;");
                builder.CloseComponent();
                
                builder.CloseElement(); // flex container
                builder.CloseElement(); // div
            };
            return fragment;
        });
        
        return result ?? false;
    }

    private void ToggleClientListVisibility()
    {
        _isGroupManagedSettingVisible = !_isGroupManagedSettingVisible;
    }
    
    private string GetIndentStyle()
    {
        var styles = new List<string>();
        
        // Calculate indent based on IndentAttribute if present
        if (Setting.Indent is > 0)
        {
            var indentPixels = Setting.Indent.Value * IndentationPixelMultiplier; // 10px per indent level
            styles.Add($"margin-left: {indentPixels}px");
        }
        // Fall back to the original logic for EnablesSettings
        else if (Setting.IsEnabledByOtherSetting)
        {
            styles.Add($"margin-left: {IndentationPixelMultiplier}px");
        }
        
        return string.Join("; ", styles);
    }
}
