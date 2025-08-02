using Fig.Common.NetStandard.Scripting;

namespace Fig.Web.Scripting;

public class JintEngineFactory : IJsEngineFactory
{
    public IJsEngine CreateEngine(TimeSpan? timeoutInterval = null)
    {
        return new JintEngine(timeoutInterval ?? TimeSpan.FromSeconds(5));
    }
}