using System;
using System.Collections.Generic;
using Fig.Common.NetStandard.Scripting;

namespace Fig.Client.Testing;

/// <summary>
/// Main testing framework for Fig display scripts
/// </summary>
public class DisplayScriptTestRunner
{
    private readonly IScriptRunner _scriptRunner;

    public DisplayScriptTestRunner()
    {
        var infiniteLoopDetector = new InfiniteLoopDetector();
        var factory = new JintEngineFactory();
        _scriptRunner = new ScriptRunner(infiniteLoopDetector, factory);
    }

    /// <summary>
    /// Run a display script against a test client
    /// </summary>
    /// <param name="script">The JavaScript display script to execute</param>
    /// <param name="client">The test client with configured settings</param>
    public void RunScript(string script, TestClient client)
    {
        try
        {
            _scriptRunner.RunScript(script, client);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Script execution failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create a new test client
    /// </summary>
    /// <param name="clientName">Name of the test client</param>
    /// <returns>A new TestClient instance for configuring settings</returns>
    public TestClient CreateTestClient(string clientName)
    {
        return new TestClient(clientName);
    }
}
