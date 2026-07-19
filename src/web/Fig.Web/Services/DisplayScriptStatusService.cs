namespace Fig.Web.Services;

public class DisplayScriptStatusService : IDisplayScriptStatusService
{
    private int _pendingCount;
    private bool _isComplete;
    private bool _hasStarted;

    public bool IsProcessing => _hasStarted && !_isComplete;

    public bool IsComplete => _isComplete;

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

    public void ScriptCompleted()
    {
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
        _isComplete = false;
        _hasStarted = false;
        NotifyChange();
    }

    private void NotifyChange()
    {
        OnChange?.Invoke();
    }
}
