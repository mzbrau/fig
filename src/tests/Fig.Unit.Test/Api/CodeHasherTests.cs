using System;
using Fig.Api;
using Fig.Api.Validators;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class CodeHasherTests
{
    private Mock<IOptions<ApiSettings>> _mockApiSettings = null!;
    private CodeHasher _codeHasher = null!;

    [SetUp]
    public void SetUp()
    {
        _mockApiSettings = new Mock<IOptions<ApiSettings>>();
        var apiSettings = new ApiSettings
        {
            Secret = "test-secret-for-hashing",
            DbConnectionString = "test-connection"
        };
        _mockApiSettings.Setup(x => x.Value).Returns(apiSettings);
        _codeHasher = new CodeHasher(_mockApiSettings.Object);
    }

    [Test]
    public void GetHash_WithValidCode_ShouldReturnHash()
    {
        // Arrange
        var code = "test-code";

        // Act
        var hash = _codeHasher.GetHash(code);

        // Assert
        Assert.That(hash, Is.Not.Null);
        Assert.That(hash, Is.Not.Empty);
        Assert.That(hash.StartsWith("$2"), Is.True, "Hash should start with BCrypt prefix");
    }

    [Test]
    public void GetHash_WithNullCode_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _codeHasher.GetHash(null!));
        Assert.That(ex!.Message, Does.Contain("Code cannot be null or empty"));
        Assert.That(ex.ParamName, Is.EqualTo("code"));
    }

    [Test]
    public void GetHash_WithEmptyCode_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _codeHasher.GetHash(string.Empty));
        Assert.That(ex!.Message, Does.Contain("Code cannot be null or empty"));
        Assert.That(ex.ParamName, Is.EqualTo("code"));
    }

    [Test]
    public void GetHash_WithWhitespaceCode_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _codeHasher.GetHash("   "));
        Assert.That(ex!.Message, Does.Contain("Code cannot be null or empty"));
        Assert.That(ex.ParamName, Is.EqualTo("code"));
    }

    [Test]
    public void GetHash_WithNullSecret_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var apiSettings = new ApiSettings
        {
            Secret = null!,
            DbConnectionString = "test-connection"
        };
        _mockApiSettings.Setup(x => x.Value).Returns(apiSettings);
        var codeHasher = new CodeHasher(_mockApiSettings.Object);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => codeHasher.GetHash("test-code"));
        Assert.That(ex!.Message, Does.Contain("API secret is not configured"));
    }

    [Test]
    public void IsValid_WithValidCodeAndHash_ShouldReturnTrue()
    {
        // Arrange
        var code = "test-code";
        var hash = _codeHasher.GetHash(code);

        // Act
        var result = _codeHasher.IsValid(hash, code);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsValid_WithInvalidCode_ShouldReturnFalse()
    {
        // Arrange
        var code = "test-code";
        var hash = _codeHasher.GetHash(code);

        // Act
        var result = _codeHasher.IsValid(hash, "wrong-code");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_WithNullCode_ShouldReturnFalse()
    {
        // Arrange
        var code = "test-code";
        var hash = _codeHasher.GetHash(code);

        // Act
        var result = _codeHasher.IsValid(hash, null);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_WithEmptyCode_ShouldReturnFalse()
    {
        // Arrange
        var code = "test-code";
        var hash = _codeHasher.GetHash(code);

        // Act
        var result = _codeHasher.IsValid(hash, string.Empty);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_WithWhitespaceCode_ShouldReturnFalse()
    {
        // Arrange
        var code = "test-code";
        var hash = _codeHasher.GetHash(code);

        // Act
        var result = _codeHasher.IsValid(hash, "   ");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_WithNullHash_ShouldReturnFalse()
    {
        // Act
        var result = _codeHasher.IsValid(null!, "test-code");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_WithEmptyHash_ShouldReturnFalse()
    {
        // Act
        var result = _codeHasher.IsValid(string.Empty, "test-code");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_WithWhitespaceHash_ShouldReturnFalse()
    {
        // Act
        var result = _codeHasher.IsValid("   ", "test-code");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_WithNullSecret_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var apiSettings = new ApiSettings
        {
            Secret = "test-secret",
            DbConnectionString = "test-connection"
        };
        _mockApiSettings.Setup(x => x.Value).Returns(apiSettings);
        var codeHasher = new CodeHasher(_mockApiSettings.Object);
        var hash = codeHasher.GetHash("test-code");

        // Update to null secret
        apiSettings.Secret = null!;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => codeHasher.IsValid(hash, "test-code"));
        Assert.That(ex!.Message, Does.Contain("API secret is not configured"));
    }
}
