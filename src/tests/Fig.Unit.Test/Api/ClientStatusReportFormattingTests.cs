using Fig.Api.Reports;
using Fig.Api.Reports.Implementations;
using Fig.Api.Reports.Rendering;
using Fig.Api.Utils;
using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ReportValueFormatterTypeTests
{
    [Test]
    public void FormatFriendlyType_MapsNullableDateTime()
    {
        Assert.That(ReportValueFormatter.FormatFriendlyType(typeof(DateTime?)), Is.EqualTo("DateTime"));
    }

    [Test]
    public void FormatFriendlyType_MapsDataGridList()
    {
        Assert.That(
            ReportValueFormatter.FormatFriendlyType(typeof(List<Dictionary<string, object>>)),
            Is.EqualTo("DataGrid"));
    }

    [Test]
    public void FormatFriendlyType_SimplifiesUnsupportedGenericName()
    {
        Assert.That(ReportValueFormatter.FormatFriendlyType(typeof(List<int>)), Is.EqualTo("List"));
    }
}

[TestFixture]
public class ReportMarkdownTests
{
    [Test]
    public void ToHtml_RendersHeading()
    {
        var html = ReportMarkdown.ToHtml("# My Heading");
        Assert.That(html, Does.Contain("<h1"));
        Assert.That(html, Does.Contain("My Heading"));
        Assert.That(html, Does.Not.Contain("# My Heading"));
    }

    [Test]
    public void ToHtml_RendersImageFromDataUrl()
    {
        var html = ReportMarkdown.ToHtml("![logo](data:image/png;base64,abc)");
        Assert.That(html, Does.Contain("<img"));
        Assert.That(html, Does.Contain("data:image/png;base64,abc"));
    }

    [Test]
    public void ToHtml_PlainTextIsEncoded()
    {
        var html = ReportMarkdown.ToHtml("Hello & friends");
        Assert.That(html, Is.EqualTo("Hello &amp; friends"));
    }
}

[TestFixture]
public class ReportDataGridHtmlTests
{
    [Test]
    public void Build_RendersNestedTableWithoutJsonTypeMetadata()
    {
        var setting = new SettingBusinessEntity
        {
            Name = "MyAnimals",
            ValueType = typeof(List<Dictionary<string, object>>),
            DataGridDefinitionJson = JsonConvert.SerializeObject(new DataGridDefinitionDataContract(
            [
                new DataGridColumnDataContract("Name", typeof(string)),
                new DataGridColumnDataContract("Legs", typeof(int))
            ], false)),
            Value = new DataGridSettingBusinessEntity(
            [
                new Dictionary<string, object?> { ["Name"] = "Cat", ["Legs"] = 4 },
                new Dictionary<string, object?> { ["Name"] = "Spider", ["Legs"] = 8 }
            ])
        };

        var html = ReportDataGridHtml.Build(setting);

        Assert.That(html, Does.Contain("report-table-nested"));
        Assert.That(html, Does.Contain("<th>Name</th>"));
        Assert.That(html, Does.Contain("<td>Cat</td>"));
        Assert.That(html, Does.Contain("<td>8</td>"));
        Assert.That(html, Does.Not.Contain("$type"));
    }

    [Test]
    public void Build_MasksSecretColumns()
    {
        var setting = new SettingBusinessEntity
        {
            Name = "Logins",
            ValueType = typeof(List<Dictionary<string, object>>),
            DataGridDefinitionJson = JsonConvert.SerializeObject(new DataGridDefinitionDataContract(
            [
                new DataGridColumnDataContract("Username", typeof(string)),
                new DataGridColumnDataContract("Password", typeof(string), isSecret: true)
            ], false)),
            Value = new DataGridSettingBusinessEntity(
            [
                new Dictionary<string, object?> { ["Username"] = "alice", ["Password"] = "s3cret" }
            ])
        };

        var html = ReportDataGridHtml.Build(setting);

        Assert.That(html, Does.Contain("alice"));
        Assert.That(html, Does.Contain(ReportDataGridHtml.SecretMask));
        Assert.That(html, Does.Not.Contain("s3cret"));
    }

    [Test]
    public void Build_EmptyGridShowsNoRows()
    {
        var setting = new SettingBusinessEntity
        {
            Name = "Items",
            ValueType = typeof(List<Dictionary<string, object>>),
            DataGridDefinitionJson = JsonConvert.SerializeObject(new DataGridDefinitionDataContract(
            [
                new DataGridColumnDataContract("Value", typeof(string))
            ], false)),
            Value = new DataGridSettingBusinessEntity([])
        };

        var html = ReportDataGridHtml.Build(setting);
        Assert.That(html, Does.Contain("No rows"));
    }

