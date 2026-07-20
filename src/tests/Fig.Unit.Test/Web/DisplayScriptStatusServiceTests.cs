using Fig.Common.NetStandard.Scripting;
using Fig.Web.Services;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class DisplayScriptStatusServiceTests
{
    private DisplayScriptStatusService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new DisplayScriptStatusService();
    }

    [Test]
    public void RegisterScripts_SetsProcessing()
    {
        _sut.RegisterScripts(2);

        Assert.That(_sut.IsProcessing, Is.True);
        Assert.That(_sut.IsComplete, Is.False);
    }

    [Test]
    public void ScriptCompleted_DoesNotCompleteUntilPendingReachesZero()
    {
        _sut.RegisterScripts(2);

        _sut.ScriptCompleted(ScriptRunResult.Succeeded("c"));

        Assert.That(_sut.IsProcessing, Is.True);
        Assert.That(_sut.IsComplete, Is.False);

        _sut.ScriptCompleted(ScriptRunResult.Succeeded("c"));

        Assert.That(_sut.IsProcessing, Is.False);
        Assert.That(_sut.IsComplete, Is.True);
    }

    [Test]
    public void ScriptCompleted_TalliesSucceededFailedAndSkipped()
    {
        _sut.RegisterScripts(4);

        _sut.ScriptCompleted(ScriptRunResult.Succeeded("c"));
        _sut.ScriptCompleted(ScriptRunResult.Succeeded("c"));
        _sut.ScriptCompleted(ScriptRunResult.Failed("c", new InvalidOperationException("bad")));
        _sut.ScriptCompleted(ScriptRunResult.Skipped());

        Assert.That(_sut.ExecutedCount, Is.EqualTo(4));
        Assert.That(_sut.SucceededCount, Is.EqualTo(2));
        Assert.That(_sut.FailedCount, Is.EqualTo(1));
        Assert.That(_sut.SkippedCount, Is.EqualTo(1));
        Assert.That(_sut.IsComplete, Is.True);
    }

    [Test]
    public void ScriptCompleted_WhenNoPending_StillTalliesResult()
    {
        _sut.ScriptCompleted(ScriptRunResult.Skipped());

        Assert.That(_sut.IsProcessing, Is.False);
        Assert.That(_sut.IsComplete, Is.False);
        Assert.That(_sut.ExecutedCount, Is.EqualTo(1));
        Assert.That(_sut.SkippedCount, Is.EqualTo(1));
    }

    [Test]
    public void ScriptCompleted_WhenResultNull_DoesNotTallyOutcome()
    {
        _sut.RegisterScripts(1);
        _sut.ScriptCompleted(null);

        Assert.That(_sut.IsComplete, Is.True);
        Assert.That(_sut.ExecutedCount, Is.EqualTo(0));
        Assert.That(_sut.SucceededCount, Is.EqualTo(0));
        Assert.That(_sut.FailedCount, Is.EqualTo(0));
        Assert.That(_sut.SkippedCount, Is.EqualTo(0));
    }

    [Test]
    public void MarkComplete_WithoutRegister_IsNoOp()
    {
        _sut.MarkComplete();

        Assert.That(_sut.IsProcessing, Is.False);
        Assert.That(_sut.IsComplete, Is.False);
    }

    [Test]
    public void MarkComplete_ForcesCompleteWhilePending()
    {
        _sut.RegisterScripts(3);

        _sut.MarkComplete();

        Assert.That(_sut.IsProcessing, Is.False);
        Assert.That(_sut.IsComplete, Is.True);
    }

    [Test]
    public void RegisterScripts_ZeroOrNegative_IsNoOp()
    {
        _sut.RegisterScripts(0);
        _sut.RegisterScripts(-1);

        Assert.That(_sut.IsProcessing, Is.False);
        Assert.That(_sut.IsComplete, Is.False);
    }

    [Test]
    public void Reset_ClearsStateAndTallies()
    {
        _sut.RegisterScripts(2);
        _sut.ScriptCompleted(ScriptRunResult.Succeeded("c"));
        _sut.Reset();

        Assert.That(_sut.IsProcessing, Is.False);
        Assert.That(_sut.IsComplete, Is.False);
        Assert.That(_sut.ExecutedCount, Is.EqualTo(0));
        Assert.That(_sut.SucceededCount, Is.EqualTo(0));
        Assert.That(_sut.FailedCount, Is.EqualTo(0));
        Assert.That(_sut.SkippedCount, Is.EqualTo(0));
    }
}
