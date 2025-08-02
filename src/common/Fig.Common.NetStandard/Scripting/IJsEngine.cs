using System;

namespace Fig.Common.NetStandard.Scripting;

public interface IJsEngine : IDisposable
{
    public IJsEngine SetValue<T>(string name, T? obj);

    public IJsEngine Execute(string code, string? source = null);
}