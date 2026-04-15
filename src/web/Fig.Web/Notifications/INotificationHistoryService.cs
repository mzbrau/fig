namespace Fig.Web.Notifications;

public interface INotificationHistoryService
{
    void Record(NotificationRecord record);

    IReadOnlyList<NotificationRecord> GetAll();

    int UnreadCount { get; }

    void MarkAllAsRead();

    void Clear();

    event Action? OnChange;
}
