using System;
using System.Collections.Generic;

namespace Fig.Common.NetStandard.Scripting;

public interface IDataGridColumn
{
    string Name { get; }

    Type Type { get; }

    List<string>? ValidValues { get; }
    
    int? EditorLineCount { get; }

    bool IsReadOnly { get; }
    
    string? ValidationRegex { get; }
    
    string? ValidationExplanation { get; }
    
    bool IsSecret { get; }
    
    string StartingWidth { get; }
}