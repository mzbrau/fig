using System.Dynamic;
using Fig.Web.Dashboards.Components;
using Fig.Web.Dashboards.Runtime;
using Fig.Web.Scripting;
using NUnit.Framework;

namespace Fig.Unit.Test.Web.Dashboards;

[TestFixture]
public class DashboardComponentDataBinderTests
{
    private readonly DashboardTransformEngine _engine = new(new JintEngineFactory());

    private static DashboardFigRoot SampleFig()
    {
        return new DashboardFigRoot
        {
            clients = new DashboardJsArray(new object?[]
            {
                new DashboardClientJsModel { name = "AspNetApi" },
                new DashboardClientJsModel { name = "Yarp Example" }
            }),
            runSessions = new DashboardJsArray(new object?[]
            {
                new DashboardRunSessionJsModel { name = "AspNetApi", hostname = "host-a" },
                new DashboardRunSessionJsModel { name = "AspNetApi", hostname = "host-b" },
                new DashboardRunSessionJsModel { name = "Yarp Example", hostname = "host-c" }
            })
        };
    }

    [Test]
    public void ShallBindKpiFromJintObjectLiteral()
    {
        var data = _engine.ExecuteScript(
            "return { value: fig.runSessions.length, label: 'Connected run sessions' };",
            SampleFig());

        Assert.That(data, Is.InstanceOf<ExpandoObject>());

        var kpi = DashboardComponentDataBinder.ToKpi(data);
        Assert.That(Convert.ToInt32(kpi.Value), Is.EqualTo(3));
        Assert.That(kpi.Label, Is.EqualTo("Connected run sessions"));
    }

    [Test]
    public void ShallBindTextFromJintObjectLiteral()
    {
        var data = _engine.ExecuteScript(
            "return { text: 'Hello from Fig', variant: 'heading' };",
            SampleFig());

        Assert.That(data, Is.InstanceOf<ExpandoObject>());

        var text = DashboardComponentDataBinder.ToText(data);
        Assert.That(text.Text, Is.EqualTo("Hello from Fig"));
        Assert.That(text.Variant, Is.EqualTo("heading"));
        Assert.That(text.Text, Does.Not.Contain("ExpandoObject"));
    }

    [Test]
    public void ShallBindChartPointsFromJintMappedObjects()
    {
        var data = _engine.ExecuteScript(
            """
            return fig.runSessions
                .groupBy(s => s.name)
                .map(g => ({ label: g.key, value: g.items.length }));
            """,
            SampleFig());

        Assert.That(data, Is.InstanceOf<DashboardJsArray>());

        var points = DashboardComponentDataBinder.ToChartPoints(data);
        Assert.That(points, Has.Count.EqualTo(2));
        Assert.That(points.Select(p => p.Label), Is.EquivalentTo(new[] { "AspNetApi", "Yarp Example" }));
        Assert.That(points.Single(p => p.Label == "AspNetApi").Value, Is.EqualTo(2));
        Assert.That(points.Single(p => p.Label == "Yarp Example").Value, Is.EqualTo(1));
        Assert.That(points.Select(p => p.Label), Has.None.Contain("Key"));
    }

    [Test]
    public void ShallBindListItemsFromJintMappedObjects()
    {
        var data = _engine.ExecuteScript(
            "return fig.runSessions.map(s => ({ text: s.name, secondary: s.hostname }));",
            SampleFig());

        Assert.That(data, Is.InstanceOf<DashboardJsArray>());

        var list = DashboardComponentDataBinder.ToList(data);
        Assert.That(list.Items, Has.Count.EqualTo(3));
        Assert.That(list.Items[0].Text, Is.EqualTo("AspNetApi"));
        Assert.That(list.Items[0].Secondary, Is.EqualTo("host-a"));
        Assert.That(list.Items.Select(i => i.Text), Has.None.Contain("Key"));
    }

    [Test]
    public void ShallBindKeyValueFromDefaultStyleScript()
    {
        var data = _engine.ExecuteScript(
            "return [{ key: 'clients', value: fig.clients.length }, { key: 'runSessions', value: fig.runSessions.length }];",
            SampleFig());

        var keyValue = DashboardComponentDataBinder.ToKeyValue(data);
        Assert.That(keyValue.Items, Has.Count.EqualTo(2));
        Assert.That(keyValue.Items[0].Key, Is.EqualTo("clients"));
        Assert.That(Convert.ToInt32(keyValue.Items[0].Value), Is.EqualTo(2));
        Assert.That(keyValue.Items[1].Key, Is.EqualTo("runSessions"));
        Assert.That(Convert.ToInt32(keyValue.Items[1].Value), Is.EqualTo(3));
        Assert.That(keyValue.StatusIcon, Is.Null);
    }

