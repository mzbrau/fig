using System;

namespace Fig.Common.NetStandard.Scripting;

public interface IJsEngineFactory
{
    public IJsEngine CreateEngine(TimeSpan? timeoutInterval = null);
}