namespace Fig.Integration.MicrosoftSentinel.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Anonymizes a string by keeping only the first and last characters and replacing the middle with a fixed number of asterisks.
    /// </summary>
    /// <param name="value">The string to anonymize</param>
    /// <param name="starCount">The number of asterisks to use (default: 5)</param>
    /// <returns>The anonymized string, or null/empty if input is null/empty</returns>
    public static string? Anonymize(this string? value, int starCount = 5)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length <= 2)
            return new string('*', starCount);

        return $"{value[0]}{new string('*', starCount)}{value[^1]}";
    }
}
