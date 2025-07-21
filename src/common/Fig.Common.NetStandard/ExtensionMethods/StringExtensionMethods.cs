namespace Fig.Common.NetStandard.ExtensionMethods;

public static class StringExtensionMethods
{
    public static bool IsValidCssColor(this string color)
    {
        // Simple validation for common CSS color formats
        return System.Text.RegularExpressions.Regex.IsMatch(color, 
            @"^(#([0-9a-fA-F]{3}){1,2}|rgb\([^)]+\)|rgba\([^)]+\)|[a-zA-Z]+)$");
    }
}