using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class NoSettings : TestSettingsBase
{
    public override string ClientName => "NoSettings";
    public override string ClientDescription => "Client with no settings";

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}