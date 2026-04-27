namespace Fig.Web.Services;

public class DisplayScriptStatusService : IDisplayScriptStatusService
{
    private int _pendingCount;
    private bool _isComplete;
    private bool _hasStarted;
    private bool _markedComplete;

    public bool IsProcessing => _hasStarted && !_markedComplete;

    public bool IsComplete => _isComplete;

    public event Action? OnChange;

    public void RegisterScripts(int count)
    {
        if (count <= 0)
            return;

        _hasStarted = true;
        _isComplete = false;
        _markedComplete = false;
        Interlocked.Add(ref _pendingCount, count);
        NotifyChange();
    }

    public void ScriptCompleted()
    {
        if (_pendingCount <= 0)
            return;

        Interlocked.Decrement(ref _pendingCount);
        NotifyChange();
    }

    public void MarkComplete()
    {
        if (!_hasStarted)
            return;

        _markedComplete = true;
        _isComplete = true;
        NotifyChange();
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _pendingCount, 0);
        _isComplete = false;
        _hasStarted = false;
        _markedComplete = false;
        NotifyChange();
    }

    private void NotifyChange()
    {
        OnChange?.Invoke();
    }
}
