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
}