    [Test]
    public void ShallBindKeyValueStatusFromWrapperAndExcludeReservedKeys()
    {
        var data = _engine.ExecuteScript(
            """
            return {
              statusIcon: 'check',
              statusColor: '#22c55e',
              items: [
                { key: 'clients', value: fig.clients.length },
                { key: 'runSessions', value: fig.runSessions.length }
              ]
            };
            """,
            SampleFig());

        var keyValue = DashboardComponentDataBinder.ToKeyValue(data);
        Assert.That(keyValue.StatusIcon, Is.EqualTo("check"));
        Assert.That(keyValue.StatusColor, Is.EqualTo("#22c55e"));
        Assert.That(keyValue.Items, Has.Count.EqualTo(2));
        Assert.That(keyValue.Items.Select(i => i.Key), Has.None.Contain("statusIcon"));
    }

    [Test]
    public void ShallBindKeyValueStatusFromConfigFallback()
    {
        var data = _engine.ExecuteScript(
            "return [{ key: 'a', value: 1 }];",
            SampleFig());

        var config = new Newtonsoft.Json.Linq.JObject
        {
            ["statusIcon"] = "warning",
            ["statusColor"] = "#f0ad4e"
        };

        var keyValue = DashboardComponentDataBinder.ToKeyValue(data, config);
        Assert.That(keyValue.StatusIcon, Is.EqualTo("warning"));
        Assert.That(keyValue.StatusColor, Is.EqualTo("#f0ad4e"));
        Assert.That(keyValue.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public void ShallExcludeReservedKeysFromFlatObjectKeyValue()
    {
        var data = _engine.ExecuteScript(
            "return { statusIcon: 'info', statusColor: '#5bc0de', clients: 2, runSessions: 3 };",
            SampleFig());

        var keyValue = DashboardComponentDataBinder.ToKeyValue(data);
        Assert.That(keyValue.StatusIcon, Is.EqualTo("info"));
        Assert.That(keyValue.StatusColor, Is.EqualTo("#5bc0de"));
        Assert.That(keyValue.Items.Select(i => i.Key), Is.EquivalentTo(new[] { "clients", "runSessions" }));
    }

    [Test]
    public void ShallReadLegendPositionFromConfig()
    {
        Assert.That(
            DashboardComponentDataBinder.ReadLegendPosition(null),
            Is.EqualTo(Radzen.Blazor.LegendPosition.Right));
        Assert.That(
            DashboardComponentDataBinder.ReadLegendPosition(new Newtonsoft.Json.Linq.JObject { ["legendPosition"] = "bottom" }),
            Is.EqualTo(Radzen.Blazor.LegendPosition.Bottom));
        Assert.That(
            DashboardComponentDataBinder.ReadLegendPositionCss(new Newtonsoft.Json.Linq.JObject { ["LegendPosition"] = "BOTTOM" }),
            Is.EqualTo("bottom"));
        Assert.That(
            DashboardComponentDataBinder.ReadLegendVisible(new Newtonsoft.Json.Linq.JObject { ["legendPosition"] = "hidden" }),
            Is.False);
        Assert.That(
            DashboardComponentDataBinder.ReadLegendPositionCss(new Newtonsoft.Json.Linq.JObject { ["legendPosition"] = "hidden" }),
            Is.EqualTo("hidden"));
        Assert.That(
            DashboardComponentDataBinder.ReadLegendVisible(new Newtonsoft.Json.Linq.JObject { ["legendPosition"] = "right" }),
            Is.True);
    }

    [Test]
    public void ShallBindExpandoObjectDirectlyWithoutKeyValuePairArray()
    {
        dynamic expando = new ExpandoObject();
        expando.text = "Direct";
        expando.variant = "body";

        var text = DashboardComponentDataBinder.ToText((object)expando);
        Assert.That(text.Text, Is.EqualTo("Direct"));
        Assert.That(text.Variant, Is.EqualTo("body"));
    }
}
