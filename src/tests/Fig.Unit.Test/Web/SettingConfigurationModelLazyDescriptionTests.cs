using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.ExtensionMethods;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class SettingConfigurationModelLazyDescriptionTests
{
    private SettingClientConfigurationModel _parent = null!;

    [SetUp]
    public void SetUp()
    {
        _parent = new SettingClientConfigurationModel(
            "ClientA",
            "desc",
            null,
            false,
            Mock.Of<IScriptRunner>());
        StringExtensionMethods.ResetDescriptionHtmlTiming();
    }

    [TearDown]
    public void TearDown()
    {
        StringExtensionMethods.TakeDescriptionHtmlElapsedMs();
    }

    [Test]
    public void Constructor_DoesNotConvertDescriptionToHtml()
    {
        var setting = CreateSetting("## Heading\n\nSome **bold** text");

        Assert.That(setting.RawDescription, Does.Contain("## Heading"));
        Assert.That(setting.TruncatedDescription, Is.Not.Empty);
        Assert.That(StringExtensionMethods.TakeDescriptionHtmlElapsedMs(), Is.EqualTo(0));
    }

    [Test]
    public void Description_ConvertsMarkdownOnFirstAccess()
    {
        var setting = CreateSetting("## Heading\n\nSome **bold** text");

        var html = setting.Description.ToString();

        Assert.That(html, Does.Contain("<h2"));
        Assert.That(html, Does.Contain("<strong>bold</strong>"));
        Assert.That(StringExtensionMethods.TakeDescriptionHtmlElapsedMs(), Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Description_CachesHtmlAfterFirstAccess()
    {
        var setting = CreateSetting("## Once");

        _ = setting.Description;
        StringExtensionMethods.ResetDescriptionHtmlTiming();
        _ = setting.Description;

        Assert.That(StringExtensionMethods.TakeDescriptionHtmlElapsedMs(), Is.EqualTo(0));
    }

    [Test]
    public void SetDescription_InvalidatesCachedHtml()
    {
        var setting = CreateSetting("## First");
        _ = setting.Description;

        setting.SetDescription("## Second");
        var html = setting.Description.ToString();

        Assert.That(html, Does.Contain("Second"));
        Assert.That(html, Does.Not.Contain("First"));
    }

    [Test]
    public void TruncatedDescription_AvailableWithoutHtmlConversion()
    {
        var setting = CreateSetting("Plain description without markup markers");

        Assert.That(setting.TruncatedDescription, Does.Contain("Plain description"));
        Assert.That(StringExtensionMethods.TakeDescriptionHtmlElapsedMs(), Is.EqualTo(0));
    }

    private StringSettingConfigurationModel CreateSetting(string description)
    {
        return new StringSettingConfigurationModel(
            new SettingDefinitionDataContract(
                "MySetting",
                description,
                new StringSettingDataContract("value"),
                false,
                typeof(string)),
            _parent,
            new SettingPresentation(false));
    }
}
