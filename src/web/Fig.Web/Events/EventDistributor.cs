using System.Collections.Concurrent;

namespace Fig.Web.Events;

public class EventDistributor : IEventDistributor
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, List<Action>> _subscriptions = new();

    public EventDistributor(ILogger<EventDistributor> logger)
    {
        _logger = logger;
    }
    
    public void Subscribe(string topic, Action callback)
    {
        _subscriptions.AddOrUpdate(
            topic,
            new List<Action> { callback },
            (_, callbacks) =>
            {
                callbacks.Add(callback);
                return callbacks;
            });
    }

    public void Publish(string topic)
    {
        if (_subscriptions.TryGetValue(topic, out var subscribers))
        {
            foreach (var subscriber in subscribers)
            {
                try
                {
                    subscriber();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error occurred on callback for topic {Topic}", topic);
                }
            }
        }
    }
}