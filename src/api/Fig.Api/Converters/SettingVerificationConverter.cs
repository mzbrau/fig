using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class SettingVerificationConverter : ISettingVerificationConverter
{
    public SettingDynamicVerificationBusinessEntity Convert(SettingDynamicVerificationDefinitionDataContract verification)
    {
        return new SettingDynamicVerificationBusinessEntity
        {
            Name = verification.Name,
            Description = verification.Description,
            Code = verification.Code, // TODO Encrypt code and create checksum with service secret.
            TargetRuntime = verification.TargetRuntime,
        };
    }

    public SettingPluginVerificationBusinessEntity Convert(SettingPluginVerificationDefinitionDataContract verification)
    {
        return new SettingPluginVerificationBusinessEntity
        {
            Name = verification.Name,
            PropertyArguments = verification.PropertyArguments
        };
    }

    public SettingDynamicVerificationDefinitionDataContract Convert(SettingDynamicVerificationBusinessEntity verification)
    {
        // Note we do not send out code property
        return new SettingDynamicVerificationDefinitionDataContract
        {
            Name = verification.Name,
            Description = verification.Description,
            TargetRuntime = verification.TargetRuntime
        };
    }

    public SettingPluginVerificationDefinitionDataContract Convert(SettingPluginVerificationBusinessEntity verification,
        string? description)
    {
        return new SettingPluginVerificationDefinitionDataContract
        {
            Name = verification.Name,
            Description = description,
            PropertyArguments = verification.PropertyArguments
        };
    }
}