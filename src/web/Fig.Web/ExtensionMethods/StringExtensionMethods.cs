using Microsoft.AspNetCore.Components;

namespace Fig.Web.ExtensionMethods;

public static class StringExtensionMethods
{
    public static string? QueryString(this NavigationManager navigationManager, string key)
    {
        return navigationManager.QueryString()[key];
    }
}