using Fig.Contracts.Settings;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public interface ISettingHistoryConverter
{
    SettingHistoryModel Convert(SettingValueDataContract value);
}