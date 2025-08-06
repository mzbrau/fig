using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Common.NetStandard.Scripting;

namespace Fig.Client.Testing.Scripts;

/// <summary>
/// A test client implementation for testing display scripts
/// </summary>
public class TestClient : IScriptableClient
{
    public TestClient(string name, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        Name = name;
        Settings = new List<IScriptableSetting>();
    }

    public Guid Id { get; }
    
    public string Name { get; }
    
    public List<IScriptableSetting> Settings { get; }

    /// <summary>
    /// Add a setting to the test client
    /// </summary>
    public TestClient AddSetting(IScriptableSetting setting)
    {
        Settings.Add(setting);
        return this;
    }

    /// <summary>
    /// Add a string setting to the test client
    /// </summary>
    public TestClient AddStringSetting(string name, string? initialValue = null)
    {
        Settings.Add(new TestSetting(name, typeof(string), initialValue));
        return this;
    }

    /// <summary>
    /// Add a boolean setting to the test client
    /// </summary>
    public TestClient AddBooleanSetting(string name, bool initialValue = false)
    {
        Settings.Add(new TestSetting(name, typeof(bool), initialValue));
        return this;
    }

    /// <summary>
    /// Add an integer setting to the test client
    /// </summary>
    public TestClient AddIntegerSetting(string name, int initialValue = 0)
    {
        Settings.Add(new TestSetting(name, typeof(int), initialValue));
        return this;
    }

    /// <summary>
    /// Add a double setting to the test client
    /// </summary>
    public TestClient AddDoubleSetting(string name, double initialValue = 0.0)
    {
        Settings.Add(new TestSetting(name, typeof(double), initialValue));
        return this;
    }

    /// <summary>
    /// Add a dropdown setting to the test client
    /// </summary>
    public TestClient AddDropDownSetting(string name, string? initialValue = null, params string[] validValues)
    {
        Settings.Add(new TestDropDownSetting(name, initialValue, validValues.ToList()));
        return this;
    }

    /// <summary>
    /// Add a timespan setting to the test client
    /// </summary>
    public TestClient AddTimeSpanSetting(string name, TimeSpan? initialValue = null)
    {
        Settings.Add(new TestTimeSpanSetting(name, initialValue));
        return this;
    }
    
    /// <summary>
    /// Add a date time setting to the test client
    /// </summary>
    public TestClient AddDateTimeSetting(string name, DateTime? initialValue = null)
    {
        Settings.Add(new TestSetting(name, typeof(DateTime), initialValue));
        return this;
    }

    /// <summary>
    /// Add a data grid setting to the test client
    /// </summary>
    public TestClient AddDataGridSetting(string name, List<Dictionary<string, IDataGridValueModel>>? initialValue = null)
    {
        Settings.Add(new TestDataGridSetting(name, initialValue));
        return this;
    }

    /// <summary>
    /// Get a setting by name
    /// </summary>
    public IScriptableSetting? GetSetting(string name)
    {
        return Settings.FirstOrDefault(s => s.Name == name);
    }

    /// <summary>
    /// Get a setting by name with type casting
    /// </summary>
    public T? GetSetting<T>(string name) where T : class, IScriptableSetting
    {
        return Settings.FirstOrDefault(s => s.Name == name) as T;
    }
}
