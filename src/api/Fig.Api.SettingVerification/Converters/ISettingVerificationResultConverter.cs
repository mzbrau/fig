using Fig.Api.SettingVerification.Sdk;
using Fig.Contracts.SettingVerification;

namespace Fig.Api.SettingVerification.Converters;

public interface ISettingVerificationResultConverter
{
    VerificationResultDataContract Convert(VerificationResult result);
}