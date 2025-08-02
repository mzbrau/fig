using System;
using System.Collections.Generic;

namespace Fig.Common.NetStandard.Scripting;

public interface IDataGridSettingModel : IScriptableSetting
{
    IDataGridConfigurationModel? DataGridConfiguration { get; set; }
    
    List<Dictionary<string, IDataGridValueModel>>? Value { get; set; }
    
    void ValidateDataGrid(Action<int, string, string?>? processValidationError = null);
}
