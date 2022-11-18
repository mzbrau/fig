using Fig.Contracts.SettingVerification;
using Fig.Web.Models.Setting;

namespace Fig.Web.Converters;

public class SettingVerificationConverter : ISettingVerificationConverter
{
    public VerificationResultModel Convert(VerificationResultDataContract dataContract)
    {
        return new VerificationResultModel(success: dataContract.Success, message: dataContract.Message,
            logs: dataContract.Logs, executionTime: dataContract.ExecutionTime.ToLocalTime());
    }
}