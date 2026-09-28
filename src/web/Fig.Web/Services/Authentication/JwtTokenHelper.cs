using System.Text;
using Newtonsoft.Json;

namespace Fig.Web.Services.Authentication;

public static class JwtTokenHelper
{
    public static bool IsExpired(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return true;

        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
                return true;

            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (data == null || !data.TryGetValue("exp", out var expiryValue))
                return true;

            var expiry = Convert.ToInt64(expiryValue);
            return DateTimeOffset.UtcNow >= DateTimeOffset.FromUnixTimeSeconds(expiry);
        }
        catch
        {
            return true;
        }
    }
}
