using Fig.Contracts.SettingVerification;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public class SettingVerificationConverter : ISettingVerificationConverter
{
    public VerificationResultModel Convert(VerificationResultDataContract dataContract)
    {
        return new VerificationResultModel
        {
            Success = dataContract.Success,
            Message = dataContract.Message,
            Logs = dataContract.Logs,
            ExecutionTime = dataContract.ExecutionTime,
            RequestingUser = dataContract.RequestingUser
        };
    }
}