using Fig.Api.DatabaseMigrations.Migrations;
using NUnit.Framework;

namespace Fig.Unit.Test.DatabaseMigrations;

[TestFixture]
public class Migration_005_SeedSettingGroupsTests
{
    private Migration_005_SeedSettingGroups _migration = null!;

    [SetUp]
    public void SetUp()
    {
        _migration = new Migration_005_SeedSettingGroups();
    }

    [Test]
    public void ExecutionNumber_ShouldReturn5()
    {
        Assert.That(_migration.ExecutionNumber, Is.EqualTo(5));
    }

    [Test]
    public void GetLeafName_ReturnsTrimmedName_WhenNoDelimiter()
    {
        Assert.That(Migration_005_SeedSettingGroups.GetLeafName("  MySetting  "), Is.EqualTo("MySetting"));
    }

    [Test]
    public void GetLeafName_ReturnsLastSegment_WhenDelimited()
    {
        Assert.That(Migration_005_SeedSettingGroups.GetLeafName("Parent->Child->Leaf"), Is.EqualTo("Leaf"));
    }

    [Test]
    public void GetLeafName_ReturnsEmpty_WhenNullOrWhitespace()
    {
        Assert.That(Migration_005_SeedSettingGroups.GetLeafName(null!), Is.EqualTo(string.Empty));
        Assert.That(Migration_005_SeedSettingGroups.GetLeafName("   "), Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetLeafName_ReturnsOriginal_WhenOnlyDelimiters()
    {
        // Split with RemoveEmptyEntries yields no parts; method falls back to trimmed input.
        Assert.That(Migration_005_SeedSettingGroups.GetLeafName("->"), Is.EqualTo("->"));
    }
}
