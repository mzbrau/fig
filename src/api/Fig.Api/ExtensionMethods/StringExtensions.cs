using System.Text.RegularExpressions;

namespace Fig.Api.ExtensionMethods
{
    public static class StringExtensions
    {
        /// <summary>
        /// Sanitizes a string for safe logging by removing newlines and other potentially risky elements.
        /// </summary>
        /// <param name="input">The input string to sanitize.</param>
        /// <returns>A sanitized string safe for logging.</returns>
        public static string Sanitize(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove newlines, carriage returns, tabs, and non-printable characters
            var sanitized = Regex.Replace(input, "[\r\n\t]", string.Empty);
            sanitized = Regex.Replace(sanitized, "[\x00-\x1F\x7F]", string.Empty); // Remove ASCII control chars
            sanitized = sanitized.Trim();
            return sanitized;
        }
    }
}
