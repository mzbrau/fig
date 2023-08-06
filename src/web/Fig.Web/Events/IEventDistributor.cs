namespace Fig.Web.Events;

public interface IEventDistributor
{
    void Subscribe(string topic, Action callback);

    void Publish(string topic);
}