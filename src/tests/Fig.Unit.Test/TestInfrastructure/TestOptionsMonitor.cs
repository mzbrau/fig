using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Fig.Unit.Test.TestInfrastructure;

public class TestOptionsMonitor<T> : IOptionsMonitor<T>
{
    private readonly T _currentValue;
    private readonly List<Action<T, string>> _changeListeners = new();
    
    public TestOptionsMonitor(T value) => _currentValue = value;
    
    public T CurrentValue => _currentValue;
    
    public T Get(string? name) => _currentValue;
    
    public IDisposable OnChange(Action<T, string> listener)
    {
        _changeListeners.Add(listener);
        return new ChangeSubscription(() => _changeListeners.Remove(listener));
    }
    
    public void TriggerChange()
    {
        foreach (var listener in _changeListeners)
        {
            listener(_currentValue, string.Empty);
        }
    }
    
    private class ChangeSubscription : IDisposable
    {
        private readonly Action _unsubscribe;
        
        public ChangeSubscription(Action unsubscribe) => _unsubscribe = unsubscribe;
        
        public void Dispose() => _unsubscribe();
    }
}
