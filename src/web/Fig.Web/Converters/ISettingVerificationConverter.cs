using Fig.Contracts.SettingVerification;
using Fig.Web.Models.Setting;

namespace Fig.Web.Converters;

public interface ISettingVerificationConverter
{
    VerificationResultModel Convert(VerificationResultDataContract dataContract);
}