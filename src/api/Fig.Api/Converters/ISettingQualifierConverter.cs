using Fig.Api.BusinessEntities;
using Fig.Contracts.Settings;

namespace Fig.Api.Converters;

public interface ISettingQualifierConverter
{
    SettingQualifiersBusinessEntity Convert(SettingQualifiersDataContract dataContract);
}