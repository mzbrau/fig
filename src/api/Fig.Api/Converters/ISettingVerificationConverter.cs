using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface ISettingVerificationConverter
{
    SettingDynamicVerificationBusinessEntity Convert(SettingDynamicVerificationDefinitionDataContract verification);

    SettingPluginVerificationBusinessEntity Convert(SettingPluginVerificationDefinitionDataContract verification);
    
    SettingDynamicVerificationDefinitionDataContract Convert(SettingDynamicVerificationBusinessEntity verification);
    
    SettingPluginVerificationDefinitionDataContract Convert(SettingPluginVerificationBusinessEntity verification, string? description);
}