namespace Fig.Web.Pages.Dialogs;

public sealed class DashboardPropertiesDialogResult
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool AdminOnly { get; set; }

    public int StatusSeconds { get; set; }

    public int SettingsSeconds { get; set; }
}
