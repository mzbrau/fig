using Fig.Client.Abstractions.Data;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.Authentication;
using Fig.Web.Models.Setting;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages.Setting;

public partial class SettingIcons
{
    [Parameter]
    public ISetting Setting { get; set; } = null!;
    
    [Parameter]
    public double FontSize { get; set; } = 1.0;
    
    [Parameter]
    public EventCallback<(ElementReference element, string tooltip, bool multiLine)> ShowTooltip { get; set; }
    
    [Inject]
    private IAccountService AccountService { get; set; } = null!;
    
    [Inject]
    private IScriptRunner ScriptRunner { get; set; } = null!;
    
    private bool IsAdmin => AccountService.AuthenticatedUser?.Role == Role.Administrator;
    
    private string GetIconStyle()
    {
        return $"font-size: {FontSize}em; margin-left: {(FontSize >= 1.0 ? 6 : 4)}px";
    }
    
    private string GetBoltIconStyle()
    {
        return $"font-size: {FontSize}em; margin-left: {(FontSize >= 1.0 ? 6 : 4)}px; font-variation-settings: 'FILL' 1;";
    }
    
    private string GetStarsIconStyle()
    {
        return $"font-size: {FontSize}em; margin-left: {(FontSize >= 1.0 ? 6 : 4)}px; font-variation-settings: 'FILL' 1;";
    }
    
    private string GetJavascriptIconStyle()
    {
        return "font-size: 1.5em; margin-left: 6px";
    }
    
    private string GetScheduleIconStyle()
    {
        return $"font-size: {FontSize}em; margin-left: 4px";
    }
    
    private int GetDependentSettingsCount()
    {
        if (Setting?.Parent?.Settings == null) return 0;
        return Setting.Parent.Settings.Count(s => s.DependsOnProperty == Setting.Name);
    }
    
    private async Task OnShowTooltip(ElementReference element, string tooltip, bool multiLine = false)
    {
        if (ShowTooltip.HasDelegate)
        {
            await ShowTooltip.InvokeAsync((element, tooltip, multiLine));
        }
    }
}
