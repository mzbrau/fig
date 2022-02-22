using Fig.Contracts.Settings;
using Fig.Web.Models.Setting;

namespace Fig.Web.Converters;

public interface ISettingHistoryConverter
{
    SettingHistoryModel Convert(SettingValueDataContract value);
}