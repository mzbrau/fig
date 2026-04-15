using Fig.Api;
using Fig.Api.Services;
using Fig.Common.NetStandard.Cryptography;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class EncryptionServiceTests
{
    private Mock<ICryptography> _cryptographyMock = null!;
    private Mock<IOptionsMonitor<ApiSettings>> _apiSettingsMock = null!;
    private EncryptionService _encryptionService = null!;

    [SetUp]
    public void Setup()
    {
        _cryptographyMock = new Mock<ICryptography>();
        _apiSettingsMock = new Mock<IOptionsMonitor<ApiSettings>>();
        _apiSettingsMock.SetupGet(a => a.CurrentValue).Returns(new ApiSettings
        {
            Secret = "current-secret",
            PreviousSecret = "previous-secret",
            DbConnectionString = "Data Source=fig.db;Version=3;New=True"
        });

        _encryptionService = new EncryptionService(_cryptographyMock.Object, _apiSettingsMock.Object);
    }

    [Test]
    public void DecryptForImport_WithoutCustomKey_UsesConfiguredServerSecret()
    {
        _cryptographyMock
            .Setup(a => a.Decrypt("current-secret", "cipher-text", "previous-secret", false))
            .Returns("plain-text");

        var result = _encryptionService.DecryptForImport("cipher-text");

        Assert.That(result, Is.EqualTo("plain-text"));
        _cryptographyMock.Verify(a => a.Decrypt("current-secret", "cipher-text", "previous-secret", false), Times.Once);
        _cryptographyMock.Verify(a => a.Decrypt("custom-key", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public void DecryptForImport_WithCustomKey_UsesProvidedKeyInsteadOfCurrentServerSecret()
    {
        _cryptographyMock
            .Setup(a => a.Decrypt("custom-key", "cipher-text", null, false))
            .Returns("plain-text");

        var result = _encryptionService.DecryptForImport("cipher-text", "custom-key");

        Assert.That(result, Is.EqualTo("plain-text"));
        _cryptographyMock.Verify(a => a.Decrypt("custom-key", "cipher-text", null, false), Times.Once);
        _cryptographyMock.Verify(a => a.Decrypt("current-secret", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public void DecryptForImport_WithWhitespaceKey_UsesConfiguredServerSecret()
    {
        _cryptographyMock
            .Setup(a => a.Decrypt("current-secret", "cipher-text", "previous-secret", false))
            .Returns("plain-text");

        var result = _encryptionService.DecryptForImport("cipher-text", "   ");

        Assert.That(result, Is.EqualTo("plain-text"));
        _cryptographyMock.Verify(a => a.Decrypt("current-secret", "cipher-text", "previous-secret", false), Times.Once);
        _cryptographyMock.Verify(a => a.Decrypt("   ", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Never);
    }
}
