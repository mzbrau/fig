using System.Collections.Generic;
using Fig.Contracts.SettingVerification;

namespace Fig.Integration.Test.Api.TestSettings.Verifications;

public class WebsiteVerifierV2 : ISettingVerification
{
    public VerificationResultDataContract PerformVerification(IDictionary<string, object?> settingValues)
    {
        var result = new VerificationResultDataContract
        {
            Message = "Simulated Failure"
        };
        return result;
    }
}