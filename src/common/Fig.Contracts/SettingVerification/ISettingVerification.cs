using System.Collections.Generic;

namespace Fig.Contracts.SettingVerification
{
    public interface ISettingVerification
    {
        VerificationResultDataContract PerformVerification(IDictionary<string, object?> settingValues);
    }
}