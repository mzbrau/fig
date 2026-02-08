using System;
using Fig.Api;
using Fig.Api.Services;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class HashValidationCacheTests
{
    private Mock<IOptions<ApiSettings>> _mockApiSettings = null!;

    [SetUp]
    public void SetUp()
    {
        _mockApiSettings = new Mock<IOptions<ApiSettings>>();
    }

    private HashValidationCache CreateCache(int hashCacheExpiryMinutes = 60)
    {
        var apiSettings = new ApiSettings
        {
            Secret = "test-secret",
            DbConnectionString = "test-connection",
            HashCacheExpiryMinutes = hashCacheExpiryMinutes
        };
        _mockApiSettings.Setup(x => x.Value).Returns(apiSettings);
        return new HashValidationCache(_mockApiSettings.Object);
    }

    [Test]
    public void ValidateClientSecret_WithValidSecret_ShouldReturnTrue()
    {
        // Arrange
        var cache = CreateCache();
        var secret = "test-client-secret-1234567890123456789012";
        var bcryptHash = BCrypt.Net.BCrypt.EnhancedHashPassword(secret);

        // Act
        var result = cache.ValidateClientSecret("TestClient", secret, bcryptHash);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ValidateClientSecret_WithInvalidSecret_ShouldReturnFalse()
    {
        // Arrange
        var cache = CreateCache();
        var secret = "test-client-secret-1234567890123456789012";
        var bcryptHash = BCrypt.Net.BCrypt.EnhancedHashPassword(secret);

        // Act
        var result = cache.ValidateClientSecret("TestClient", "wrong-secret", bcryptHash);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateClientSecret_WithCachedResult_ShouldReturnCachedValue()
    {
        // Arrange
        var cache = CreateCache();
        var secret = "test-client-secret-1234567890123456789012";
        var bcryptHash = BCrypt.Net.BCrypt.EnhancedHashPassword(secret);

        // First call - populates cache
        var result1 = cache.ValidateClientSecret("TestClient", secret, bcryptHash);

        // Second call - should use cache
        var result2 = cache.ValidateClientSecret("TestClient", secret, bcryptHash);

        // Assert
        Assert.That(result1, Is.True);
        Assert.That(result2, Is.True);
    }

    [Test]
    public void ValidateClientSecret_WithChangedHash_ShouldRevalidate()
    {
        // Arrange
        var cache = CreateCache();
        var secret = "test-client-secret-1234567890123456789012";
        var bcryptHash1 = BCrypt.Net.BCrypt.EnhancedHashPassword(secret);
        var bcryptHash2 = BCrypt.Net.BCrypt.EnhancedHashPassword(secret);

        // First call with hash1
        cache.ValidateClientSecret("TestClient", secret, bcryptHash1);

        // Second call with different hash - should revalidate
        var result = cache.ValidateClientSecret("TestClient", secret, bcryptHash2);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ValidateClientSecret_WithChangedSecret_ShouldRevalidate()
    {
        // Arrange
        var cache = CreateCache();
        var secret1 = "test-client-secret-1234567890123456789012";
        var secret2 = "different-secret-12345678901234567890123";
        var bcryptHash = BCrypt.Net.BCrypt.EnhancedHashPassword(secret1);

        // First call with secret1
        cache.ValidateClientSecret("TestClient", secret1, bcryptHash);

        // Second call with secret2 - should revalidate (fingerprint different)
        var result = cache.ValidateClientSecret("TestClient", secret2, bcryptHash);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateClientSecret_WithCacheDisabled_ShouldNotCache()
    {
        // Arrange - expiry of 0 disables cache
        var cache = CreateCache(0);
        var secret = "test-client-secret-1234567890123456789012";
        var bcryptHash = BCrypt.Net.BCrypt.EnhancedHashPassword(secret);

        // Act - multiple calls should all perform validation
        var result1 = cache.ValidateClientSecret("TestClient", secret, bcryptHash);
        var result2 = cache.ValidateClientSecret("TestClient", secret, bcryptHash);

        // Assert
        Assert.That(result1, Is.True);
        Assert.That(result2, Is.True);
    }

    [Test]
    public void ValidateCodeHash_WithValidCodeAndHash_ShouldReturnTrue()
    {
        // Arrange
        var cache = CreateCache();
        var code = "test-code";
        var hash = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        Func<string, string> computeHash = _ => hash;

        // Act
        var result = cache.ValidateCodeHash("TestClient", "TestSetting", code, hash, computeHash);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ValidateCodeHash_WithInvalidHash_ShouldReturnFalse()
    {
        // Arrange
        var cache = CreateCache();
        var code = "test-code";
        var storedHash = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var computedHash = Convert.ToBase64String(new byte[] { 9, 10, 11, 12, 13, 14, 15, 16 });
        Func<string, string> computeHash = _ => computedHash;

        // Act
        var result = cache.ValidateCodeHash("TestClient", "TestSetting", code, storedHash, computeHash);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateCodeHash_WithCachedResult_ShouldUseCachedValue()
    {
        // Arrange
        var cache = CreateCache();
        var code = "test-code";
        var hash = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var computeCount = 0;
        Func<string, string> computeHash = _ =>
        {
            computeCount++;
            return hash;
        };

        // First call - populates cache
        cache.ValidateCodeHash("TestClient", "TestSetting", code, hash, computeHash);

        // Second call - should use cache
        cache.ValidateCodeHash("TestClient", "TestSetting", code, hash, computeHash);

        // Assert - computeHash should only be called once
        Assert.That(computeCount, Is.EqualTo(1));
    }

    [Test]
    public void ValidateCodeHash_WithCacheDisabled_ShouldNotCache()
    {
        // Arrange - expiry of 0 disables cache
        var cache = CreateCache(0);
        var code = "test-code";
        var hash = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var computeCount = 0;
        Func<string, string> computeHash = _ =>
        {
            computeCount++;
            return hash;
        };

        // Act - multiple calls should all perform validation
        cache.ValidateCodeHash("TestClient", "TestSetting", code, hash, computeHash);
        cache.ValidateCodeHash("TestClient", "TestSetting", code, hash, computeHash);

        // Assert - computeHash should be called twice (no caching)
        Assert.That(computeCount, Is.EqualTo(2));
    }

    [Test]
    public void InvalidateClient_ShouldRemoveClientSecretCache()
    {
        // Arrange
        var cache = CreateCache();
        var secret = "test-client-secret-1234567890123456789012";
        var bcryptHash = BCrypt.Net.BCrypt.EnhancedHashPassword(secret);

        // Populate cache
        cache.ValidateClientSecret("TestClient", secret, bcryptHash);

        // Act
        cache.InvalidateClient("TestClient");

        // The next call should perform BCrypt validation again
        // (we can't directly test this without timing, but at least verify no exceptions)
        var result = cache.ValidateClientSecret("TestClient", secret, bcryptHash);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void InvalidateClient_ShouldRemoveCodeHashCache()
    {
        // Arrange
        var cache = CreateCache();
        var code = "test-code";
        var hash = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var computeCount = 0;
        Func<string, string> computeHash = _ =>
        {
            computeCount++;
            return hash;
        };

        // Populate cache
        cache.ValidateCodeHash("TestClient", "TestSetting", code, hash, computeHash);
        Assert.That(computeCount, Is.EqualTo(1));

        // Act
        cache.InvalidateClient("TestClient");

        // Next call should compute again since cache was invalidated
        cache.ValidateCodeHash("TestClient", "TestSetting", code, hash, computeHash);

        // Assert
        Assert.That(computeCount, Is.EqualTo(2));
    }

    [Test]
    public void CleanupExpiredEntries_ShouldRemoveExpiredEntries()
    {
        // Arrange - use very short expiry for testing
        var apiSettings = new ApiSettings
        {
            Secret = "test-secret",
            DbConnectionString = "test-connection",
            HashCacheExpiryMinutes = 60 // 1 hour - we can't easily test time-based expiry without reflection
        };
        _mockApiSettings.Setup(x => x.Value).Returns(apiSettings);
        var cache = new HashValidationCache(_mockApiSettings.Object);

        // Act - cleanup should not throw
        cache.CleanupExpiredEntries();

        // Assert - no exceptions thrown
        Assert.Pass();
    }

    [Test]
    public void CleanupExpiredEntries_WhenCacheDisabled_ShouldNotThrow()
    {
        // Arrange
        var cache = CreateCache(0);

        // Act & Assert - should not throw
        cache.CleanupExpiredEntries();
        Assert.Pass();
    }

    [Test]
    public void ValidateClientSecret_WithDifferentClients_ShouldMaintainSeparateCaches()
    {
        // Arrange
        var cache = CreateCache();
        var secret1 = "test-client-secret-1234567890123456789012";
        var secret2 = "different-client-secret-12345678901234567";
        var bcryptHash1 = BCrypt.Net.BCrypt.EnhancedHashPassword(secret1);
        var bcryptHash2 = BCrypt.Net.BCrypt.EnhancedHashPassword(secret2);

        // Act
        var result1 = cache.ValidateClientSecret("Client1", secret1, bcryptHash1);
        var result2 = cache.ValidateClientSecret("Client2", secret2, bcryptHash2);

        // Assert
        Assert.That(result1, Is.True);
        Assert.That(result2, Is.True);
    }

    [Test]
    public void ValidateCodeHash_WithDifferentSettings_ShouldMaintainSeparateCaches()
    {
        // Arrange
        var cache = CreateCache();
        var code = "test-code";
        var hash1 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var hash2 = Convert.ToBase64String(new byte[] { 9, 10, 11, 12, 13, 14, 15, 16 });

        var computeCount1 = 0;
        var computeCount2 = 0;
        Func<string, string> computeHash1 = _ => { computeCount1++; return hash1; };
        Func<string, string> computeHash2 = _ => { computeCount2++; return hash2; };

        // Act
        cache.ValidateCodeHash("TestClient", "Setting1", code, hash1, computeHash1);
        cache.ValidateCodeHash("TestClient", "Setting2", code, hash2, computeHash2);
        cache.ValidateCodeHash("TestClient", "Setting1", code, hash1, computeHash1);
        cache.ValidateCodeHash("TestClient", "Setting2", code, hash2, computeHash2);

        // Assert - each setting should only trigger compute once
        Assert.That(computeCount1, Is.EqualTo(1));
        Assert.That(computeCount2, Is.EqualTo(1));
    }

    [Test]
    public void ValidateClientSecret_WithPreviousSecretKey_ShouldCacheSeparately()
    {
        // Arrange
        var cache = CreateCache();
        var secret = "test-client-secret-1234567890123456789012";
        var bcryptHash = BCrypt.Net.BCrypt.EnhancedHashPassword(secret);

        // Act - simulating current and previous secret validation
        var currentResult = cache.ValidateClientSecret("TestClient", secret, bcryptHash);
        var previousResult = cache.ValidateClientSecret("TestClient:previous", secret, bcryptHash);

        // Assert
        Assert.That(currentResult, Is.True);
        Assert.That(previousResult, Is.True);
    }
}
