using System.Collections.Generic;

namespace Fig.Contracts.SettingVerification
{
    public interface ISettingVerification
    {
        VerificationResultDataContract PerformVerification(Dictionary<string, object> settingValues);
    }
}