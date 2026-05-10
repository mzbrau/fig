using System.Reflection;
using Fig.Web.Models.Setting;
using Fig.Web.Pages.Setting;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class SettingIconsTests
{
    [Test]
    public void GetGroupManagedTooltip_ShowsSpecificGroupName_WhenGroupIsAvailable()
    {
        var component = new SettingIcons
        {
            Setting = CreateMockSetting("Shared Messaging")
        };

        var tooltip = InvokeGroupManagedTooltip(component);

        Assert.That(tooltip, Is.EqualTo("Managed by setting group Shared Messaging"));
    }

    [Test]
    public void GetGroupManagedTooltip_ShowsGenericText_WhenGroupIsMissing()
    {
        var component = new SettingIcons
        {
            Setting = CreateMockSetting(null)
        };

        var tooltip = InvokeGroupManagedTooltip(component);

        Assert.That(tooltip, Is.EqualTo("Managed by a setting group"));
    }

    private static string InvokeGroupManagedTooltip(SettingIcons component)
    {
        var method = typeof(SettingIcons).GetMethod("GetGroupManagedTooltip", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);

        return (string)method!.Invoke(component, null)!;
    }

    private static ISetting CreateMockSetting(string? groupName)
    {
        var mock = new Mock<ISetting>();
        mock.SetupGet(s => s.Group).Returns(groupName);
        return mock.Object;
    }
}
