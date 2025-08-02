using Fig.Common.NetStandard.Scripting;
using Jint;

namespace Fig.Web.Scripting;

public class WebScriptBeautifier : IScriptBeautifier
{
    private readonly IBeautifyLoader _beautifyLoader;
    private string? _beautifyJs;

    public WebScriptBeautifier(IBeautifyLoader beautifyLoader)
    {
        _beautifyLoader = beautifyLoader;
        
        _ = Task.Run(async () =>
        {
            return _beautifyJs = await _beautifyLoader.LoadBeautifyScript();
        });
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
            Console.WriteLine("Failed to beautify Javascript code" + e);
        }

        return script;
    }
}
