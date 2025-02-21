using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Fig.Common.Events;

public class EventDistributor : IEventDistributor
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, List<Delegate>> _subscriptions = new();

    public EventDistributor(ILogger<EventDistributor> logger)
    {
        _logger = logger;
    }
    
    public void Subscribe(string topic, Action callback)
    {
        _subscriptions.AddOrUpdate(
            topic,
            [callback],
            (_, callbacks) =>
            {
                callbacks.Add(callback);
                return callbacks;
            });
    }

    // Subscribe method for callbacks with one parameter
    public void Subscribe<T>(string topic, Action<T> callback)
    {
        _subscriptions.AddOrUpdate(
            topic,
            [callback],
            (_, callbacks) =>
            {
                callbacks.Add(callback);
                return callbacks;
            });
    }

    public void Subscribe(string topic, Func<Task> callback)
    {
        _subscriptions.AddOrUpdate(
            topic,
            [callback],
            (_, callbacks) =>
            {
                callbacks.Add(callback);
                return callbacks;
            });
    }

    public void Subscribe<T>(string topic, Func<T, Task> callback)
    {
        _subscriptions.AddOrUpdate(
            topic,
            [callback],
            (_, callbacks) =>
            {
                callbacks.Add(callback);
                return callbacks;
            });
    }

    // Publish method without a parameter
    public void Publish(string topic)
    {
        if (_subscriptions.TryGetValue(topic, out var subscribers))
        {
            foreach (var subscriber in subscribers)
            {
                try
                {
                    if (subscriber is Action action)
                    {
                        action();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error occurred on callback for topic {Topic}", topic);
                }
            }
        }
    }

    // Publish method with one parameter
    public void Publish<T>(string topic, T arg)
    {
        if (_subscriptions.TryGetValue(topic, out var subscribers))
        {
            foreach (var subscriber in subscribers)
            {
                try
                {
                    if (subscriber is Action<T> action)
                    {
                        action(arg);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error occurred on callback for topic {Topic}", topic);
                }
            }
        }
    }

    public async Task PublishAsync(string topic)
    {
        if (_subscriptions.TryGetValue(topic, out var subscribers))
        {
            foreach (var subscriber in subscribers)
            {
                try
                {
                    switch (subscriber)
                    {
                        case Action action:
                            action();
                            break;
                        case Func<Task> asyncAction:
                            await asyncAction();
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error occurred on callback for topic {Topic}", topic);
                }
            }
        }
    }

    public async Task PublishAsync<T>(string topic, T arg)
    {
        if (_subscriptions.TryGetValue(topic, out var subscribers))
        {
            foreach (var subscriber in subscribers)
            {
                try
                {
                    switch (subscriber)
                    {
                        case Action<T> action:
                            action(arg);
                            break;
                        case Func<T, Task> asyncAction:
                            await asyncAction(arg);
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error occurred on callback for topic {Topic}", topic);
                }
            }
        }
    }
}