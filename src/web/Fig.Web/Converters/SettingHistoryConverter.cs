using Fig.Contracts.Settings;
using Fig.Web.Models.Setting;

namespace Fig.Web.Converters;

public class SettingHistoryConverter : ISettingHistoryConverter
{
    public SettingHistoryModel Convert(SettingValueDataContract dataContract)
    {
        return new SettingHistoryModel(dateTime: dataContract.ChangedAt.ToLocalTime(), value: dataContract.Value,
            user: dataContract.ChangedBy);
    }
}