using System;
using System.Collections.Generic;

namespace Fig.Common.NetStandard.Scripting;

public interface IScriptableClient
{
    Guid Id { get; }
    
    string Name { get; }
    
    List<IScriptableSetting> Settings { get; }
}
