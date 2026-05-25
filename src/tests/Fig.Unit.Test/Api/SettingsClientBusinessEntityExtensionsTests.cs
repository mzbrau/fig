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
            .Setup(a => a.DecryptWithValidation("valid", It.IsAny<Func<string, bool>>(), false))
            .Returns(validJson);
        encryptionService
            .Setup(a => a.DecryptWithValidation("invalid", It.IsAny<Func<string, bool>>(), false))
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
}
