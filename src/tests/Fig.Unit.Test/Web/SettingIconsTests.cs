using Fig.Web.Models.Setting;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class SettingIconsTests
{
    [Test]
    public void GroupIcon_UsesSharedHubIcon()
    {
        Assert.That(SettingGroupVisuals.Icon, Is.EqualTo("hub"));
    }

    [Test]
    public void GroupLabel_UsesSharedSettingGroupText()
    {
        Assert.That(SettingGroupVisuals.Label, Is.EqualTo("Setting Group"));
    }

    [Test]
    public void GetManagedByTooltip_ShowsSpecificGroupName_WhenGroupIsAvailable()
    {
        var tooltip = SettingGroupVisuals.GetManagedByTooltip("Shared Messaging");
        
        Assert.That(tooltip, Is.EqualTo("Managed by setting group Shared Messaging"));
    }

    [Test]
    public void GetManagedByTooltip_ShowsGenericText_WhenGroupIsMissing()
    {
        var tooltip = SettingGroupVisuals.GetManagedByTooltip(null);
        
        Assert.That(tooltip, Is.EqualTo("Managed by a setting group"));
    }
}
