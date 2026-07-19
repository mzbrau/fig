using Fig.Common.NetStandard.Scripting;

namespace Fig.Web.Services;

public class DisplayScriptStatusService : IDisplayScriptStatusService
{
    private int _pendingCount;
    private bool _isComplete;
    private bool _hasStarted;
    private int _executedCount;
    private int _succeededCount;
    private int _failedCount;
    private int _skippedCount;

    public bool IsProcessing => _hasStarted && !_isComplete;

    public bool IsComplete => _isComplete;

    public int ExecutedCount => _executedCount;

    public int SucceededCount => _succeededCount;

    public int FailedCount => _failedCount;

    public int SkippedCount => _skippedCount;

    public event Action? OnChange;

    public void RegisterScripts(int count)
    {
        if (count <= 0)
            return;

        _hasStarted = true;
        _isComplete = false;
        Interlocked.Add(ref _pendingCount, count);
        NotifyChange();
    }

    public void ScriptCompleted(ScriptRunResult? result = null)
    {
        if (result is not null)
        {
            Interlocked.Increment(ref _executedCount);
            if (result.WasSkipped)
                Interlocked.Increment(ref _skippedCount);
            else if (result.Success)
                Interlocked.Increment(ref _succeededCount);
            else
                Interlocked.Increment(ref _failedCount);
        }

        if (_pendingCount <= 0)
            return;

        var remaining = Interlocked.Decrement(ref _pendingCount);
        if (remaining <= 0)
        {
            _pendingCount = 0;
            _isComplete = true;
        }

        NotifyChange();
    }

    public void MarkComplete()
    {
        if (!_hasStarted)
            return;

        // Safety fallback when some scripts fail to report completion.
        Interlocked.Exchange(ref _pendingCount, 0);
        _isComplete = true;
        NotifyChange();
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _pendingCount, 0);
        Interlocked.Exchange(ref _executedCount, 0);
        Interlocked.Exchange(ref _succeededCount, 0);
        Interlocked.Exchange(ref _failedCount, 0);
        Interlocked.Exchange(ref _skippedCount, 0);
        _isComplete = false;
        _hasStarted = false;
        NotifyChange();
    }

    private void NotifyChange()
    {
        OnChange?.Invoke();
    }
}
