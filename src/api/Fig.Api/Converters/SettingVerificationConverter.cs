using Fig.Api.SettingVerification;
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class SettingVerificationConverter : ISettingVerificationConverter
{
    private readonly ILogger<SettingVerificationConverter> _logger;
    private readonly IVerificationFactory _verificationFactory;

    public SettingVerificationConverter(ILogger<SettingVerificationConverter> logger,
        IVerificationFactory verificationFactory)
    {
        _logger = logger;
        _verificationFactory = verificationFactory;
    }

    public SettingVerificationBusinessEntity Convert(SettingVerificationDefinitionDataContract verification)
    {
        return new SettingVerificationBusinessEntity
        {
            Name = verification.Name,
            PropertyArguments = verification.PropertyArguments
        };
    }

    public SettingVerificationDefinitionDataContract Convert(SettingVerificationBusinessEntity verification)
    {
        var description = string.Empty;
        try
        {
            description = _verificationFactory.GetVerifier(verification.Name).Description;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to set verifier description");
        }

        return new SettingVerificationDefinitionDataContract(verification.Name,
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