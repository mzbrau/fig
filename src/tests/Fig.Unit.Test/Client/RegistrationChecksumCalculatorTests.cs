using Fig.Client.RegistrationChecksum;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class RegistrationChecksumCalculatorTests
{
    [Test]
    public void Compute_SameDefinition_ProducesSameChecksum()
    {
        var definition = CreateDefinition("Description A", "SettingA", "default-a");

        var first = RegistrationChecksumCalculator.Compute(definition);
        var second = RegistrationChecksumCalculator.Compute(definition);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void Compute_DifferentDescription_ProducesDifferentChecksum()
    {
        var definitionA = CreateDefinition("Description A", "SettingA", "default-a");
        var definitionB = CreateDefinition("Description B", "SettingA", "default-a");

        Assert.That(
            RegistrationChecksumCalculator.Compute(definitionB),
            Is.Not.EqualTo(RegistrationChecksumCalculator.Compute(definitionA)));
    }

    [Test]
    public void Compute_DifferentDefaultValue_ProducesDifferentChecksum()
    {
        var definitionA = CreateDefinition("Description", "SettingA", "default-a");
        var definitionB = CreateDefinition("Description", "SettingA", "default-b");

        Assert.That(
            RegistrationChecksumCalculator.Compute(definitionB),
            Is.Not.EqualTo(RegistrationChecksumCalculator.Compute(definitionA)));
    }

    [Test]
    public void Compute_SettingsOrder_DoesNotAffectChecksum()
    {
        var definitionA = new SettingsClientDefinitionDataContract(
            "Client",
            "Description",
            null,
            false,
            [
                CreateSetting("B", "b", 1),
                CreateSetting("A", "a", 2)
            ],
            []);

        var definitionB = new SettingsClientDefinitionDataContract(
            "Client",
            "Description",
            null,
            false,
            [
                CreateSetting("A", "a", 2),
                CreateSetting("B", "b", 1)
            ],
            []);

        Assert.That(
            RegistrationChecksumCalculator.Compute(definitionB),
            Is.EqualTo(RegistrationChecksumCalculator.Compute(definitionA)));
    }
    
    [Test]
    public void Compute_DisplayOrder_DoesAffectChecksum()
    {
        var definitionA = new SettingsClientDefinitionDataContract(
            "Client",
            "Description",
            null,
            false,
            [
                CreateSetting("B", "b", 1),
                CreateSetting("A", "a", 2)
            ],
            []);

        var definitionB = new SettingsClientDefinitionDataContract(
            "Client",
            "Description",
            null,
            false,
            [
                CreateSetting("A", "a", 1),
                CreateSetting("B", "b", 2)
            ],
            []);

        Assert.That(
            RegistrationChecksumCalculator.Compute(definitionB),
            Is.Not.EqualTo(RegistrationChecksumCalculator.Compute(definitionA)));
    }

    private static SettingsClientDefinitionDataContract CreateDefinition(
        string description,
        string settingName,
        string defaultValue) =>
        new(
            "Client",
            description,
            null,
            false,
            [CreateSetting(settingName, defaultValue, 1)],
            []);

    private static SettingDefinitionDataContract CreateSetting(string name, string defaultValue, int displayOrder) =>
        new(
            name,
            "Setting description",
            defaultValue: new StringSettingDataContract(defaultValue),
            displayOrder: displayOrder);
}
