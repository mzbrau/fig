using System.Text;
using Newtonsoft.Json;

namespace Fig.Web.Services.Authentication;

public static class JwtTokenHelper
{
    public static bool IsExpired(string? token)
    {
        if (!TryGetExpiry(token, out var expiry))
            return true;

        return DateTimeOffset.UtcNow >= expiry;
    }

    public static bool TryGetExpiry(string? token, out DateTimeOffset expiry)
    {
        expiry = default;
        if (!TryGetPayloadClaim(token, "exp", out var value))
            return false;

        try
        {
            expiry = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(value));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryGetIssuedAt(string? token, out DateTimeOffset issuedAt)
    {
        issuedAt = default;
        if (!TryGetPayloadClaim(token, "iat", out var value))
            return false;

        try
        {
            issuedAt = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(value));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static TimeSpan GetWarningLead(TimeSpan tokenLifetime)
    {
        if (tokenLifetime <= TimeSpan.Zero)
            return TimeSpan.Zero;

        return tokenLifetime < TimeSpan.FromMinutes(4)
            ? tokenLifetime / 2
            : TimeSpan.FromMinutes(2);
    }

    private static bool TryGetPayloadClaim(string? token, string claim, out object value)
    {
        value = null!;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
                return false;

            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (data == null || !data.TryGetValue(claim, out var claimValue) || claimValue is null)
                return false;

            value = claimValue;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
