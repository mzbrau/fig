using System;
using System.Linq;
using System.Security.Cryptography;
using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Common.NetStandard.Json;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class SettingsClientBusinessEntityExtensionsTests
{
    [Test]
    public void DeserializeAndDecryptBestEffort_WhenOneSettingCannotBeDecrypted_OmitsOnlyFailedSetting()
    {
        var validJson = JsonConvert.SerializeObject(new StringSettingBusinessEntity("good"), JsonSettings.FigDefault);
        var encryptionService = new Mock<IEncryptionService>();
        encryptionService
            .Setup(a => a.DecryptWithValidation("valid",
                It.IsAny<Func<string, bool>>(),
                false,
                ValidatedDecryptionMode.Strict))
            .Callback<string?, Func<string, bool>, bool, ValidatedDecryptionMode>((_, validator, _, _) => validator(validJson))
            .Returns(validJson);
        encryptionService
            .Setup(a => a.DecryptWithValidation("invalid",
                It.IsAny<Func<string, bool>>(),
                false,
                ValidatedDecryptionMode.Strict))
            .Throws(new CryptographicException("Unable to decrypt"));
        var client = new SettingClientBusinessEntity
        {
            Name = "Client",
            Settings =
            {
                new SettingBusinessEntity { Name = "GoodSetting", ValueAsJson = "valid" },
                new SettingBusinessEntity { Name = "BadSetting", ValueAsJson = "invalid" }
            }
        };
        SettingBusinessEntity? failedSetting = null;

        client.DeserializeAndDecryptBestEffort(encryptionService.Object, (setting, _) => failedSetting = setting);

        Assert.That(client.Settings.Select(a => a.Name), Is.EquivalentTo(new[] { "GoodSetting" }));
        Assert.That(client.Settings.Single().Value?.GetValue(), Is.EqualTo("good"));
        Assert.That(failedSetting?.Name, Is.EqualTo("BadSetting"));
    }

    [Test]
    public void DeserializeAndDecrypt_WhenLaterValidationAttemptFails_KeepsValidDecryptedValue()
    {
        var validJson = JsonConvert.SerializeObject(new StringSettingBusinessEntity("good"), JsonSettings.FigDefault);
        var encryptionService = new Mock<IEncryptionService>();
        encryptionService
            .Setup(a => a.DecryptWithValidation("encrypted",
                It.IsAny<Func<string, bool>>(),
                true,
                ValidatedDecryptionMode.Strict))
            .Callback<string?, Func<string, bool>, bool, ValidatedDecryptionMode>((_, validator, _, _) =>
            {
                Assert.That(validator(validJson), Is.True);
                Assert.That(validator("not-json"), Is.False);
            })
            .Returns(validJson);
        var client = new SettingClientBusinessEntity
        {
            Name = "Client",
            Settings =
            {
                new SettingBusinessEntity { Name = "GoodSetting", ValueAsJson = "encrypted" }
            }
        };

        client.DeserializeAndDecrypt(encryptionService.Object, tryFallbackFirst: true);

        Assert.That(client.Settings.Single().Value?.GetValue(), Is.EqualTo("good"));
    }

    [Test]
    public void DeserializeAndDecryptBestEffort_WhenLaterValidationAttemptFails_KeepsSetting()
    {
        var validJson = JsonConvert.SerializeObject(new StringSettingBusinessEntity("good"), JsonSettings.FigDefault);
        var encryptionService = new Mock<IEncryptionService>();
        encryptionService
            .Setup(a => a.DecryptWithValidation("encrypted",
                It.IsAny<Func<string, bool>>(),
                false,
                ValidatedDecryptionMode.Strict))
            .Callback<string?, Func<string, bool>, bool, ValidatedDecryptionMode>((_, validator, _, _) =>
            {
                Assert.That(validator(validJson), Is.True);
                Assert.That(validator("not-json"), Is.False);
            })
            .Returns(validJson);
        var client = new SettingClientBusinessEntity
        {
            Name = "Client",
            Settings =
            {
                new SettingBusinessEntity { Name = "GoodSetting", ValueAsJson = "encrypted" }
            }
        };
        SettingBusinessEntity? failedSetting = null;

        client.DeserializeAndDecryptBestEffort(encryptionService.Object, (setting, _) => failedSetting = setting);

        Assert.That(client.Settings.Select(a => a.Name), Is.EquivalentTo(new[] { "GoodSetting" }));
        Assert.That(client.Settings.Single().Value?.GetValue(), Is.EqualTo("good"));
        Assert.That(failedSetting, Is.Null);
    }

    [Test]
    public void DeserializeAndDecrypt_WhenDecryptedJsonIsNull_IncludesSettingContext()
    {
        var encryptionService = new Mock<IEncryptionService>();
        encryptionService
            .Setup(a => a.DecryptWithValidation("encrypted",
                It.IsAny<Func<string, bool>>(),
                false,
                ValidatedDecryptionMode.Strict))
            .Returns("null");
        var client = new SettingClientBusinessEntity
        {
            Name = "Client",
            Settings =
            {
                new SettingBusinessEntity { Name = "NullSetting", ValueAsJson = "encrypted" }
            }
        };

        var ex = Assert.Throws<JsonSerializationException>(() => client.DeserializeAndDecrypt(encryptionService.Object));

        Assert.That(ex?.Message, Does.Contain("NullSetting"));
        Assert.That(ex?.InnerException?.Message, Does.Contain("Decrypted setting value JSON did not contain a setting value."));
    }
}
