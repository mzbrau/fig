namespace Fig.Client.Abstractions.Validation;

internal static class DisplayScriptPath
{
    public const string NestedSettingSeparator = "->";
    public const string ScriptPropertySeparator = ".";

    public static string NormalizePropertyName(string propertyName)
    {
        return propertyName.Replace(NestedSettingSeparator, ScriptPropertySeparator);
    }
}
