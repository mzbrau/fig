using Fig.Client.Migration;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingMigrations;
using Fig.Contracts.Settings;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class MigrateFromMigrationConverterTests
{
    [Test]
    public void Convert_WithScalarMigrationMethod_ShouldReturnConvertedValue()
    {
        var settings = CreateSettings(
            typeof(MigrateFromMigrationConverterTests).GetMethod(nameof(ConvertSeconds))!,
            typeof(TimeSpan));
        var request = new SettingMigrationRequestDataContract(
            "TimeoutSeconds",
            "Timeout",
            null,
            typeof(int),
            typeof(TimeSpan),
            new IntSettingDataContract(15),
            false,
            false,
            "fingerprint");

        var result = new MigrateFromMigrationConverter().Convert(settings, [request]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].MigratedValue, Is.TypeOf<TimeSpanSettingDataContract>());
        Assert.That(result[0].MigratedValue!.GetValue(), Is.EqualTo(TimeSpan.FromSeconds(15)));
    }

    [Test]
    public void Convert_WithJsonSourceAndComplexParameter_ShouldDeserializeBeforeInvokingMethod()
    {
        var settings = CreateSettings(
            typeof(MigrateFromMigrationConverterTests).GetMethod(nameof(ExtractName))!,
            typeof(string));
        var request = new SettingMigrationRequestDataContract(
            "OldJson",
            "NewName",
            null,
            typeof(string),
            typeof(string),
            new JsonSettingDataContract("""{"Name":"legacy"}"""),
            false,
            false,
            "fingerprint");

        var result = new MigrateFromMigrationConverter().Convert(settings, [request]);

        Assert.That(result[0].MigratedValue, Is.TypeOf<StringSettingDataContract>());
        Assert.That(result[0].MigratedValue!.GetValue(), Is.EqualTo("legacy"));
    }

    [Test]
    public void Convert_WithSecretSourceAndNonSecretTarget_ShouldThrow()
    {
        var settings = CreateSettings(
            typeof(MigrateFromMigrationConverterTests).GetMethod(nameof(Copy))!,
            typeof(string));
        var request = new SettingMigrationRequestDataContract(
            "OldSecret",
            "NewSetting",
            null,
            typeof(string),
            typeof(string),
            new StringSettingDataContract("secret"),
            true,
            false,
            "fingerprint");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new MigrateFromMigrationConverter().Convert(settings, [request]));

        Assert.That(ex!.Message, Does.Contain("not allowed"));
    }

    public static TimeSpan ConvertSeconds(int seconds) => TimeSpan.FromSeconds(seconds);

    public static string ExtractName(LegacyValue value) => value.Name;

    public static string Copy(string value) => value;

    private static SettingsClientDefinitionDataContract CreateSettings(System.Reflection.MethodInfo method, Type targetType)
    {
        var (targetName, sourceName) = method.Name switch
        {
            nameof(ExtractName) => ("NewName", "OldJson"),
            nameof(Copy) => ("NewSetting", "OldSecret"),
            _ => ("Timeout", "TimeoutSeconds")
        };

        var target = new SettingDefinitionDataContract(
            targetName,
            "Target setting",
            valueType: targetType,
            migrateFrom: sourceName,
            migrateFromMigrationMethod: method.Name)
        {
            MigrateFromMigrationMethodInfo = method
        };

        return new SettingsClientDefinitionDataContract(
            "TestClient",
            "Test client",
            null,
            false,
            [target],
            []);
    }

    public class LegacyValue
    {
        public string Name { get; set; } = string.Empty;
    }
}
