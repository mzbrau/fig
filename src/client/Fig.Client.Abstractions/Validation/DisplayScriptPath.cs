namespace Fig.Client.Abstractions.Validation;

internal static class DisplayScriptPath
{
    public const string NestedSettingSeparator = "->";
    public const string ScriptPropertySeparator = ".";
    public const string SettingPlaceholder = "{{this}}";

    public static string NormalizePropertyName(string propertyName)
    {
        return propertyName.Replace(NestedSettingSeparator, ScriptPropertySeparator);
    }

    public static string? SubstitutePlaceholder(string? script, string settingName)
    {
        if (script is null)
            return null;

        if (script.Length == 0 || !script.Contains(SettingPlaceholder))
            return script;

        var normalizedName = NormalizePropertyName(settingName);
        return script.Replace(SettingPlaceholder, normalizedName);
    }
}
