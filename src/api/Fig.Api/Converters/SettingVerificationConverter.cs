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
            TargetRuntime = verification.TargetRuntime,
            SettingsVerified = verification.SettingsVerified
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
        return new SettingDynamicVerificationDefinitionDataContract(
            verification.Name,
            verification.Description,
            null,
            verification.TargetRuntime,
            verification.SettingsVerified?.ToList() ?? new List<string>());
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

        return new SettingPluginVerificationDefinitionDataContract(verification.Name,
            description,
            verification.PropertyArguments?.ToList() ?? new List<string>());
    }

    public VerificationResultDataContract Convert(VerificationResultBusinessEntity verificationResult)
    {
        return new VerificationResultDataContract
        {
            Success = verificationResult.Success,
            Message = verificationResult.Message,
            Logs = verificationResult.Logs,
            RequestingUser = verificationResult.RequestingUser ?? "Unknown",
            ExecutionTime = verificationResult.ExecutionTime
        };
    }

    public VerificationResultBusinessEntity Convert(VerificationResultDataContract verificationResult,
        Guid clientId, string verificationName, string? requestingUser)
    {
        return new VerificationResultBusinessEntity
        {
            ClientId = clientId,
            VerificationName = verificationName,
            Success = verificationResult.Success,
            Message = verificationResult.Message,
            RequestingUser = requestingUser,
            ExecutionTime = verificationResult.ExecutionTime,
            Logs = verificationResult.Logs
        };
    }
}