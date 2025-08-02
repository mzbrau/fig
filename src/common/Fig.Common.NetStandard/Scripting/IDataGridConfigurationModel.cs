using System.Collections.Generic;

namespace Fig.Common.NetStandard.Scripting;

public interface IDataGridConfigurationModel
{
    List<IDataGridColumn> Columns { get; }

    bool IsLocked { get; }
}