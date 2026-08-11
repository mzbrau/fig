using System.Globalization;
using Fig.Contracts.Status;
using Fig.Web.Utils;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class CustomStatusPropertyFormatterTests
{
    [Test]
    public void Format_NullValue_ReturnsEmDash()
    {
        var property = new CustomStatusPropertyDataContract("X", CustomStatusValueType.String, null);
        Assert.That(CustomStatusPropertyFormatter.Format(property), Is.EqualTo("—"));
    }

    [Test]
    public void Format_Boolean_ReturnsYesOrNo()
    {
        Assert.That(
            CustomStatusPropertyFormatter.Format(
                new CustomStatusPropertyDataContract("A", CustomStatusValueType.Boolean, true)),
            Is.EqualTo("Yes"));
        Assert.That(
            CustomStatusPropertyFormatter.Format(
                new CustomStatusPropertyDataContract("B", CustomStatusValueType.Boolean, false)),
            Is.EqualTo("No"));
    }

    [Test]
    public void Format_DateTime_IncludesLocalAndRelative()
    {
        var utc = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var formatted = CustomStatusPropertyFormatter.Format(
            new CustomStatusPropertyDataContract("T", CustomStatusValueType.DateTime,
                utc.ToString("O", CultureInfo.InvariantCulture)));

        Assert.That(formatted, Does.Contain(utc.ToLocalTime().ToString("g")));
        Assert.That(formatted, Does.Contain("("));
    }

    [Test]
    public void Format_DateTimeOffset_IncludesLocalAndRelative()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var formatted = CustomStatusPropertyFormatter.Format(
            new CustomStatusPropertyDataContract("T", CustomStatusValueType.DateTimeOffset,
                dto.ToString("O", CultureInfo.InvariantCulture)));

        Assert.That(formatted, Does.Contain(dto.LocalDateTime.ToString("g")));
    }

    [Test]
    public void Format_TimeSpan_UsesHumanizer()
    {
        var formatted = CustomStatusPropertyFormatter.Format(
            new CustomStatusPropertyDataContract("L", CustomStatusValueType.TimeSpan, "00:00:01.5000000"));
        Assert.That(formatted, Does.Contain("second").IgnoreCase.Or.Contain("ms").IgnoreCase);
    }

    [Test]
    public void Format_Decimal_UsesInvariantString()
    {
        var formatted = CustomStatusPropertyFormatter.Format(
            new CustomStatusPropertyDataContract("D", CustomStatusValueType.Decimal, "12.34"));
        Assert.That(formatted, Is.EqualTo("12.34"));
    }

    [Test]
    public void Format_DateOnly_UsesShortDate()
    {
        var date = new DateOnly(2024, 3, 5);
        var formatted = CustomStatusPropertyFormatter.Format(
            new CustomStatusPropertyDataContract("D", CustomStatusValueType.DateOnly,
                date.ToString("O", CultureInfo.InvariantCulture)));
        Assert.That(formatted, Is.EqualTo(date.ToString("d", CultureInfo.CurrentCulture)));
    }

    [Test]
    public void Format_TimeOnly_UsesShortTime()
    {
        var time = new TimeOnly(9, 30, 15);
        var formatted = CustomStatusPropertyFormatter.Format(
            new CustomStatusPropertyDataContract("T", CustomStatusValueType.TimeOnly,
                time.ToString("O", CultureInfo.InvariantCulture)));
        Assert.That(formatted, Is.EqualTo(time.ToString("t", CultureInfo.CurrentCulture)));
        Assert.That(formatted, Does.Not.Contain("0000000"));
    }

    [Test]
    public void Format_Guid_UsesDFormat()
    {
        var guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var formatted = CustomStatusPropertyFormatter.Format(
            new CustomStatusPropertyDataContract("G", CustomStatusValueType.Guid, guid.ToString("D")));
        Assert.That(formatted, Is.EqualTo(guid.ToString("D")));
    }

    [Test]
    public void Format_InvalidDateTime_FallsBackToInvariantString()
    {
        const string raw = "not-a-date";
        var formatted = CustomStatusPropertyFormatter.Format(
            new CustomStatusPropertyDataContract("T", CustomStatusValueType.DateTime, raw));
        Assert.That(formatted, Is.EqualTo(raw));
    }
}
