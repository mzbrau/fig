using Fig.Api.SettingVerification.Plugin;
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class SettingVerificationConverter : ISettingVerificationConverter
{
    private readonly ILogger<SettingVerificationConverter> _logger;
    private readonly IVerificationPluginFactory _verificationPluginFactory;

    public SettingVerificationConverter(ILogger<SettingVerificationConverter> logger,
        IVerificationPluginFactory verificationPluginFactory)
    {
        _logger = logger;
        _verificationPluginFactory = verificationPluginFactory;
    }

    public SettingDynamicVerificationBusinessEntity Convert(
        SettingDynamicVerificationDefinitionDataContract verification)
    {
        return new SettingDynamicVerificationBusinessEntity
        {
            Name = verification.Name,
            Description = verification.Description,
            Code = verification.Code,
            TargetRuntime = verification.TargetRuntime
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

    public SettingDynamicVerificationDefinitionDataContract Convert(
        SettingDynamicVerificationBusinessEntity verification)
    {
        // Note we do not send out code property
        return new SettingDynamicVerificationDefinitionDataContract
        {
            Name = verification.Name,
            Description = verification.Description,
            TargetRuntime = verification.TargetRuntime
        };
    }

    public SettingPluginVerificationDefinitionDataContract Convert(SettingPluginVerificationBusinessEntity verification)
    {
        var description = string.Empty;
        try
        {
            description = _verificationPluginFactory.GetVerifier(verification.Name).Description;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        return new SettingPluginVerificationDefinitionDataContract
        {
            Name = verification.Name,
            Description = description,
            PropertyArguments = verification.PropertyArguments.ToList()
        };
    }
}