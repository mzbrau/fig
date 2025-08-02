using Fig.Common.NetStandard.Scripting;
using Jint;

namespace Fig.Web.Scripting;

public class JintEngine : IJsEngine
{
    private readonly Engine _engine;
    
    public JintEngine(TimeSpan timeoutInterval)
    {
        _engine = new Engine(options =>
        {
            options.TimeoutInterval(timeoutInterval);
        });
    }

    public IJsEngine SetValue<T>(string name, T? obj)
    {
        _engine.SetValue<T>(name, obj);
        return this;
    }

    public IJsEngine Execute(string code, string? source = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return this; // No-op for empty scripts
        
        _engine.Execute(code, source);
        return this;
    }
    
    public void Dispose()
    {
        _engine.Dispose();
    }
}