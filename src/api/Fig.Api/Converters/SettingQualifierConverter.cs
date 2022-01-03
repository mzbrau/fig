using Fig.Api.BusinessEntities;
using Fig.Contracts.Settings;

namespace Fig.Api.Converters;

public class SettingQualifierConverter : ISettingQualifierConverter
{
    public SettingQualifiersBusinessEntity Convert(SettingQualifiersDataContract dataContract)
    {
        return new SettingQualifiersBusinessEntity
        {
            Hostname = dataContract.Hostname,
            Username = dataContract.Username,
            Instance = dataContract.Instance
        };
    }
}