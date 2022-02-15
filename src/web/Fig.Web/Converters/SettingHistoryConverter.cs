using Fig.Contracts.Settings;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public class SettingHistoryConverter : ISettingHistoryConverter
{
    public SettingHistoryModel Convert(SettingValueDataContract dataContract)
    {
        return new SettingHistoryModel
        {
            DateTime = dataContract.ChangedAt.ToLocalTime(),
            Value = dataContract.Value,
            User = dataContract.ChangedBy
        };
    }
}