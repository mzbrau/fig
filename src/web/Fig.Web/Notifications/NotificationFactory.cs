using Radzen;

namespace Fig.Web.Notifications
{
    public class NotificationFactory : INotificationFactory
    {
        private readonly INotificationHistoryService _historyService;

        public NotificationFactory(INotificationHistoryService historyService)
        {
            _historyService = historyService;
        }

        public NotificationMessage Failure(string heading, string? message)
        {
            return CreateMessage(NotificationSeverity.Error, heading, message);
        }

        public NotificationMessage Info(string heading, string message)
        {
            return CreateMessage(NotificationSeverity.Info, heading, message);
        }

        public NotificationMessage Success(string heading, string message)
        {
            return CreateMessage(NotificationSeverity.Success, heading, message);
        }

        public NotificationMessage Warning(string heading, string message)
        {
            return CreateMessage(NotificationSeverity.Warning, heading, message);
        }

        private NotificationMessage CreateMessage(NotificationSeverity severity, string heading, string? message)
        {
            _historyService.Record(new NotificationRecord
            {
                Timestamp = DateTime.Now,
                Severity = severity,
                Summary = heading,
                Detail = message
            });

            return new NotificationMessage()
            {
                Severity = severity,
                Summary = heading,
                Detail = message,
                Duration = 10000,
                Style = "position: fixed; bottom: 0px; right: 10px;"
            };
        }
    }
}
