using Radzen;

namespace Fig.Web.Notifications
{
    public interface INotificationFactory
    {
        public NotificationMessage Success(string heading, string message);

        public NotificationMessage Failure(string heading, string message);

        public NotificationMessage Info(string heading, string message);

        public NotificationMessage Warning(string heading, string message);
    }
}
