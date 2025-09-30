using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            // Always register with full name for backward compatibility
            engine.SetValue(setting.Name, wrapper);
            settingWrappers.Add(wrapper);
        }
        
        // For nested settings, create proper JavaScript object hierarchy using dot notation
        var processedPaths = new HashSet<string>();
        
        foreach (var setting in client.Settings)
        {
            if (setting.Name.Contains("->"))
            {
                var wrapper = settingWrappers.First(w => w.Name == setting.Name);
                var parts = setting.Name.Split(new[] { "->" }, StringSplitOptions.None);
                
                // Create the nested object hierarchy
                for (int i = 0; i < parts.Length; i++)
                {
                    var currentPath = string.Join(".", parts.Take(i + 1));
                    
                    if (i < parts.Length - 1) // Not the final property
                    {
                        if (!processedPaths.Contains(currentPath))
                        {
                            // Create the object if it doesn't exist
                            engine.Execute($"if (typeof {currentPath} === 'undefined') {{ {currentPath} = {{}}; }}");
                            processedPaths.Add(currentPath);
                        }
                    }
                    else // Final property - set the wrapper
                    {
                        // Use JavaScript code to assign the wrapper to the nested property
                        var finalPath = string.Join(".", parts);
                        var tempVarName = "__temp_" + Guid.NewGuid().ToString("N");
                        
                        engine.SetValue(tempVarName, wrapper);
                        engine.Execute($"{finalPath} = {tempVarName}; delete {tempVarName};");
                    }
                }
            }
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