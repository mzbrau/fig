using System.Diagnostics;
using Fig.Common.Events;
using Fig.Web.Events;
using Fig.Web.Models.Setting;
using Jint;

namespace Fig.Web.Scripting;

public class ScriptRunner : IScriptRunner
{
    private readonly IEventDistributor _eventDistributor;
    private readonly IInfiniteLoopDetector _infiniteLoopDetector;
    private string? _beautifyJs;

    public ScriptRunner(IBeautifyLoader beautifyLoader,
        IEventDistributor eventDistributor,
        IInfiniteLoopDetector infiniteLoopDetector)
    {
        _eventDistributor = eventDistributor;
        _infiniteLoopDetector = infiniteLoopDetector;
        
        _ = Task.Run(async () =>
        {
            return _beautifyJs = await beautifyLoader.LoadBeautifyScript();
        });
    }
    
    public void RunScript(string? script, SettingClientConfigurationModel client)
    {
        if (string.IsNullOrWhiteSpace(script) || _infiniteLoopDetector.IsPossibleInfiniteLoop(client.Id))
            return;
        
        var watch = Stopwatch.StartNew();
        using var engine = new Engine(options =>
        {
            options.TimeoutInterval(TimeSpan.FromSeconds(5));
        });

        engine.SetValue("log", new Action<object>(Console.WriteLine));

        List<SettingWrapper> settingWrappers = new();
        foreach (var setting in client.Settings)
        {
            var wrapper = new SettingWrapper(setting);
            engine.SetValue(setting.Name, wrapper);
            settingWrappers.Add(wrapper);
        }

        try
        {
            engine.Execute(script);
            settingWrappers.ForEach(a => a.ApplyChangesToDataGrid());
            Console.WriteLine($"Script for {client.Name} executed successfully in {watch.ElapsedMilliseconds}ms");
            _eventDistributor.Publish(EventConstants.RefreshView);
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
        if (string.IsNullOrEmpty(_beautifyJs))
        {
            
            return script;
        }
        
        using var engine = new Engine();

        try
        {
            engine.Execute("var global = {};").Execute(_beautifyJs);
            var jsBeautify = engine.Evaluate("global.js_beautify");
            var result = engine.Invoke(jsBeautify, script).AsString();

            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine("failed to beautify code" + e);
        }

        return script;
    }
}