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
    
    public ScriptRunResult RunScript(string? script, IScriptableClient client, bool bypassLoopDetection = false)
    {
        if (client is null)
            throw new ArgumentNullException(nameof(client));
        
        if (string.IsNullOrWhiteSpace(script))
            return ScriptRunResult.Skipped();

        var results = RunScripts(
            new[] { (SettingName: string.Empty, Script: script!) },
            client,
            bypassLoopDetection);

        return results.Count > 0 ? results[0].Result : ScriptRunResult.Skipped();
    }

    public IReadOnlyList<(string SettingName, ScriptRunResult Result)> RunScripts(
        IReadOnlyList<(string SettingName, string Script)> scripts,
        IScriptableClient client,
        bool bypassLoopDetection = false)
    {
        if (client is null)
            throw new ArgumentNullException(nameof(client));

        if (scripts is null || scripts.Count == 0)
            return Array.Empty<(string, ScriptRunResult)>();

        if (!bypassLoopDetection && _infiniteLoopDetector.IsPossibleInfiniteLoop(client.Id))
        {
            return scripts
                .Select(s => (s.SettingName, ScriptRunResult.Skipped()))
                .ToList();
        }

        var results = new List<(string SettingName, ScriptRunResult Result)>(scripts.Count);
        var watch = Stopwatch.StartNew();
        using var engine = _jsEngineFactory.CreateEngine(TimeSpan.FromSeconds(5));

        try
        {
            engine.SetValue("log", new Action<object>(Console.WriteLine));
            var settingWrappers = RegisterSettings(engine, client);

            foreach (var (settingName, script) in scripts)
            {
                if (string.IsNullOrWhiteSpace(script))
                {
                    results.Add((settingName, ScriptRunResult.Skipped()));
                    continue;
                }

                try
                {
                    engine.Execute(script);
                    settingWrappers.ForEach(a => a.ApplyChangesToDataGrid());
                    Console.WriteLine($"Script for {client.Name}/{settingName} executed successfully in {watch.ElapsedMilliseconds}ms");
                    results.Add((settingName, ScriptRunResult.Succeeded(client.Name)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Script execution for client '{client.Name}' setting '{settingName}' failed. Error: {ex}");
                    results.Add((settingName, ScriptRunResult.Failed(client.Name, ex)));
                }
            }
        }
        catch (Exception ex)
        {
            // Registration / engine setup failure — mark any remaining scripts as failed.
            Console.WriteLine($"Script engine setup for client '{client.Name}' failed. Error: {ex}");
            while (results.Count < scripts.Count)
            {
                var remaining = scripts[results.Count];
                results.Add((remaining.SettingName, ScriptRunResult.Failed(client.Name, ex)));
            }
        }
        finally
        {
            // Initial-load runs bypass loop detection and must not pollute the
            // execution window used for interactive (value-change) runs.
            if (!bypassLoopDetection)
                _infiniteLoopDetector.AddExecution(client.Id, watch.ElapsedMilliseconds);
        }

        return results;
    }

    private static List<SettingWrapper> RegisterSettings(IJsEngine engine, IScriptableClient client)
    {
        var settingWrappers = new List<SettingWrapper>();

        foreach (var setting in client.Settings)
        {
            var wrapper = new SettingWrapper(setting);
            // Always register with full name for backward compatibility
            engine.SetValue(setting.Name, wrapper);
            settingWrappers.Add(wrapper);
        }

        // For nested settings, create proper JavaScript object hierarchy using dot notation
        // and register leaf name aliases for simpler script access.
        // Must stay inside try/catch: engine.Execute here previously escaped and crashed page load.
        var processedPaths = new HashSet<string>();
        var registeredLeafNames = new HashSet<string>();
        var topLevelNames = new HashSet<string>(client.Settings
            .Where(s => !s.Name.Contains("->"))
            .Select(s => s.Name));

        foreach (var setting in client.Settings)
        {
            if (!setting.Name.Contains("->"))
                continue;

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

            // Register leaf name alias unless it collides with a top-level setting
            // or another nested setting's leaf (first-wins)
            var leafName = parts[parts.Length - 1];
            if (!topLevelNames.Contains(leafName) && registeredLeafNames.Add(leafName))
            {
                engine.SetValue(leafName, wrapper);
            }
        }

        return settingWrappers;
    }

    public string FormatScript(string script)
    {
        if (_scriptBeautifier == null)
            return script;
            
        return _scriptBeautifier.FormatScript(script);
    }
}
