using Fig.Contracts.Diagnostics;
using NUnit.Framework;

namespace Fig.Unit.Test.Contracts;

[TestFixture]
public class LoadPerfFlagsTests
{
    [Test]
    public void Parse_NullOrEmpty_ReturnsOptimized()
    {
        Assert.That(LoadPerfFlags.Parse(null), Is.EqualTo(LoadPerfFlags.Optimized));
        Assert.That(LoadPerfFlags.Parse("  "), Is.EqualTo(LoadPerfFlags.Optimized));
    }

    [Test]
    public void Parse_Presets()
    {
        Assert.That(LoadPerfFlags.Parse("optimized"), Is.EqualTo(LoadPerfFlags.Optimized));
        Assert.That(LoadPerfFlags.Parse("baseline"), Is.EqualTo(LoadPerfFlags.Baseline));
        Assert.That(LoadPerfFlags.Parse("noCompact").CompactClientsJson, Is.False);
        Assert.That(LoadPerfFlags.Parse("noCompact").BatchDisplayScripts, Is.True);
    }

    [Test]
    public void Parse_OverridesAfterPreset()
    {
        var flags = LoadPerfFlags.Parse("optimized,batchDisplayScripts=0,compactClientsJson=0");
        Assert.That(flags.BatchDisplayScripts, Is.False);
        Assert.That(flags.CompactClientsJson, Is.False);
        Assert.That(flags.SkipNoopInit, Is.True);
        Assert.That(flags.DeferScripts, Is.True);
    }

    [Test]
    public void Parse_BaselineThenEnableOneBit()
    {
        var flags = LoadPerfFlags.Parse("baseline,compactClientsJson=1");
        Assert.That(flags.CompactClientsJson, Is.True);
        Assert.That(flags.BatchDisplayScripts, Is.False);
        Assert.That(flags.DeferScripts, Is.False);
    }

    [Test]
    public void ToHeaderValue_IsStableAndRoundTrips()
    {
        var header = LoadPerfFlags.Optimized.ToHeaderValue();
        Assert.That(header, Does.Contain("compactClientsJson=1"));
        Assert.That(LoadPerfFlags.Parse(header), Is.EqualTo(LoadPerfFlags.Optimized));
        Assert.That(LoadPerfFlags.Parse(LoadPerfFlags.Baseline.ToHeaderValue()), Is.EqualTo(LoadPerfFlags.Baseline));
    }
}
