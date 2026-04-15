namespace Fig.Web.Notifications;

public class NotificationHistoryService : INotificationHistoryService
{
    private const int MaxNotifications = 200;
    private readonly List<NotificationRecord> _notifications = new();
    private readonly object _syncRoot = new();
    private int _readCount;

    public void Record(NotificationRecord record)
    {
        lock (_syncRoot)
        {
            _notifications.Add(record);
            if (_notifications.Count > MaxNotifications)
            {
                int trimCount = _notifications.Count - MaxNotifications;
                _notifications.RemoveRange(0, trimCount);
                _readCount = Math.Max(0, _readCount - trimCount);
            }
        }

        NotifyChanged();
    }

    public IReadOnlyList<NotificationRecord> GetAll()
    {
        lock (_syncRoot)
        {
            return _notifications.ToArray();
        }
    }

    public int UnreadCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _notifications.Count - _readCount;
            }
        }
    }

    public void MarkAllAsRead()
    {
        lock (_syncRoot)
        {
            _readCount = _notifications.Count;
        }

        NotifyChanged();
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _notifications.Clear();
            _readCount = 0;
        }

        NotifyChanged();
    }

    public event Action? OnChange;

    private void NotifyChanged()
    {
        var onChange = OnChange;
        onChange?.Invoke();
    }
}
