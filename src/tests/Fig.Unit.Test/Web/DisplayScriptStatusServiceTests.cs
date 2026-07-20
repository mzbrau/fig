using Fig.Common.NetStandard.Scripting;
using Fig.Web.Services;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class DisplayScriptStatusServiceTests
{
    private Mock<IScriptRunner> _scriptRunner = null!;
    private DisplayScriptStatusService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _scriptRunner = new Mock<IScriptRunner>();
        _scriptRunner
            .Setup(r => r.FormatScript(It.IsAny<string>()))
            .Returns((string script) => $"formatted:{script}");
        _sut = new DisplayScriptStatusService(_scriptRunner.Object);
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
        _sut.ScriptCompleted(ScriptRunResult.Succeeded("c"), "SettingA", "a.Value = 1;");
        _sut.Reset();

        Assert.That(_sut.IsProcessing, Is.False);
        Assert.That(_sut.IsComplete, Is.False);
        Assert.That(_sut.ExecutedCount, Is.EqualTo(0));
        Assert.That(_sut.SucceededCount, Is.EqualTo(0));
        Assert.That(_sut.FailedCount, Is.EqualTo(0));
        Assert.That(_sut.SkippedCount, Is.EqualTo(0));
        Assert.That(_sut.HasRuns, Is.True);
        Assert.That(_sut.Runs, Has.Count.EqualTo(1));
    }

    [Test]
    public void ScriptCompleted_AppendsRunRecordWithContext()
    {
        var failed = ScriptRunResult.Failed("ClientA", new InvalidOperationException("boom"), durationMs: 12);

        _sut.ScriptCompleted(ScriptRunResult.Succeeded("ClientA", durationMs: 5), "Alpha", "alpha.Value = 1;");
        _sut.ScriptCompleted(failed, "Beta", "beta.Value = 2;");
        _sut.ScriptCompleted(ScriptRunResult.Skipped(), "Gamma", " ");

        Assert.That(_sut.HasRuns, Is.True);
        Assert.That(_sut.Runs, Has.Count.EqualTo(3));

        var succeeded = _sut.Runs[0];
        Assert.That(succeeded.ClientName, Is.EqualTo("ClientA"));
        Assert.That(succeeded.SettingName, Is.EqualTo("Alpha"));
        Assert.That(succeeded.Script, Is.EqualTo("alpha.Value = 1;"));
        Assert.That(succeeded.FormattedScript, Is.EqualTo("formatted:alpha.Value = 1;"));
        Assert.That(succeeded.DurationMs, Is.EqualTo(5));
        Assert.That(succeeded.Outcome, Is.EqualTo("Succeeded"));
        Assert.That(succeeded.Success, Is.True);
        Assert.That(succeeded.ErrorMessage, Is.Null);
        Assert.That(succeeded.SearchText, Does.Contain("clienta"));
        Assert.That(succeeded.SearchText, Does.Contain("alpha"));
        Assert.That(succeeded.SearchText, Does.Contain("alpha.value = 1;"));
        Assert.That(succeeded.SearchText, Does.Contain("succeeded"));
        Assert.That(succeeded.SearchText, Does.Contain("5 ms"));

        var failedRecord = _sut.Runs[1];
        Assert.That(failedRecord.Outcome, Is.EqualTo("Failed"));
        Assert.That(failedRecord.Success, Is.False);
        Assert.That(failedRecord.ErrorMessage, Is.EqualTo("boom"));
        Assert.That(failedRecord.DurationMs, Is.EqualTo(12));
        Assert.That(failedRecord.FormattedScript, Is.EqualTo("formatted:beta.Value = 2;"));
        Assert.That(failedRecord.SearchText, Does.Contain("boom"));
        Assert.That(failedRecord.SearchText, Does.Contain("failed"));

        var skipped = _sut.Runs[2];
        Assert.That(skipped.Outcome, Is.EqualTo("Skipped"));
        Assert.That(skipped.Success, Is.False);
        Assert.That(skipped.ErrorMessage, Is.Null);
        Assert.That(skipped.FormattedScript, Is.EqualTo(string.Empty));
        Assert.That(skipped.SearchText, Does.Contain("skipped"));
    }

    [Test]
    public void ScriptCompleted_WhenResultNull_DoesNotAppendRun()
    {
        _sut.RegisterScripts(1);
        _sut.ScriptCompleted(null);

        Assert.That(_sut.HasRuns, Is.False);
        Assert.That(_sut.Runs, Is.Empty);
    }

    [Test]
    public void ScriptCompleted_TrimsOldestRunsWhenOverCap()
    {
        for (var i = 0; i < 501; i++)
        {
            _sut.ScriptCompleted(
                ScriptRunResult.Succeeded("c", durationMs: i),
                $"Setting{i}",
                $"script{i}");
        }

        Assert.That(_sut.Runs, Has.Count.EqualTo(500));
        Assert.That(_sut.Runs[0].SettingName, Is.EqualTo("Setting1"));
        Assert.That(_sut.Runs[^1].SettingName, Is.EqualTo("Setting500"));
    }

    [Test]
    public void ScriptCompleted_DoesNotFormatWhitespaceOnlyScripts()
    {
        _sut.ScriptCompleted(ScriptRunResult.Skipped(), "Gamma", "   ");

        _scriptRunner.Verify(r => r.FormatScript(It.IsAny<string>()), Times.Never);
        Assert.That(_sut.Runs[0].FormattedScript, Is.EqualTo(string.Empty));
    }
}
