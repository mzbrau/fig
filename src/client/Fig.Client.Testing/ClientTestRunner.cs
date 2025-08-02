namespace Fig.Client.Testing;

/// <summary>
/// Enhanced test runner that can work with actual SettingsBase classes
/// </summary>
public class ClientTestRunner
{
    private readonly DisplayScriptTestRunner _displayScriptTestRunner = new();

    /// <summary>
    /// Create a test client from a SettingsBase instance with fluent API for overriding values
    /// </summary>
    /// <typeparam name="T">The settings class type</typeparam>
    /// <param name="settingsInstance">An instance of the settings class</param>
    /// <param name="clientName">Optional client name (defaults to type name)</param>
    /// <returns>A fluent client builder</returns>
    public TestClientBuilder<T> CreateClient<T>(T settingsInstance, string? clientName = null) where T : SettingsBase
    {
        var name = clientName ?? typeof(T).Name;
        return new TestClientBuilder<T>(settingsInstance, name, _displayScriptTestRunner);
    }

    /// <summary>
    /// Execute a display script against a configured test client
    /// </summary>
    /// <param name="testClient">The test client to execute against</param>
    /// <param name="script">The JavaScript display script to execute</param>
    public void ExecuteScript(TestClient testClient, string script)
    {
        _displayScriptTestRunner.RunScript(script, testClient);
    }
}
