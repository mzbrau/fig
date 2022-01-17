using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface ISettingConverter
{
    SettingDataContract Convert(SettingBusinessEntity setting);

    SettingBusinessEntity Convert(SettingDataContract setting);
}