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

        _sut.ScriptCompleted();

        Assert.That(_sut.IsProcessing, Is.True);
        Assert.That(_sut.IsComplete, Is.False);

        _sut.ScriptCompleted();

        Assert.That(_sut.IsProcessing, Is.False);
        Assert.That(_sut.IsComplete, Is.True);
    }

    [Test]
    public void ScriptCompleted_WhenNoPending_IsNoOp()
    {
        _sut.ScriptCompleted();

        Assert.That(_sut.IsProcessing, Is.False);
        Assert.That(_sut.IsComplete, Is.False);
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
    public void Reset_ClearsState()
    {
        _sut.RegisterScripts(2);
        _sut.ScriptCompleted();
        _sut.Reset();

        Assert.That(_sut.IsProcessing, Is.False);
        Assert.That(_sut.IsComplete, Is.False);
    }
}
