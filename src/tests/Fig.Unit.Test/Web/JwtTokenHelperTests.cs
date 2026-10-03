using Fig.Web.Services.Authentication;
using NUnit.Framework;
using System.Text;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class JwtTokenHelperTests
{
    [Test]
    public void IsExpired_ReturnsTrue_ForNullOrWhitespace()
    {
        Assert.That(JwtTokenHelper.IsExpired(null), Is.True);
        Assert.That(JwtTokenHelper.IsExpired(string.Empty), Is.True);
        Assert.That(JwtTokenHelper.IsExpired("   "), Is.True);
    }

    [Test]
    public void IsExpired_ReturnsTrue_ForMalformedToken()
    {
        Assert.That(JwtTokenHelper.IsExpired("not-a-jwt"), Is.True);
    }

    [Test]
    public void IsExpired_ReturnsTrue_WhenExpiryIsInThePast()
    {
        var token = CreateJwt(DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.That(JwtTokenHelper.IsExpired(token), Is.True);
    }

    [Test]
    public void IsExpired_ReturnsFalse_WhenExpiryIsInTheFuture()
    {
        var token = CreateJwt(DateTimeOffset.UtcNow.AddHours(1));

        Assert.That(JwtTokenHelper.IsExpired(token), Is.False);
    }

    [Test]
    public void TryGetExpiry_ReturnsExpiry_WhenPresent()
    {
        var expiry = DateTimeOffset.UtcNow.AddMinutes(30);
        var token = CreateJwt(expiry, DateTimeOffset.UtcNow);

        Assert.That(JwtTokenHelper.TryGetExpiry(token, out var parsed), Is.True);
        Assert.That(parsed.ToUnixTimeSeconds(), Is.EqualTo(expiry.ToUnixTimeSeconds()));
    }

    [Test]
    public void TryGetIssuedAt_ReturnsIssuedAt_WhenPresent()
    {
        var issuedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var token = CreateJwt(DateTimeOffset.UtcNow.AddHours(1), issuedAt);

        Assert.That(JwtTokenHelper.TryGetIssuedAt(token, out var parsed), Is.True);
        Assert.That(parsed.ToUnixTimeSeconds(), Is.EqualTo(issuedAt.ToUnixTimeSeconds()));
    }

    [Test]
    public void TryGetExpiry_ReturnsFalse_ForMalformedToken()
    {
        Assert.That(JwtTokenHelper.TryGetExpiry("not-a-jwt", out _), Is.False);
    }

    private static string CreateJwt(DateTimeOffset expiry, DateTimeOffset? issuedAt = null)
    {
        static string ToBase64Url(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        var header = ToBase64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payloadJson = issuedAt is null
            ? $"{{\"exp\":{expiry.ToUnixTimeSeconds()}}}"
            : $"{{\"iat\":{issuedAt.Value.ToUnixTimeSeconds()},\"exp\":{expiry.ToUnixTimeSeconds()}}}";
        var payload = ToBase64Url(payloadJson);
        return $"{header}.{payload}.sig";
    }
}
