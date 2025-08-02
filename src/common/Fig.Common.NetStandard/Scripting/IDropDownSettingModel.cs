using System.Collections.Generic;

namespace Fig.Common.NetStandard.Scripting;

public interface IDropDownSettingModel : IScriptableSetting
{
    List<string> ValidValues { get; set; }
}
