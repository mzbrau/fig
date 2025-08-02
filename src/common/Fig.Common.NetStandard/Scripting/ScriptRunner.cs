using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Fig.Common.NetStandard.Scripting;

public class ScriptRunner : IScriptRunner
{
    private readonly IInfiniteLoopDetector _infiniteLoopDetector;
    private readonly IJsEngineFactory _jsEngineFactory;
    private readonly IScriptBeautifier? _scriptBeautifier;

    public ScriptRunner(IInfiniteLoopDetector infiniteLoopDetector, IJsEngineFactory jsEngineFactory, IScriptBeautifier? scriptBeautifier = null)
    {
        _infiniteLoopDetector = infiniteLoopDetector;
        _jsEngineFactory = jsEngineFactory;
        _scriptBeautifier = scriptBeautifier;
    }
    
    public void RunScript(string? script, IScriptableClient client)
    {
        if (client is null)
            throw new ArgumentNullException(nameof(client));
        
        if (string.IsNullOrWhiteSpace(script) || _infiniteLoopDetector.IsPossibleInfiniteLoop(client.Id))
            return;
        
        var watch = Stopwatch.StartNew();
        using var engine = _jsEngineFactory.CreateEngine(TimeSpan.FromSeconds(5));

        engine.SetValue("log", new Action<object>(Console.WriteLine));

        var settingWrappers = new List<SettingWrapper>();
        foreach (var setting in client.Settings)
        {
            var wrapper = new SettingWrapper(setting);
            engine.SetValue(setting.Name, wrapper);
            settingWrappers.Add(wrapper);
        }

        try
        {
            engine.Execute(script!);
            settingWrappers.ForEach(a => a.ApplyChangesToDataGrid());
            Console.WriteLine($"Script for {client.Name} executed successfully in {watch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Script execution for client {client.Name} failed {ex}");
        }
        finally
        {
            _infiniteLoopDetector.AddExecution(client.Id, watch.ElapsedMilliseconds);
        }
    }
    
    public string FormatScript(string script)
    {
        if (_scriptBeautifier == null)
            return script;
            
        return _scriptBeautifier.FormatScript(script);
    }
}