    [Test]
    public void Build_FromRowsAndDefinition_RendersNestedTable()
    {
        var definition = new DataGridDefinitionDataContract(
        [
            new DataGridColumnDataContract("Name", typeof(string)),
            new DataGridColumnDataContract("Legs", typeof(int))
        ], false);
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "Cat", ["Legs"] = 4 }
        };

        var html = ReportDataGridHtml.Build(rows, definition);

        Assert.That(html, Does.Contain("report-table-nested"));
        Assert.That(html, Does.Contain("<th>Name</th>"));
        Assert.That(html, Does.Contain("<td>Cat</td>"));
        Assert.That(html, Does.Not.Contain("$type"));
    }

    [Test]
    public void BuildFromEventValue_ParsesFlattenedCsvIntoNestedTable()
    {
        var definition = new DataGridDefinitionDataContract(
        [
            new DataGridColumnDataContract("Username", typeof(string)),
            new DataGridColumnDataContract("Password", typeof(string), isSecret: true)
        ], false);

        var html = ReportDataGridHtml.BuildFromEventValue("alice,<SECRET>\nbob,<SECRET>", definition);

        Assert.That(html, Does.Contain("report-table-nested"));
        Assert.That(html, Does.Contain("<th>Username</th>"));
        Assert.That(html, Does.Contain("<td>alice</td>"));
        Assert.That(html, Does.Contain("<td>bob</td>"));
        Assert.That(html, Does.Contain(ReportDataGridHtml.SecretMask));
        Assert.That(html, Does.Not.Contain("<SECRET>"));
    }

    [Test]
    public void BuildFromEventValue_ParsesJsonArrayIntoNestedTable()
    {
        var definition = new DataGridDefinitionDataContract(
        [
            new DataGridColumnDataContract("Name", typeof(string)),
            new DataGridColumnDataContract("Legs", typeof(int))
        ], false);
        const string json = """[{"Name":"Cat","Legs":4},{"Name":"Spider","Legs":8}]""";

        var html = ReportDataGridHtml.BuildFromEventValue(json, definition);

        Assert.That(html, Does.Contain("report-table-nested"));
        Assert.That(html, Does.Contain("<td>Cat</td>"));
        Assert.That(html, Does.Contain("<td>8</td>"));
        Assert.That(html, Does.Not.Contain("$type"));
    }

    [Test]
    public void BuildFromEventValue_FallsBackToEncodedTextWithoutDefinition()
    {
        var html = ReportDataGridHtml.BuildFromEventValue("plain & value", definition: null);

        Assert.That(html, Is.EqualTo("plain &amp; value"));
        Assert.That(html, Does.Not.Contain("report-table-nested"));
    }

    [Test]
    public void BuildFromEventValue_NormalizesHistoricalMultilineJArrayMultiSelect()
    {
        var definition = new DataGridDefinitionDataContract(
        [
            new DataGridColumnDataContract("Name", typeof(string)),
            new DataGridColumnDataContract("Legs", typeof(int)),
            new DataGridColumnDataContract("FavouriteFood", typeof(string)),
            new DataGridColumnDataContract("Things", typeof(List<string>))
        ], false);

        const string historical = """
            Cow,4,Hay,[
              "two",
              "one"
            ]
            """;

        var html = ReportDataGridHtml.BuildFromEventValue(historical, definition);

        Assert.That(html, Does.Contain("report-table-nested"));
        Assert.That(html, Does.Contain("<td>Cow</td>"));
        Assert.That(html, Does.Contain("<td>4</td>"));
        Assert.That(html, Does.Contain("<td>Hay</td>"));
        Assert.That(html, Does.Contain("<td>two, one</td>"));
        Assert.That(html, Does.Not.Contain("<td>[</td>"));
        Assert.That(html, Does.Not.Contain("<td>]</td>"));
        Assert.That(Regex.Matches(html, "<tbody><tr>").Count, Is.EqualTo(1));
    }

    [Test]
    public void BuildFromEventValue_ParsesQuotedMultiSelectListCell()
    {
        var definition = new DataGridDefinitionDataContract(
        [
            new DataGridColumnDataContract("Name", typeof(string)),
            new DataGridColumnDataContract("Things", typeof(List<string>))
        ], false);

        var html = ReportDataGridHtml.BuildFromEventValue("Cow,\"two, one\"", definition);

        Assert.That(html, Does.Contain("<td>Cow</td>"));
        Assert.That(html, Does.Contain("<td>two, one</td>"));
        Assert.That(Regex.Matches(html, "<tbody><tr>").Count, Is.EqualTo(1));
    }

    [Test]
    public void NormalizeEmbeddedJsonArrays_CollapsesPrettyPrintedArray()
    {
        const string input = """
            Cow,4,Hay,[
              "two",
              "one"
            ]
            """;

        var normalized = ReportDataGridHtml.NormalizeEmbeddedJsonArrays(input);

        Assert.That(normalized.Trim(), Is.EqualTo("Cow,4,Hay,\"two, one\""));
    }
}

