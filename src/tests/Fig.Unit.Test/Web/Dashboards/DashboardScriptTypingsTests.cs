using Fig.Web.Dashboards.Runtime;
using Fig.Web.Dashboards.Scripting;
using NUnit.Framework;

namespace Fig.Unit.Test.Web.Dashboards;

[TestFixture]
public class DashboardScriptTypingsTests
{
    [Test]
    public void Build_IncludesFigHelpersAndFluentApis()
    {
        var libs = DashboardScriptTypings.Build("kpi");
        var ambient = libs.Single(l => l.FilePath == DashboardScriptTypings.AmbientLibPath).Content;

        Assert.That(ambient, Does.Contain("declare const fig"));
        Assert.That(ambient, Does.Contain("runSessions"));
        Assert.That(ambient, Does.Contain("clients"));
        Assert.That(ambient, Does.Contain("declare const helpers"));
        Assert.That(ambient, Does.Not.Contain("declare const transforms"));
        Assert.That(ambient, Does.Contain("groupBy"));
        Assert.That(ambient, Does.Contain("filter"));
        Assert.That(ambient, Does.Contain("toArray"));
        Assert.That(ambient, Does.Contain("customProperties"));
        Assert.That(ambient, Does.Contain("settings"));
    }

    [Test]
    public void Build_IncludesExpectedResultForKpiStatusFields()
    {
        var expected = DashboardScriptTypings.Build("kpi")
            .Single(l => l.FilePath == DashboardScriptTypings.ExpectedLibPath).Content;

        Assert.That(expected, Does.Contain("numerator"));
        Assert.That(expected, Does.Contain("denominator"));
        Assert.That(expected, Does.Contain("subtitle"));
        Assert.That(expected, Does.Contain("icon"));
    }

    [Test]
    public void Build_IncludesExpectedResultForCards()
    {
        var expected = DashboardScriptTypings.Build("cards")
            .Single(l => l.FilePath == DashboardScriptTypings.ExpectedLibPath).Content;

        Assert.That(expected, Does.Contain("title"));
        Assert.That(expected, Does.Contain("rows"));
        Assert.That(expected, Does.Contain("variant"));
    }

    [Test]
    public void Build_IncludesExpectedResultForKeyValue()
    {
        var libs = DashboardScriptTypings.Build("keyValue");
        var expected = libs.Single(l => l.FilePath == DashboardScriptTypings.ExpectedLibPath).Content;

        Assert.That(expected, Does.Contain("ExpectedScriptResult"));
        Assert.That(expected, Does.Contain("statusIcon"));
        Assert.That(expected, Does.Contain("statusColor"));
        Assert.That(expected, Does.Contain("items"));
    }

    [Test]
    public void Build_IncludesExpectedResultForCharts()
    {
        var bar = DashboardScriptTypings.Build("bar")
            .Single(l => l.FilePath == DashboardScriptTypings.ExpectedLibPath).Content;
        var donut = DashboardScriptTypings.Build("donut")
            .Single(l => l.FilePath == DashboardScriptTypings.ExpectedLibPath).Content;

        Assert.That(bar, Does.Contain("label"));
        Assert.That(bar, Does.Contain("value"));
        Assert.That(donut, Does.Contain("label"));
        Assert.That(donut, Does.Contain("value"));
    }

    [Test]
    public void Build_InjectsLiveSettingAndCustomPropertyKeys()
    {
        var fig = new DashboardFigRoot
        {
            clients = new DashboardJsArray(
            [
                new DashboardClientJsModel
                {
                    name = "App",
                    settings = new Dictionary<string, object?>
                    {
                        ["MySetting"] = "value",
                        ["OtherSetting"] = 1
                    }
                }
            ]),
            runSessions = new DashboardJsArray(
            [
                new DashboardRunSessionJsModel
                {
                    name = "App",
                    customProperties = new Dictionary<string, object?>
                    {
                        ["Region"] = "eu",
                        ["Tier"] = "prod"
                    }
                }
            ])
        };

        var libs = DashboardScriptTypings.Build("list", fig);
        var dynamic = libs.Single(l => l.FilePath == DashboardScriptTypings.DynamicLibPath).Content;

        Assert.That(dynamic, Does.Contain("MySetting"));
        Assert.That(dynamic, Does.Contain("OtherSetting"));
        Assert.That(dynamic, Does.Contain("Region"));
        Assert.That(dynamic, Does.Contain("Tier"));
        Assert.That(dynamic, Does.Not.Contain("NamedTransformId"));
    }

    [Test]
    public void Build_ReturnsThreeLibs()
    {
        var libs = DashboardScriptTypings.Build("text");
        Assert.That(libs, Has.Count.EqualTo(3));
        Assert.That(libs.Select(l => l.FilePath), Does.Contain(DashboardScriptTypings.AmbientLibPath));
        Assert.That(libs.Select(l => l.FilePath), Does.Contain(DashboardScriptTypings.DynamicLibPath));
        Assert.That(libs.Select(l => l.FilePath), Does.Contain(DashboardScriptTypings.ExpectedLibPath));
    }
}
