using System.Collections.Generic;
using Fig.Common.NetStandard.Scripting;

namespace Fig.Client.Testing.Scripts;

/// <summary>
/// A test dropdown setting implementation for testing display scripts
/// </summary>
public class TestDropDownSetting : TestSetting, IDropDownSettingModel
{
    public TestDropDownSetting(string name, object? initialValue = null, List<string>? validValues = null)
        : base(name, typeof(string), initialValue)
    {
        ValidValues = validValues ?? new List<string>();
    }

    public List<string> ValidValues { get; set; }
}
