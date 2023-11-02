using Fig.Client;

namespace Fig.Test.Common.TestSettings;

public abstract class TestSettingsBase : SettingsBase
{
    public abstract string ClientName { get; }
}