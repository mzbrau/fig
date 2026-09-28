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

    private static string CreateJwt(DateTimeOffset expiry)
    {
        static string ToBase64Url(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        var header = ToBase64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = ToBase64Url($"{{\"exp\":{expiry.ToUnixTimeSeconds()}}}");
        return $"{header}.{payload}.sig";
    }
}
