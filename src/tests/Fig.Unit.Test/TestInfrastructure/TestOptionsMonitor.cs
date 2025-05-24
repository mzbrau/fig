using System;
using Microsoft.Extensions.Options;

namespace Fig.Unit.Test.TestInfrastructure;

public class TestOptionsMonitor<T> : IOptionsMonitor<T>
{
    private readonly T _currentValue;
    
    public TestOptionsMonitor(T value) => _currentValue = value;
    
    public T CurrentValue => _currentValue;
    
    public T Get(string? name) => _currentValue;
    
    public IDisposable OnChange(Action<T, string> listener) => new DummyDisposable();
    
    private class DummyDisposable : IDisposable { public void Dispose() { } }
}
