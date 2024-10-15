namespace Fig.Common.Events;

public interface IEventDistributor
{
    void Subscribe(string topic, Action callback);

    void Subscribe<T>(string topic, Action<T> callback);

    void Publish(string topic);

    void Publish<T>(string topic, T arg);
}