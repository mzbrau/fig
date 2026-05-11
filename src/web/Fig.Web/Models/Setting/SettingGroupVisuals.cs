namespace Fig.Web.Models.Setting;

public static class SettingGroupVisuals
{
    public const string Icon = "hub";
    public const string Label = "Setting Group";

    public static string GetManagedByTooltip(string? groupName)
    {
        return string.IsNullOrWhiteSpace(groupName)
            ? "Managed by a setting group"
            : $"Managed by setting group {groupName}";
    }
}
