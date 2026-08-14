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
    public void ShallBindKpiStatusCardFields()
    {
        var data = _engine.ExecuteScript(
            """
            return {
              numerator: 2,
              denominator: 3,
              label: 'AspNetApi replicas',
              subtitle: '2 of 3 running',
              variant: 'warning',
              icon: 'warning'
            };
            """,
            SampleFig());

        var kpi = DashboardComponentDataBinder.ToKpi(data);
        Assert.That(Convert.ToInt32(kpi.Numerator), Is.EqualTo(2));
        Assert.That(Convert.ToInt32(kpi.Denominator), Is.EqualTo(3));
        Assert.That(kpi.Label, Is.EqualTo("AspNetApi replicas"));
        Assert.That(kpi.Subtitle, Is.EqualTo("2 of 3 running"));
        Assert.That(kpi.Variant, Is.EqualTo("warning"));
        Assert.That(kpi.Icon, Is.EqualTo("warning"));
    }

    [Test]
    public void ShallBindCardsFromJintMappedObjects()
    {
        var data = _engine.ExecuteScript(
            """
            const expected = 2;
            return fig.clients.map(c => {
              const sessions = fig.runSessions.filter(s => s.name === c.name);
              return {
                title: c.name,
                value: sessions.length + '/' + expected,
                variant: sessions.length >= expected ? 'success' : 'warning',
                rows: [{ key: 'Sessions', value: sessions.length }]
              };
            });
            """,
            SampleFig());

        var cards = DashboardComponentDataBinder.ToCards(data);
        Assert.That(cards.Cards, Has.Count.EqualTo(2));

        var aspNet = cards.Cards.Single(c => c.Title == "AspNetApi");
        Assert.That(aspNet.Value?.ToString(), Is.EqualTo("2/2"));
        Assert.That(aspNet.Variant, Is.EqualTo("success"));
        Assert.That(aspNet.Rows, Has.Count.EqualTo(1));
        Assert.That(aspNet.Rows[0].Key, Is.EqualTo("Sessions"));
        Assert.That(Convert.ToInt32(aspNet.Rows[0].Value), Is.EqualTo(2));

        var yarp = cards.Cards.Single(c => c.Title == "Yarp Example");
        Assert.That(yarp.Value?.ToString(), Is.EqualTo("1/2"));
        Assert.That(yarp.Variant, Is.EqualTo("warning"));
    }

    [Test]
    public void ShallExecuteReplicaAndCardsPresets()
    {
        var fig = new DashboardFigRoot
        {
            clients = new DashboardJsArray(new object?[]
            {
                new DashboardClientJsModel { name = "AspNetApi", instance = "a" },
                new DashboardClientJsModel { name = "AspNetApi", instance = "b" },
                new DashboardClientJsModel { name = "Yarp Example" }
            }),
            runSessions = new DashboardJsArray(new object?[]
            {
                new DashboardRunSessionJsModel
                {
                    name = "AspNetApi",
                    instance = "a",
                    hostname = "host-a",
                    applicationVersion = "1.2.0",
                    figVersion = "5.0.0",
                    startTimeUtc = "2026-08-13T08:00:00Z",
                    uptimeHuman = "12 hours",
                    uptimePercent24Hr = 100,
                    customProperties = new Dictionary<string, object?>
                    {
                        ["isMaster"] = true,
                        ["lastSyncTime"] = "2026-08-13T10:00:00Z"
                    }
                },
                new DashboardRunSessionJsModel
                {
                    name = "AspNetApi",
                    instance = "b",
                    hostname = "host-b",
                    applicationVersion = "1.2.0",
                    figVersion = "5.0.1",
                    startTimeUtc = "2026-08-13T10:00:00Z",
                    uptimeHuman = "10 hours",
                    uptimePercent24Hr = 90,
                    customProperties = new Dictionary<string, object?>
                    {
                        ["isMaster"] = false,
                        ["lastSyncTime"] = "2026-08-13T12:00:00Z"
                    }
                },
                new DashboardRunSessionJsModel
                {
                    name = "Yarp Example",
                    hostname = "host-c",
                    applicationVersion = "2.0.0",
                    figVersion = "5.0.0",
                    startTimeUtc = "2026-08-13T11:00:00Z",
                    uptimeHuman = "9 hours",
                    uptimePercent24Hr = 95
                }
            })
        };

        var registry = new DashboardComponentRegistry();
        var replica = registry.GetPreset("replica-count-status")!;
        var kpi = DashboardComponentDataBinder.ToKpi(_engine.ExecuteScript(replica.Script, fig));
        Assert.That(Convert.ToInt32(kpi.Numerator), Is.EqualTo(2));
        Assert.That(Convert.ToInt32(kpi.Denominator), Is.EqualTo(3));
        Assert.That(kpi.Variant, Is.EqualTo("warning"));

        var masterSync = registry.GetPreset("master-and-last-sync")!;
        var keyValue = DashboardComponentDataBinder.ToKeyValue(_engine.ExecuteScript(masterSync.Script, fig));
        Assert.That(keyValue.Items.Single(i => i.Key == "Master").Value?.ToString(), Is.EqualTo("host-a"));
        Assert.That(keyValue.Items.Single(i => i.Key == "Last sync").Value?.ToString(), Is.EqualTo("2026-08-13T12:00:00Z"));

        var overview = registry.GetPreset("all-clients-overview")!;
        var cards = DashboardComponentDataBinder.ToCards(_engine.ExecuteScript(overview.Script, fig));
        Assert.That(cards.Cards, Has.Count.EqualTo(2));
        var aspNet = cards.Cards.Single(c => c.Title == "AspNetApi");
        Assert.That(aspNet.Value?.ToString(), Is.EqualTo("2/2"));
        Assert.That(aspNet.Variant, Is.EqualTo("success"));
        Assert.That(aspNet.Rows.Single(r => r.Key == "App version").Value?.ToString(), Is.EqualTo("1.2.0"));
        Assert.That(aspNet.Rows.Single(r => r.Key == "Runtime").Value?.ToString(), Is.EqualTo("12 hours"));
        Assert.That(aspNet.Rows.Single(r => r.Key == "Fig version").Value?.ToString(), Is.EqualTo("Multiple"));
        Assert.That(aspNet.Rows.Single(r => r.Key == "Uptime %").Value?.ToString(), Is.EqualTo("95.0%"));

        var uptime = registry.GetPreset("all-clients-uptime")!;
        var uptimeCards = DashboardComponentDataBinder.ToCards(_engine.ExecuteScript(uptime.Script, fig));
        var aspNetUptime = uptimeCards.Cards.Single(c => c.Title == "AspNetApi");
        Assert.That(aspNetUptime.Value?.ToString(), Is.EqualTo("95.0%"));
        Assert.That(aspNetUptime.Rows.Single(r => r.Key == "Running").Value?.ToString(), Is.EqualTo("2/2"));
    }

    [Test]
    public void ShallBindTextFromJintObjectLiteral()
    {
        var data = _engine.ExecuteScript(
            "return { text: 'Hello from Fig', variant: 'heading' };",
            SampleFig());

        Assert.That(data, Is.InstanceOf<ExpandoObject>());

        var text = DashboardComponentDataBinder.ToText(data);
        Assert.That(text.Lines, Has.Count.EqualTo(1));
        Assert.That(text.Lines[0].Text, Is.EqualTo("Hello from Fig"));
        Assert.That(text.Lines[0].Size, Is.EqualTo("xl"));
        Assert.That(text.Lines[0].Text, Does.Not.Contain("ExpandoObject"));
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
    public void ShallReadCardStyleFromConfig()
    {
        Assert.That(DashboardComponentDataBinder.ReadCardStyle(null), Is.EqualTo("compact"));
        Assert.That(
            DashboardComponentDataBinder.ReadCardStyle(new Newtonsoft.Json.Linq.JObject { ["cardStyle"] = "unknown" }),
            Is.EqualTo("compact"));
        Assert.That(
            DashboardComponentDataBinder.ReadCardStyle(new Newtonsoft.Json.Linq.JObject { ["cardStyle"] = "wide" }),
            Is.EqualTo("wide"));
        Assert.That(
            DashboardComponentDataBinder.ReadCardStyle(new Newtonsoft.Json.Linq.JObject { ["CardStyle"] = "WIDE" }),
            Is.EqualTo("wide"));
        Assert.That(
            DashboardComponentDataBinder.ReadCardStyle(new Newtonsoft.Json.Linq.JObject { ["cardStyle"] = "extraWide" }),
            Is.EqualTo("extraWide"));
        Assert.That(
            DashboardComponentDataBinder.ReadCardStyle(new Newtonsoft.Json.Linq.JObject { ["CardStyle"] = "EXTRAWIDE" }),
            Is.EqualTo("extraWide"));
        Assert.That(
            DashboardComponentDataBinder.ReadCardStyle(new Newtonsoft.Json.Linq.JObject { ["cardStyle"] = "compact" }),
            Is.EqualTo("compact"));
    }

    [Test]
    public void ShallReadChartSizeFromConfig()
    {
        Assert.That(DashboardComponentDataBinder.ReadChartSize(null), Is.EqualTo("large"));
        Assert.That(
            DashboardComponentDataBinder.ReadChartSize(new Newtonsoft.Json.Linq.JObject { ["chartSize"] = "unknown" }),
            Is.EqualTo("large"));
        Assert.That(
            DashboardComponentDataBinder.ReadChartSize(new Newtonsoft.Json.Linq.JObject { ["chartSize"] = "small" }),
            Is.EqualTo("small"));
        Assert.That(
            DashboardComponentDataBinder.ReadChartSize(new Newtonsoft.Json.Linq.JObject { ["ChartSize"] = "SMALL" }),
            Is.EqualTo("small"));
        Assert.That(
            DashboardComponentDataBinder.ReadChartSize(new Newtonsoft.Json.Linq.JObject { ["chartSize"] = "large" }),
            Is.EqualTo("large"));
    }

    [Test]
    public void ShallBindExpandoObjectDirectlyWithoutKeyValuePairArray()
    {
        dynamic expando = new ExpandoObject();
        expando.text = "Direct";
        expando.variant = "body";

        var text = DashboardComponentDataBinder.ToText((object)expando);
        Assert.That(text.Lines, Has.Count.EqualTo(1));
        Assert.That(text.Lines[0].Text, Is.EqualTo("Direct"));
        Assert.That(text.Lines[0].Size, Is.EqualTo("md"));
    }

    [Test]
    public void ShallBindTextLinesFromObject()
    {
        var data = _engine.ExecuteScript(
            """
            return {
              lines: [
                { text: '99.5%', size: 'xxl', color: '#8fd18f', align: 'center', weight: 'bold' },
                { text: 'Uptime (24h)', size: 'sm', color: '#9aa0a6', align: 'center' }
              ]
            };
            """,
            SampleFig());

        var text = DashboardComponentDataBinder.ToText(data);
        Assert.That(text.Lines, Has.Count.EqualTo(2));
        Assert.That(text.Lines[0].Text, Is.EqualTo("99.5%"));
        Assert.That(text.Lines[0].Size, Is.EqualTo("xxl"));
        Assert.That(text.Lines[0].Color, Is.EqualTo("#8fd18f"));
        Assert.That(text.Lines[0].Align, Is.EqualTo("center"));
        Assert.That(text.Lines[0].Weight, Is.EqualTo("bold"));
        Assert.That(text.Lines[1].Text, Is.EqualTo("Uptime (24h)"));
        Assert.That(text.Lines[1].Size, Is.EqualTo("sm"));
    }
}
