using Fig.Api.Reports;
using Fig.Datalayer.BusinessEntities.SettingValues;
using NUnit.Framework;

namespace Fig.Unit.Test.Api.Reports;

[TestFixture]
public class ReportValueFormatterTests
{
    [Test]
    public void FormatSettingValue_ReturnsEmptyForNull()
    {
        Assert.That(ReportValueFormatter.FormatSettingValue(null), Is.EqualTo(string.Empty));
    }

    [Test]
    public void FormatSettingValue_FormatsStringValue()
    {
        var value = new StringSettingBusinessEntity("hello");
        Assert.That(ReportValueFormatter.FormatSettingValue(value), Is.EqualTo("hello"));
    }

    [Test]
    public void FormatObject_FormatsDateTimeAsUtc()
    {
        var dt = new DateTime(2026, 3, 1, 12, 30, 0, DateTimeKind.Utc);
        Assert.That(ReportValueFormatter.FormatObject(dt), Is.EqualTo("2026-03-01 12:30:00 UTC"));
    }

    [Test]
    public void FormatObject_FormatsPrimitive()
    {
        Assert.That(ReportValueFormatter.FormatObject(42), Is.EqualTo("42"));
        Assert.That(ReportValueFormatter.FormatObject(true), Is.EqualTo("True"));
    }

    [Test]
    public void FormatObject_SerializesComplexObjectAsJson()
    {
        var json = ReportValueFormatter.FormatObject(new Dictionary<string, object?> { ["a"] = 1 });
        Assert.That(json, Does.Contain("\"a\""));
        Assert.That(json, Does.Contain("1"));
    }

    [Test]
    public void FormatClientDisplay_IncludesInstanceWhenPresent()
    {
        Assert.That(ReportValueFormatter.FormatClientDisplay("App", null), Is.EqualTo("App"));
        Assert.That(ReportValueFormatter.FormatClientDisplay("App", " "), Is.EqualTo("App"));
        Assert.That(ReportValueFormatter.FormatClientDisplay("App", "prod"), Is.EqualTo("App [prod]"));
    }

    [Test]
    public void FormatValueAsHtml_EncodesSpecialCharacters()
    {
        var value = new StringSettingBusinessEntity("<script>");
        Assert.That(ReportValueFormatter.FormatValueAsHtml(value), Is.EqualTo("&lt;script&gt;"));
    }
}