[TestFixture]
public class ChangedSettingDataGridFormattingTests
{
    [Test]
    public void GetDataGridValue_JoinsMultiSelectListWithoutNewlines()
    {
        var definition = new DataGridDefinitionDataContract(
        [
            new DataGridColumnDataContract("Name", typeof(string)),
            new DataGridColumnDataContract("Things", typeof(List<string>))
        ], false);

        var grid = new DataGridSettingBusinessEntity(
        [
            new Dictionary<string, object?>
            {
                ["Name"] = "Cow",
                ["Things"] = new List<string> { "two", "one" }
            }
        ]);

        var flattened = ChangedSetting.GetDataGridValue(grid, definition).Value;

        Assert.That(flattened, Is.Not.Null);
        Assert.That(flattened!.Trim(), Is.EqualTo("Cow,\"two, one\""));
        Assert.That(flattened, Does.Not.Contain("["));
    }

    [Test]
    public void GetDataGridValue_JoinsJArrayMultiSelectWithoutNewlines()
    {
        var definition = new DataGridDefinitionDataContract(
        [
            new DataGridColumnDataContract("Name", typeof(string)),
            new DataGridColumnDataContract("Things", typeof(List<string>))
        ], false);

        var grid = new DataGridSettingBusinessEntity(
        [
            new Dictionary<string, object?>
            {
                ["Name"] = "Cow",
                ["Things"] = JArray.Parse("""["two","one"]""")
            }
        ]);

        var flattened = ChangedSetting.GetDataGridValue(grid, definition).Value;

        Assert.That(flattened, Is.Not.Null);
        Assert.That(flattened!.Trim(), Is.EqualTo("Cow,\"two, one\""));
        Assert.That(flattened, Does.Not.Contain("["));
    }

    [Test]
    public void FormatCsvField_QuotesWhenNeeded()
    {
        Assert.That(ChangedSetting.FormatCsvField("plain"), Is.EqualTo("plain"));
        Assert.That(ChangedSetting.FormatCsvField("a,b"), Is.EqualTo("\"a,b\""));
        Assert.That(ChangedSetting.FormatCsvField(new List<string> { "two", "one" }), Is.EqualTo("\"two, one\""));
    }
}

[TestFixture]
public class SettingHistorySeeBelowTests
{
    [Test]
    public void FormatValueHtml_UsesDataGridBuildInsteadOfRawJson()
    {
        var grid = new DataGridSettingBusinessEntity(
        [
            new Dictionary<string, object?> { ["Name"] = "Cat" }
        ]);

        var html = SettingHistoryReport.FormatValueHtmlCore(grid, isSecret: false, asDataGrid: true, definition: null);

        Assert.That(html, Does.Contain("report-table-nested").Or.Contain("<table"));
        Assert.That(html, Does.Not.Contain("{\"Name\":\"Cat\"}"));
    }
}

[TestFixture]
public class ClientStatusSecretMaskingTests
{
    [Test]
    public void SecretSettingsAreIncludedWithMaskedValues()
    {
        var secret = new SettingBusinessEntity
        {
            Name = "Password",
            IsSecret = true,
            Value = new StringSettingBusinessEntity("cat")
        };
        var publicSetting = new SettingBusinessEntity
        {
            Name = "Public",
            IsSecret = false,
            Value = new StringSettingBusinessEntity("visible"),
            ValueType = typeof(string)
        };

        Assert.That(ClientStatusReport.FormatValueHtmlCore(secret), Is.EqualTo(ReportDataGridHtml.SecretMask));
        Assert.That(ClientStatusReport.FormatValueHtmlCore(publicSetting), Is.EqualTo("visible"));
    }
}
