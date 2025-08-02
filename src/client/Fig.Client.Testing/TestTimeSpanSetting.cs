using System;
using Fig.Common.NetStandard.Scripting;

namespace Fig.Client.Testing;

/// <summary>
/// A test timespan setting implementation for testing display scripts
/// </summary>
public class TestTimeSpanSetting : TestSetting, ITimeSpanSettingModel
{
    public TestTimeSpanSetting(string name, TimeSpan? initialValue = null)
        : base(name, typeof(TimeSpan?), initialValue)
    {
    }
}
