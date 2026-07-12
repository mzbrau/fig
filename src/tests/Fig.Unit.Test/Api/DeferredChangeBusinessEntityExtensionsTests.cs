using System;
using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Contracts.Scheduling;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class DeferredChangeBusinessEntityExtensionsTests
{
    [Test]
    public void SerializeAndDecrypt_PreservesScheduleApplyAndRevertTimes()
    {
        var applyAt = DateTime.UtcNow.AddMinutes(3);
        var revertAt = applyAt.AddMinutes(8);
        var changeSet = new SettingValueUpdatesDataContract(
            [new SettingDataContract("AStringSetting", new StringSettingDataContract("temporary"))],
            "scheduled change",
            new ScheduleDataContract(applyAt, revertAt));

        var encryptionService = new Mock<IEncryptionService>();
        encryptionService
            .Setup(a => a.Encrypt(It.IsAny<string>()))
            .Returns<string>(value => value);
        encryptionService
            .Setup(a => a.DecryptWithValidation(It.IsAny<string>(), It.IsAny<Func<string, bool>>(), false, ValidatedDecryptionMode.Strict))
            .Returns<string, Func<string, bool>, bool, ValidatedDecryptionMode>((value, validator, _, _) =>
            {
                Assert.That(validator(value), Is.True);
                return value;
            });

        var entity = new DeferredChangeBusinessEntity
        {
            ClientName = "Client",
            ExecuteAtUtc = applyAt,
            ChangeSet = changeSet
        };

        entity.SerializeAndEncrypt(encryptionService.Object);
        entity.ChangeSet = null;
        entity.DeserializeAndDecrypt(encryptionService.Object);

        Assert.That(entity.ChangeSet, Is.Not.Null);
        Assert.That(entity.ChangeSet!.Schedule, Is.Not.Null);
        Assert.That(entity.ChangeSet.Schedule!.ApplyAtUtc, Is.EqualTo(applyAt).Within(1).Seconds);
        Assert.That(entity.ChangeSet.Schedule.RevertAtUtc, Is.EqualTo(revertAt).Within(1).Seconds);
        Assert.That(entity.ChangeSet.ValueUpdates, Is.Not.Null);
    }
}
