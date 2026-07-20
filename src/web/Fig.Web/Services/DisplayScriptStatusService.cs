using Fig.Common.NetStandard.Scripting;
using Fig.Web.Models.Setting;

namespace Fig.Web.Services;

public class DisplayScriptStatusService : IDisplayScriptStatusService
{
    private const int MaxRuns = 500;

    private readonly IScriptRunner _scriptRunner;
    private readonly List<DisplayScriptRunRecord> _runs = new();
    private readonly object _runsLock = new();

    private int _pendingCount;
    private bool _isComplete;
    private bool _hasStarted;
    private int _executedCount;
    private int _succeededCount;
    private int _failedCount;
    private int _skippedCount;

    public DisplayScriptStatusService(IScriptRunner scriptRunner)
    {
        _scriptRunner = scriptRunner;
    }

    public bool IsProcessing => _hasStarted && !_isComplete;

    public bool IsComplete => _isComplete;

    public int ExecutedCount => _executedCount;

    public int SucceededCount => _succeededCount;

    public int FailedCount => _failedCount;

    public int SkippedCount => _skippedCount;

    public bool HasRuns
    {
        get
        {
            lock (_runsLock)
            {
                return _runs.Count > 0;
            }
        }
    }

    public IReadOnlyList<DisplayScriptRunRecord> Runs
    {
        get
        {
            lock (_runsLock)
            {
                return _runs.ToArray();
            }
        }
    }

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

    public void ScriptCompleted(ScriptRunResult? result = null, string? settingName = null, string? script = null)
    {
        var changed = false;

        if (result is not null)
        {
            Interlocked.Increment(ref _executedCount);
            if (result.WasSkipped)
                Interlocked.Increment(ref _skippedCount);
            else if (result.Success)
                Interlocked.Increment(ref _succeededCount);
            else
                Interlocked.Increment(ref _failedCount);

            AppendRun(result, settingName, script);
            changed = true;
        }

        if (_pendingCount > 0)
        {
            var remaining = Interlocked.Decrement(ref _pendingCount);
            if (remaining <= 0)
            {
                _pendingCount = 0;
                _isComplete = true;
            }

            changed = true;
        }

        if (changed)
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
        // Run history is intentionally preserved across settings reloads.
        NotifyChange();
    }

    private void AppendRun(ScriptRunResult result, string? settingName, string? script)
    {
        var outcome = result.WasSkipped
            ? "Skipped"
            : result.Success
                ? "Succeeded"
                : "Failed";

        var rawScript = script ?? string.Empty;
        var timestamp = DateTime.Now;
        var errorMessage = result.WasSkipped ? null : result.ErrorMessage;
        var formattedScript = string.IsNullOrWhiteSpace(rawScript)
            ? string.Empty
            : _scriptRunner.FormatScript(rawScript);

        var searchText = string.Join(
                '\n',
                result.ClientName,
                settingName,
                rawScript,
                outcome,
                errorMessage,
                $"{result.DurationMs} ms",
                timestamp.ToString("HH:mm:ss"))
            .ToLowerInvariant();

        var record = new DisplayScriptRunRecord
        {
            Timestamp = timestamp,
            ClientName = result.ClientName,
            SettingName = settingName ?? string.Empty,
            Script = rawScript,
            FormattedScript = formattedScript,
            DurationMs = result.DurationMs,
            Outcome = outcome,
            Success = result.Success && !result.WasSkipped,
            ErrorMessage = errorMessage,
            SearchText = searchText
        };

        lock (_runsLock)
        {
            _runs.Add(record);
            if (_runs.Count > MaxRuns)
            {
                var trimCount = _runs.Count - MaxRuns;
                _runs.RemoveRange(0, trimCount);
            }
        }
    }

    private void NotifyChange()
    {
        OnChange?.Invoke();
    }
}
