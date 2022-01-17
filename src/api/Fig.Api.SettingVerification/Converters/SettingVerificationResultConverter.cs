using Fig.Api.SettingVerification.Sdk;
using Fig.Contracts.SettingVerification;

namespace Fig.Api.SettingVerification.Converters;

public class SettingVerificationResultConverter : ISettingVerificationResultConverter
{
    public VerificationResultDataContract Convert(VerificationResult result)
    {
        return new VerificationResultDataContract
        {
            Success = result.Success,
            Message = result.Message,
            Logs = result.Logs
        };
    }
}