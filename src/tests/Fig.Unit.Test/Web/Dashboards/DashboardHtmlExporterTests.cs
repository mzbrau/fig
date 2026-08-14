using Fig.Contracts.Dashboards;
using Fig.Web.Dashboards.Components.Contracts;
using Fig.Web.Dashboards.Export;
using Fig.Web.Dashboards.Runtime;
using NUnit.Framework;

namespace Fig.Unit.Test.Web.Dashboards;

[TestFixture]
public class DashboardHtmlExporterTests
{
    [Test]
    public void ShallExportStandaloneHtmlWithKpi()
    {
        var dashboard = new DashboardDataContract
        {
            Name = "Export Test",
            Description = "Desc",
            Definition = new DashboardDefinitionDataContract
            {
                Components =
                [
                    new DashboardComponentInstanceDataContract
                    {
                        Id = "kpi-1",
                        Type = "kpi",
                        Config = new Newtonsoft.Json.Linq.JObject { ["title"] = "Sessions" }
                    }
                ],
                Layout =
                [
                    new DashboardLayoutCellDataContract
                    {
                        Id = "kpi-1",
                        X = 0,
                        Y = 0,
                        Width = 4,
                        Height = 2
                    }
                ]
            }
        };

        var results = new Dictionary<string, DashboardComponentResult>
        {
            ["kpi-1"] = DashboardComponentResult.Ok(new DashboardKpiInput
            {
                Value = 42,
                Label = "Connected"
            })
        };

        var html = new DashboardHtmlExporter().Export(dashboard, results);

        Assert.That(html, Does.Contain("<!DOCTYPE html>"));
        Assert.That(html, Does.Contain("Export Test"));
        Assert.That(html, Does.Contain("42"));
        Assert.That(html, Does.Contain("chart.js"));
    }

    [Test]
    public void ShallExportStatusKpiAndCards()
    {
        var dashboard = new DashboardDataContract
        {
            Name = "Status Export",
            Definition = new DashboardDefinitionDataContract
            {
                Components =
                [
                    new DashboardComponentInstanceDataContract { Id = "kpi-1", Type = "kpi" },
                    new DashboardComponentInstanceDataContract
                    {
                        Id = "cards-1",
                        Type = "cards",
                        Config = new Newtonsoft.Json.Linq.JObject { ["cardStyle"] = "wide" }
                    },
                    new DashboardComponentInstanceDataContract
                    {
                        Id = "cards-2",
                        Type = "cards",
                        Config = new Newtonsoft.Json.Linq.JObject { ["cardStyle"] = "extraWide" }
                    }
                ],
                Layout =
                [
                    new DashboardLayoutCellDataContract { Id = "kpi-1", X = 0, Y = 0, Width = 4, Height = 2 },
                    new DashboardLayoutCellDataContract { Id = "cards-1", X = 4, Y = 0, Width = 4, Height = 2 },
                    new DashboardLayoutCellDataContract { Id = "cards-2", X = 8, Y = 0, Width = 4, Height = 2 }
                ]
            }
        };

        var results = new Dictionary<string, DashboardComponentResult>
        {
            ["kpi-1"] = DashboardComponentResult.Ok(new DashboardKpiInput
            {
                Numerator = 2,
                Denominator = 3,
                Label = "Replicas",
                Variant = "warning",
                Icon = "warning"
            }),
            ["cards-1"] = DashboardComponentResult.Ok(new DashboardCardsInput
            {
                Cards =
                [
                    new DashboardCardItem
                    {
                        Title = "AspNetApi",
                        Value = "2/2",
                        Variant = "success",
                        Rows = [new DashboardCardRow { Key = "Master", Value = "host-a" }]
                    }
                ]
            }),
            ["cards-2"] = DashboardComponentResult.Ok(new DashboardCardsInput
            {
                Cards =
                [
                    new DashboardCardItem
                    {
                        Title = "WideClient",
                        Value = "1/1",
                        Rows =
                        [
                            new DashboardCardRow
                            {
                                Key = "Last Sync",
                                Value = "2026-08-14T19:55:00.0000000Z"
                            }
                        ]
                    }
                ]
            })
        };

        var html = new DashboardHtmlExporter().Export(dashboard, results);

        Assert.That(html, Does.Contain("2/3"));
        Assert.That(html, Does.Contain("kpi--warning"));
        Assert.That(html, Does.Contain("AspNetApi"));
        Assert.That(html, Does.Contain("card--success"));
        Assert.That(html, Does.Contain("cards--wide"));
        Assert.That(html, Does.Contain("cards--extraWide"));
        Assert.That(html, Does.Contain("Master"));
        Assert.That(html, Does.Contain("2026-08-14T19:55:00.0000000Z"));
        Assert.That(html, Does.Contain("title=\"2026-08-14T19:55:00.0000000Z\""));
    }

    [Test]
    public void ShallExportDonutWithSmallChartSize()
    {
        var dashboard = new DashboardDataContract
        {
            Name = "Donut Export",
            Definition = new DashboardDefinitionDataContract
            {
                Components =
                [
                    new DashboardComponentInstanceDataContract
                    {
                        Id = "donut-1",
                        Type = "donut",
                        Config = new Newtonsoft.Json.Linq.JObject { ["chartSize"] = "small" }
                    }
                ],
                Layout =
                [
                    new DashboardLayoutCellDataContract { Id = "donut-1", X = 0, Y = 0, Width = 4, Height = 2 }
                ]
            }
        };

        var results = new Dictionary<string, DashboardComponentResult>
        {
            ["donut-1"] = DashboardComponentResult.Ok(new List<DashboardChartPoint>
            {
                new() { Label = "A", Value = 1 },
                new() { Label = "B", Value = 2 }
            })
        };

        var html = new DashboardHtmlExporter().Export(dashboard, results);

        Assert.That(html, Does.Contain("height=\"120\""));
        Assert.That(html, Does.Contain("doughnut"));
        Assert.That(html, Does.Not.Contain("height=\"180\""));
    }

    [Test]
    public void ShallEscapeScriptBreakingLabelsInChartExport()
    {
        var dashboard = new DashboardDataContract
        {
            Name = "Chart Escape",
            Definition = new DashboardDefinitionDataContract
            {
                Components =
                [
                    new DashboardComponentInstanceDataContract
                    {
                        Id = "bar-1",
                        Type = "bar"
                    }
                ],
                Layout =
                [
                    new DashboardLayoutCellDataContract { Id = "bar-1", X = 0, Y = 0, Width = 4, Height = 2 }
                ]
            }
        };

        var results = new Dictionary<string, DashboardComponentResult>
        {
            ["bar-1"] = DashboardComponentResult.Ok(new List<DashboardChartPoint>
            {
                new() { Label = "</script><script>alert(1)</script>", Value = 1 }
            })
        };

        var html = new DashboardHtmlExporter().Export(dashboard, results);

        Assert.That(html, Does.Not.Contain("</script><script>alert(1)</script>"));
        Assert.That(html, Does.Contain("\\u003c/script\\u003e"));
    }
}
