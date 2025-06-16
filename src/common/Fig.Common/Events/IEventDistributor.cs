namespace Fig.Common.Events;

public interface IEventDistributor
{
    void Subscribe(string topic, Action callback);

    void Subscribe<T>(string topic, Action<T> callback);

    void Subscribe(string topic, Func<Task> callback);

    void Subscribe<T>(string topic, Func<T, Task> callback);

    void Unsubscribe(string topic, Action callback);

    void Unsubscribe<T>(string topic, Action<T> callback);

    void Unsubscribe(string topic, Func<Task> callback);

    void Unsubscribe<T>(string topic, Func<T, Task> callback);

    void Publish(string topic);

    void Publish<T>(string topic, T arg);

    Task PublishAsync(string topic);
    
    Task PublishAsync<T>(string topic, T arg);
}